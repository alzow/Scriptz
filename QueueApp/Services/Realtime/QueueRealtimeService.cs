using System.Diagnostics;
using Supabase.Realtime;
using Supabase.Realtime.PostgresChanges;
using QueueApp.Constants;
using QueueApp.Services.Auth;

namespace QueueApp.Services.Realtime;

// The one piece of the Queue feature that isn't Refit — Realtime is a WebSocket
// subscription, not request/response. Filtered to a single business_id so a
// device only ever receives changes for its own shop's queue.
public class QueueRealtimeService : IQueueRealtimeService
{
    private readonly IAuthService _authService;
    private Client? _client;
    private RealtimeChannel? _channel;

    public QueueRealtimeService(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task SubscribeAsync(string filterColumn, string filterValue, Func<Task> onChange, string table = "queue_entries")
    {
        var realtimeUrl = $"{SupabaseConfig.ProjectUrl.Replace("https://", "wss://")}/realtime/v1";
        var token = await _authService.GetAccessTokenAsync();

        Client CreateClient()
        {
            var c = new Client(realtimeUrl, new ClientOptions())
            {
                GetHeaders = () => new Dictionary<string, string> { ["apikey"] = SupabaseConfig.AnonKey },
            };
            if (!string.IsNullOrEmpty(token))
                c.SetAuth(token);
            return c;
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

        _channel = _client.Channel("realtime", "public", table, filterColumn, filterValue, null!);
        _channel.AddPostgresChangeHandler(PostgresChangesOptions.ListenType.All, async (_, _) =>
        {
            Debug.WriteLine($"[Realtime] event received {DateTime.Now:HH:mm:ss}");
            await onChange();
        });

        await _channel.Subscribe();
        Debug.WriteLine($"[Realtime] subscribed to {table} where {filterColumn}={filterValue}");
    }

    public Task UnsubscribeAsync()
    {
        _channel?.Unsubscribe();
        _client?.Disconnect();
        _channel = null;
        _client = null;
        Debug.WriteLine($"[Realtime] unsubscribed from business");
        return Task.CompletedTask;
    }
}
