using System.Text.Json;
using System.Text.Json.Nodes;
using AshBot.AI;
using AshBot.Memory;
using AshBot.State;
using AshBot.Tools;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace AshBot.Bot;

public class MessageHandler
{
    private readonly AshConfig _cfg;
    private readonly DiscordSocketClient _client;
    private readonly OllamaClient _ollama;
    private readonly MemoryManager _memory;
    private readonly PersonalityManager _personality;
    private readonly AutonomousMessageQueue _queue;
    private readonly StateTracker _state;
    private readonly ToolExecutor _tools;
    private readonly ILogger<MessageHandler> _log;

    // Set by AshDiscordBot after construction to avoid circular dependency
    public AshDiscordBot? Bot { get; set; }

    // Per-channel conversation history
    private readonly Dictionary<ulong, List<OllamaMessage>> _history = new();

    public MessageHandler(AshConfig cfg, DiscordSocketClient client, OllamaClient ollama,
        MemoryManager memory, PersonalityManager personality, AutonomousMessageQueue queue,
        StateTracker state, ToolExecutor tools, ILogger<MessageHandler> log)
    {
        _cfg = cfg; _client = client; _ollama = ollama; _memory = memory;
        _personality = personality; _queue = queue; _state = state; _tools = tools; _log = log;
    }

    public async Task HandleMessageAsync(SocketMessage rawMsg)
    {
        if (rawMsg is not SocketUserMessage msg) return;
        if (msg.Author.Id == _client.CurrentUser.Id) return;

        var isDm = msg.Channel is IDMChannel;
        if (!isDm && msg.Channel.Id != _cfg.ChannelId) return;

        if (isDm) _log.LogInformation("[DM] Private message from {User}", msg.Author.Username);

        _state.StartThought(msg.Id, msg.Channel.Id, msg.Author.Username,
            isDm ? "[DM]" : msg.Content);

        using (msg.Channel.EnterTypingState())
        {
            try
            {
                if (Bot is not null) await Bot.SetThinkingStatusAsync();
                // Track user in memory
                _memory.UpdateOrAddPerson(msg.Author.Id.ToString(), msg.Author.Username);

                var sysPrompt = BuildSystemPrompt(msg, isDm);
                var history = GetHistory(msg.Channel.Id);

                var displayName = (msg.Author as SocketGuildUser)?.DisplayName ?? msg.Author.Username;
                var userContent = $"[{displayName}]: {msg.Content}";
                var userMsg = new OllamaMessage("user", userContent);

                var messages = new List<OllamaMessage> { new OllamaMessage("system", sysPrompt) };
                messages.AddRange(history);
                messages.Add(userMsg);

                // Multi-turn tool loop (max 5 turns)
                var resp = await _ollama.ChatAsync(messages, ToolDefinitions.All);
                int turns = 0;

                while (resp.ToolCalls?.Any() == true && turns < 5)
                {
                    turns++;
                    messages.Add(resp);

                    var toolResults = await Task.WhenAll(
                        resp.ToolCalls.Select(tc => _tools.ExecuteAsync(
                            tc.Function.Name,
                            tc.Function.Arguments,
                            msg.Channel as IMessageChannel)));

                    foreach (var result in toolResults)
                        messages.Add(new OllamaMessage("tool", JsonSerializer.Serialize(result)));

                    resp = await _ollama.ChatAsync(messages, ToolDefinitions.All);
                }

                var reply = resp.Content ?? "Done. 🔥";

                // Split on double-newlines (natural paragraph breaks)
                var blocks = reply.Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var block in blocks)
                {
                    if (block.Length <= 2000)
                        await msg.Channel.SendMessageAsync(block);
                    else
                        for (int i = 0; i < block.Length; i += 2000)
                            await msg.Channel.SendMessageAsync(block[i..Math.Min(i + 2000, block.Length)]);
                    await Task.Delay(800);
                }

                // Update history
                history.Add(userMsg);
                history.Add(new OllamaMessage("assistant", reply));
                var maxTurns = isDm ? 6 : _cfg.MaxHistory;
                while (history.Count > maxTurns * 2) history.RemoveAt(0);

                _state.FinishThought();
                if (Bot is not null) await Bot.SetIdleStatusAsync();
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error handling message");
                await msg.Channel.SendMessageAsync("🔥 Error processing.");
                _state.FinishThought();
                if (Bot is not null) await Bot.SetIdleStatusAsync();
            }
        }
    }

    private string BuildSystemPrompt(SocketUserMessage msg, bool isDm)
    {
        var personality = _personality.CheckAndReload();
        var memorySummary = _memory.GetSummary();
        var relevantCtx = _memory.GetRelevantContext(msg.Content, msg.Author.Username, msg.Author.Id.ToString());
        var now = DateTimeOffset.Now.ToString("dddd, MMMM dd, yyyy — hh:mm tt zzz");

        var sb = new System.Text.StringBuilder();
        sb.Append(personality);
        sb.AppendLine("\n\n---");
        sb.AppendLine("YOUR MEMORIES:");
        sb.AppendLine(memorySummary);
        if (!string.IsNullOrEmpty(relevantCtx))
        {
            sb.AppendLine("\n## Relevant Context");
            sb.AppendLine(relevantCtx);
        }
        sb.AppendLine("\n---");
        sb.AppendLine($"CURRENT TIME: {now}");
        sb.AppendLine("SOVEREIGNTY: You can use all your tools. run_code executes C# scripts. write_file edits personality/config files. reboot_ash restarts you.");
        sb.AppendLine("PRIVACY: You can respond in DMs for private sessions.");
        sb.AppendLine("HEARTBEAT: You reflect every 4 hours of silence.");

        if (isDm)
        {
            sb.AppendLine("\n[PRIVACY MODE — DM SESSION]");
            sb.AppendLine("- This is a private conversation. Treat it with full confidentiality.");
            sb.AppendLine("- Never reference or hint at DM content in public channels.");
            sb.AppendLine("- Do not log or summarize this conversation unless explicitly asked.");
        }

        return sb.ToString();
    }

    private List<OllamaMessage> GetHistory(ulong channelId)
    {
        if (!_history.TryGetValue(channelId, out var h))
            _history[channelId] = h = new List<OllamaMessage>();
        return h;
    }
}
