using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace AshBot.State;

public class AutonomousMessageQueue
{
    private record QueueItem(ulong ChannelId, string Text, int DelaySeconds);

    private readonly Queue<QueueItem> _queue = new();
    private bool _processing;
    private readonly DiscordSocketClient _client;
    private readonly ILogger<AutonomousMessageQueue> _log;

    public AutonomousMessageQueue(DiscordSocketClient client, ILogger<AutonomousMessageQueue> log)
    {
        _client = client;
        _log = log;
    }

    public int PendingCount => _queue.Count;

    public async Task QueueMessage(ulong channelId, string text, int delaySeconds = 0)
    {
        _queue.Enqueue(new QueueItem(channelId, text, delaySeconds));
        _log.LogInformation("📬 Message queued for autonomous sending");
        if (!_processing) _ = ProcessAsync();
        await Task.CompletedTask;
    }

    private async Task ProcessAsync()
    {
        _processing = true;
        while (_queue.TryDequeue(out var item))
        {
            if (item.DelaySeconds > 0) await Task.Delay(item.DelaySeconds * 1000);
            try
            {
                var channel = _client.GetChannel(item.ChannelId) as IMessageChannel
                    ?? await _client.Rest.GetChannelAsync(item.ChannelId) as IMessageChannel;
                if (channel is not null)
                {
                    for (int i = 0; i < item.Text.Length; i += 2000)
                    {
                        await channel.SendMessageAsync(item.Text[i..Math.Min(i + 2000, item.Text.Length)]);
                        if (i + 2000 < item.Text.Length) await Task.Delay(500);
                    }
                }
            }
            catch (Exception ex) { _log.LogError("Autonomous send failed: {Ex}", ex.Message); }
        }
        _processing = false;
    }
}
