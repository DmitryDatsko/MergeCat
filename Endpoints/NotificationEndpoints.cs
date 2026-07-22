using System.Text.Json;
using MergeCat.Services;

namespace MergeCat.Endpoints;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("api/notifications/stream", StreamNotifications).RequireAuthorization();
    }

    private static async Task StreamNotifications(
        HttpContext ctx,
        PurchaseNotificationHub hub,
        CancellationToken ct
    )
    {
        var walletAddress = ctx.User.FindFirst("address")?.Value?.ToLower();
        if (string.IsNullOrEmpty(walletAddress))
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        ctx.Response.Headers.ContentType = "text/event-stream";
        ctx.Response.Headers.CacheControl = "no-cache";
        ctx.Response.Headers.Connection = "keep-alive";

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            ct,
            ctx.RequestAborted
        );
        var reader = hub.Subscribe(walletAddress, linkedCts.Token);

        try
        {
            while (!linkedCts.IsCancellationRequested)
            {
                var readTask = reader.WaitToReadAsync(linkedCts.Token).AsTask();
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(15), linkedCts.Token);

                var completed = await Task.WhenAny(readTask, timeoutTask);

                if (completed == timeoutTask)
                {
                    await ctx.Response.WriteAsync(": hearbeat\n\n", linkedCts.Token);
                    await ctx.Response.Body.FlushAsync(linkedCts.Token);
                    continue;
                }

                if (!await readTask)
                    break;

                while (reader.TryRead(out var payload))
                {
                    var json = JsonSerializer.Serialize(payload);
                    await ctx.Response.WriteAsync(
                        $"event: purchase\ndata: {json}\n\n",
                        linkedCts.Token
                    );
                    await ctx.Response.Body.FlushAsync(linkedCts.Token);
                }
            }
        }
        catch (OperationCanceledException) { }
    }
}
