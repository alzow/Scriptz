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

    public QueueRealtimeService(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task SubscribeAsync(object owner, string filterColumn, string filterValue, Func<Task> onChange, string table = "queue_entries")
    {
        await _gate.WaitAsync();
        try
        {
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
                entry = new ChannelEntry(channel);
                _channels[key] = entry;

                channel.AddPostgresChangeHandler(PostgresChangesOptions.ListenType.All, async (_, _) =>
                {
                    Debug.WriteLine($"[Realtime] event on {key} {DateTime.Now:HH:mm:ss}");
                    await entry.NotifyAsync();
                });

                await channel.Subscribe();
                Debug.WriteLine($"[Realtime] subscribed to {key}");
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

            if (_owners.Count == 0 && _client is not null)
            {
                _client.Disconnect();
                _client = null;
                Debug.WriteLine("[Realtime] disconnected — nothing left subscribed");
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private void DetachOwner((object Owner, string Table) ownerKey)
    {
        if (!_owners.Remove(ownerKey, out var registration))
            return;

        if (!_channels.TryGetValue(registration.ChannelKey, out var entry))
            return;

        entry.Handlers.Remove(registration.OnChange);

        if (entry.Handlers.Count > 0)
            return;

        entry.Channel.Unsubscribe();
        _channels.Remove(registration.ChannelKey);
        Debug.WriteLine($"[Realtime] unsubscribed from {registration.ChannelKey}");
    }

    private async Task<Client> EnsureConnectedAsync()
    {
        if (_client is not null)
            return _client;

        var realtimeUrl = $"{SupabaseConfig.ProjectUrl.Replace("https://", "wss://")}/realtime/v1";
        var token = await _authService.GetAccessTokenAsync();

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
