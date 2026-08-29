using System.Diagnostics;
using Supabase.Realtime;
using Supabase.Realtime.PostgresChanges;
using QueueApp.Constants;
using QueueApp.Services.Auth;

namespace QueueApp.Services.Realtime;

// The one piece of the Queue feature that isn't Refit — Realtime is a WebSocket subscription, not
// request/response. One socket carries every screen's feed; each screen owns a channel filtered to
// the rows it cares about, and channels are shared and reference counted when two screens happen to
// want the same filter.
public class QueueRealtimeService : IQueueRealtimeService
{
    // How long a channel nobody is listening to is kept joined before it is actually torn down.
    //
    // A tab switch raises Disappearing on the outgoing page and Appearing on the incoming one, both
    // fire-and-forget (see BaseViewModel), so a screen coming back can easily unsubscribe *after* it
    // has resubscribed. Tearing down on the spot meant every switch left and rejoined the channel —
    // and, when the last owner happened to go first, closed and reopened the socket underneath it.
    // Holding the channel for a beat makes the round trip a no-op: the handler comes straight back
    // onto a channel that never stopped delivering.
    private static readonly TimeSpan IdleGrace = TimeSpan.FromSeconds(5);

    private sealed class ChannelEntry
    {
        public ChannelEntry(RealtimeChannel channel)
        {
            Channel = channel;
        }

        public RealtimeChannel Channel { get; }

        public List<Func<Task>> Handlers { get; } = new();

