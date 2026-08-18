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

    public async Task SubscribeAsync(Guid businessId, Func<Task> onChange)
    {
        var realtimeUrl = $"{SupabaseConfig.ProjectUrl.Replace("https://", "wss://")}/realtime/v1";

        _client = new Client(realtimeUrl, new ClientOptions())
        {
            GetHeaders = () => new Dictionary<string, string> { ["apikey"] = SupabaseConfig.AnonKey },
        };

        var token = await _authService.GetAccessTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            _client.SetAuth(token);
        }

        await _client.ConnectAsync();
        Debug.WriteLine("[Realtime] connected");

        _channel = _client.Channel("realtime", "public", "queue_entries", "business_id", businessId.ToString(), null!);
        _channel.AddPostgresChangeHandler(PostgresChangesOptions.ListenType.All, async (_, _) =>
        {
            Debug.WriteLine($"[Realtime] event received {DateTime.Now:HH:mm:ss}");
            await onChange();
        });

        await _channel.Subscribe();
        Debug.WriteLine($"[Realtime] subscribed to business {businessId}");
    }

    public Task UnsubscribeAsync()
    {
        _channel?.Unsubscribe();
        _client?.Disconnect();
        _channel = null;
        _client = null;
        return Task.CompletedTask;
    }
}
