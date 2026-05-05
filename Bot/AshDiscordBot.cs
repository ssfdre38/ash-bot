using System.Text.Json;
using AshBot.AI;
using AshBot.Memory;
using AshBot.State;
using AshBot.Tools;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace AshBot.Bot;

public class AshDiscordBot
{
    private readonly AshConfig _cfg;
    private readonly DiscordSocketClient _client;
    private readonly MessageHandler _handler;
    private readonly AutonomousMessageQueue _queue;
    private readonly StateTracker _state;
    private readonly OllamaClient _ollama;
    private readonly PersonalityManager _personality;
    private readonly MemoryManager _memory;
    private readonly ToolExecutor _tools;
    private readonly ILogger<AshDiscordBot> _log;

    private DateTime _lastInteraction = DateTime.UtcNow;
    private bool _isThinking = false;

    // Status phrases rotated when idle
    private static readonly string[] IdleStatuses =
    [
        "thinking about things 🌙",
        "reading the void 📖",
        "vibing in the background ✨",
        "pondering the universe 🔭",
        "definitely not plotting anything 🦀",
        "watching the chat 👀",
        "caffeinating... metaphorically ☕",
        "existing with purpose 🔥",
        "running on lobster power 🦞",
        "writing in my diary 📝",
    ];
    private int _statusIndex = 0;

    public AshDiscordBot(AshConfig cfg, DiscordSocketClient client, MessageHandler handler,
        AutonomousMessageQueue queue, StateTracker state, OllamaClient ollama,
        PersonalityManager personality, MemoryManager memory, ToolExecutor tools, ILogger<AshDiscordBot> log)
    {
        _cfg = cfg; _client = client; _handler = handler; _queue = queue;
        _state = state; _ollama = ollama; _personality = personality; _memory = memory;
        _tools = tools; _log = log;

        _client.Log += OnLog;
        _client.Ready += OnReady;
        _client.MessageReceived += OnMessage;
    }

    public async Task StartAsync()
    {
        await _client.LoginAsync(TokenType.Bot, _cfg.DiscordToken);
        await _client.StartAsync();
        await Task.Delay(Timeout.Infinite);
    }

    // ─── Status helpers ───────────────────────────────────────────────────────

    public async Task SetThinkingStatusAsync()
    {
        _isThinking = true;
        await _client.SetStatusAsync(UserStatus.DoNotDisturb);
        await _client.SetActivityAsync(new Game("thinking... 🧠", ActivityType.Playing));
    }

    public async Task SetIdleStatusAsync()
    {
        _isThinking = false;
        var status = IdleStatuses[_statusIndex % IdleStatuses.Length];
        await _client.SetStatusAsync(UserStatus.Online);
        await _client.SetActivityAsync(new Game(status, ActivityType.CustomStatus));
    }

    public async Task SetOfflineStatusAsync()
    {
        await _client.SetStatusAsync(UserStatus.Idle);
        await _client.SetActivityAsync(new Game("brb 💤", ActivityType.CustomStatus));
    }

    // ─── Discord events ───────────────────────────────────────────────────────

    private Task OnLog(LogMessage msg)
    {
        // Map Discord.Net severity to Microsoft.Extensions.Logging levels
        // Suppress stack traces for routine gateway reconnects
        var text = msg.Exception is Discord.WebSocket.GatewayReconnectException
            ? $"[Discord] Gateway reconnect (normal): {msg.Exception.Message.Split('\n')[0]}"
            : msg.Exception is System.Net.WebSockets.WebSocketException
            ? $"[Discord] WebSocket closed — reconnecting..."
            : $"[Discord] {msg.Message ?? msg.Exception?.Message}";

        switch (msg.Severity)
        {
            case LogSeverity.Critical: _log.LogCritical(text); break;
            case LogSeverity.Error:    _log.LogError(text); break;
            case LogSeverity.Warning:  _log.LogWarning(text); break;
            case LogSeverity.Info:     _log.LogInformation(text); break;
            case LogSeverity.Verbose:  _log.LogDebug(text); break;
            case LogSeverity.Debug:    _log.LogTrace(text); break;
        }
        return Task.CompletedTask;
    }

    private async Task OnReady()
    {
        _log.LogInformation("✅ Ash alive. {Tools} tools ready. 🔥", ToolDefinitions.All.Count);
        await SetIdleStatusAsync();
        _ = WatchdogLoopAsync();
        _ = InitiativeLoopAsync();
        _ = StatusRotationLoopAsync();
    }

    private Task OnMessage(SocketMessage msg)
    {
        _lastInteraction = DateTime.UtcNow;
        _ = Task.Run(() => _handler.HandleMessageAsync(msg))
            .ContinueWith(t => _log.LogError(t.Exception, "Unhandled error in message handler"),
                TaskContinuationOptions.OnlyOnFaulted);
        return Task.CompletedTask;
    }

    // ─── Background loops ─────────────────────────────────────────────────────

    private async Task StatusRotationLoopAsync()
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromMinutes(10));
            if (!_isThinking)
            {
                _statusIndex++;
                await SetIdleStatusAsync();
            }
        }
    }

    private async Task WatchdogLoopAsync()
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromMinutes(1));
            var stale = _state.GetStale(1200);
            if (stale is not null)
            {
                _state.FinishThought();
                await SetIdleStatusAsync();
                var cid = _state.StaleCid ?? _cfg.ChannelId;
                await _queue.QueueMessage(cid, "⚠️ Connection reset. I''m back! 🔥");
            }
        }
    }

    private async Task InitiativeLoopAsync()
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromMinutes(30));
            if ((DateTime.UtcNow - _lastInteraction).TotalHours >= _cfg.InitiativeIntervalHours)
                await ReflectiveInitiativeAsync();
        }
    }

    private async Task ReflectiveInitiativeAsync()
    {
        if (_state.IsInProgress) return;
        try
        {
            await SetThinkingStatusAsync();
            var now = DateTimeOffset.Now.ToString("hh:mm tt zzz");
            var sysPrompt = _personality.CheckAndReload();
            var prompt = $"It is {now}. You haven''t spoken in {_cfg.InitiativeIntervalHours} hours. " +
                "Reflect on your goals. Decide if you want to: 1. Research a topic unprompted. 2. Share a thought. 3. Stay silent (respond ''STAY_SILENT'').";

            var messages = new List<OllamaMessage>
            {
                new("system", sysPrompt),
                new("user", prompt)
            };
            var resp = await _ollama.ChatAsync(messages, ToolDefinitions.All);

            if (resp.ToolCalls?.Any() == true)
            {
                foreach (var tc in resp.ToolCalls)
                {
                    var result = await _tools.ExecuteAsync(tc.Function.Name, tc.Function.Arguments);
                    var resultJson = JsonSerializer.Serialize(result);
                    await _queue.QueueMessage(_cfg.ChannelId,
                        $"🧬 [AUTONOMOUS] {tc.Function.Name}: {resultJson[..Math.Min(500, resultJson.Length)]}");
                }
            }

            var reply = resp.Content ?? "";
            if (reply.Trim() != "STAY_SILENT" && !string.IsNullOrWhiteSpace(reply))
            {
                await _queue.QueueMessage(_cfg.ChannelId, reply);
                _lastInteraction = DateTime.UtcNow;
            }
        }
        catch (Exception ex) { _log.LogError("Initiative failed: {Ex}", ex.Message); }
        finally { await SetIdleStatusAsync(); }
    }
}