        public async Task NotifyAsync()
        {
            foreach (var handler in Handlers.ToArray())
            {
                try
                {
                    await handler();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Realtime] handler threw: {ex.Message}");
                }
            }
        }
    }

    private readonly IAuthService _authService;
    private readonly SemaphoreSlim _gate = new(1, 1);

    // Keyed by owner *and* table: one screen legitimately watches two feeds at once — the browse
    // dashboard follows its queue ticket and its upcoming bookings side by side.
    private readonly Dictionary<(object Owner, string Table), (string ChannelKey, Func<Task> OnChange)> _owners = new();
    private readonly Dictionary<string, ChannelEntry> _channels = new();

    private Client? _client;
    private CancellationTokenSource? _idleSweep;

    public QueueRealtimeService(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task SubscribeAsync(object owner, string filterColumn, string filterValue, Func<Task> onChange, string table = "queue_entries")
    {
        await _gate.WaitAsync();
        try
        {
            // Something wants a feed again, so nothing is idle any more — whatever was queued for
            // teardown stays up, and the sweep is rescheduled by the next Unsubscribe.
            CancelIdleSweep();

            var key = $"{table}:{filterColumn}=eq.{filterValue}";
            var ownerKey = (owner, table);

            if (_owners.TryGetValue(ownerKey, out var existing))
            {
                if (existing.ChannelKey == key)
                    return;

                DetachOwner(ownerKey);
            }

            var client = await EnsureConnectedAsync();

            if (!_channels.TryGetValue(key, out var entry))
            {
                var channel = client.Channel("realtime", "public", table, filterColumn, filterValue, null!);
                var created = new ChannelEntry(channel);
                _channels[key] = created;
                entry = created;

                // Bound once per channel, not once per subscribe. Client.Channel hands back the
                // channel it already has for a topic rather than a new one, so binding on every
                // subscribe stacks another handler on the same channel and every row change then
                // reloads the screen once per stacked handler.
                channel.AddPostgresChangeHandler(PostgresChangesOptions.ListenType.All, async (_, _) =>
                {
                    Debug.WriteLine($"[Realtime] event on {key} {DateTime.Now:HH:mm:ss}");
                    await created.NotifyAsync();
                });

                await channel.Subscribe();
                Debug.WriteLine($"[Realtime] subscribed to {key}");
            }
            else if (entry.Handlers.Count == 0)
            {
                Debug.WriteLine($"[Realtime] reattached to {key} — still joined");
            }

            entry.Handlers.Add(onChange);
            _owners[ownerKey] = (key, onChange);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task UnsubscribeAsync(object owner)
    {
        await _gate.WaitAsync();
        try
        {
            foreach (var ownerKey in _owners.Keys.Where(k => ReferenceEquals(k.Owner, owner)).ToList())
                DetachOwner(ownerKey);

            ScheduleIdleSweep();
        }
        finally
        {
            _gate.Release();
        }
    }

    // Releases the screen's claim on its feed. The channel itself is left joined for the grace
    // period and collected by the sweep if nothing has claimed it by then.
    private void DetachOwner((object Owner, string Table) ownerKey)
    {
        if (!_owners.Remove(ownerKey, out var registration))
            return;

        if (_channels.TryGetValue(registration.ChannelKey, out var entry))
            entry.Handlers.Remove(registration.OnChange);
    }

    private void CancelIdleSweep()
    {
        _idleSweep?.Cancel();
        _idleSweep?.Dispose();
        _idleSweep = null;
    }

    private void ScheduleIdleSweep()
    {
        CancelIdleSweep();

        var cts = new CancellationTokenSource();
        _idleSweep = cts;
        _ = SweepIdleChannelsAsync(cts, cts.Token);
    }

    private async Task SweepIdleChannelsAsync(CancellationTokenSource cts, CancellationToken token)
    {
        try
        {
            await Task.Delay(IdleGrace, token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        await _gate.WaitAsync();
        try
        {
            // A subscribe that landed while this was waiting for the gate has already replaced the
            // sweep. Identity rather than the token, because that subscribe disposed the source.
            if (!ReferenceEquals(_idleSweep, cts))
                return;

            _idleSweep = null;
            cts.Dispose();

            foreach (var (key, entry) in _channels.Where(c => c.Value.Handlers.Count == 0).ToList())
            {
                // Remove, not Unsubscribe: Unsubscribe leaves the channel in the client's topic
                // cache, so the next subscribe to the same filter gets handed the dead one back and
                // tries to rejoin a channel whose leave may still be in flight — which is how a feed
                // ends up silently closed.
                _client?.Remove(entry.Channel);
                _channels.Remove(key);
                Debug.WriteLine($"[Realtime] unsubscribed from {key}");
            }

            if (_channels.Count == 0 && _client is not null)
            {
                _client.Disconnect();
                _client = null;
                Debug.WriteLine("[Realtime] disconnected — nothing left subscribed");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Realtime] sweep threw: {ex.Message}");
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<Client> EnsureConnectedAsync()
    {
        var realtimeUrl = $"{SupabaseConfig.ProjectUrl.Replace("https://", "wss://")}/realtime/v1";
        var token = await _authService.GetAccessTokenAsync();

        if (_client is not null)
        {
            // The socket outlives the access token it was opened with. Supabase authorises a
            // Postgres Changes channel against the token it holds, so a socket left running across
            // a refresh goes quiet on RLS-filtered feeds; pushing the current token on every
            // subscribe keeps it delivering.
            if (!string.IsNullOrEmpty(token))
                _client.SetAuth(token);

            return _client;
        }

        Client CreateClient()
        {
            var created = new Client(realtimeUrl, new ClientOptions())
            {
                GetHeaders = () => new Dictionary<string, string> { ["apikey"] = SupabaseConfig.AnonKey },
            };

            if (!string.IsNullOrEmpty(token))
                created.SetAuth(token);

            return created;
        }

        // ClientWebSocket's connect/handshake occasionally NREs on a cold-start Android socket
        // race (underlying platform bug, not ours) — one retry after a short delay clears it.
        // A failed client/socket can't be reused (its background reconnect loop keeps running
        // and races a second ConnectAsync on the same instance), so build a fresh one to retry.
        var client = CreateClient();
        try
        {
            await client.ConnectAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Realtime] connect failed, retrying once: {ex.Message}");
            await Task.Delay(500);
            client = CreateClient();
            await client.ConnectAsync();
        }

        Debug.WriteLine("[Realtime] connected");
        _client = client;
        return client;
    }
}
