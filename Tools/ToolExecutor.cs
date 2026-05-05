using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using AshBot.Memory;
using AshBot.State;
using Discord;
using Discord.WebSocket;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using YoutubeExplode;

namespace AshBot.Tools;

public class ToolExecutor
{
    private readonly AshConfig _cfg;
    private readonly DiscordSocketClient _discord;
    private readonly MemoryManager _memory;
    private readonly AutonomousMessageQueue _queue;
    private readonly AI.OllamaClient _ollama;
    private readonly IHttpClientFactory _http;
    private readonly ILogger<ToolExecutor> _log;
    private readonly DateTime _startTime = DateTime.UtcNow;
    private readonly YoutubeClient _yt = new();

    public ToolExecutor(AshConfig cfg, DiscordSocketClient discord, MemoryManager memory,
        AutonomousMessageQueue queue, AI.OllamaClient ollama, IHttpClientFactory http, ILogger<ToolExecutor> log)
    {
        _cfg = cfg; _discord = discord; _memory = memory;
        _queue = queue; _ollama = ollama; _http = http; _log = log;
    }

    public async Task<object> ExecuteAsync(string name, JsonElement args, IMessageChannel? channel = null)
    {
        try
        {
            string Str(string key, string def = "") => args.TryGetProperty(key, out var v) ? v.GetString() ?? def : def;
            int Int(string key, int def = 0) => args.TryGetProperty(key, out var v) ? v.GetInt32() : def;
            double Dbl(string key, double def = 0) => args.TryGetProperty(key, out var v) ? v.GetDouble() : def;

            switch (name)
            {
                case "manage_reaction":
                {
                    if (channel is null) return new { error = "No channel" };
                    var midStr = Str("message_id");
                    if (!ulong.TryParse(midStr, out var mid))
                        return new { error = $"Invalid message_id '{midStr}' — must be a numeric Discord snowflake ID, not a placeholder." };
                    var msg = await channel.GetMessageAsync(mid);
                    if (msg is null) return new { error = $"Message {mid} not found in channel." };
                    var emoji = new Emoji(Str("emoji"));
                    if (Str("action") == "remove" && msg is IUserMessage um)
                        await um.RemoveReactionAsync(emoji, _discord.CurrentUser);
                    else if (msg is IUserMessage um2)
                        await um2.AddReactionAsync(emoji);
                    return new { success = true };
                }

                case "send_dm":
                {
                    var uidStr = Str("user_id");
                    if (!ulong.TryParse(uidStr, out var uid))
                        return new { error = $"Invalid user_id '{uidStr}' — must be a numeric Discord snowflake ID." };
                    var user = await _discord.GetUserAsync(uid);
                    if (user is null) return new { error = $"User {uid} not found." };
                    var dm = await user.CreateDMChannelAsync();
                    await dm.SendMessageAsync(Str("content"));
                    return new { success = true };
                }

                case "send_embed":
                {
                    if (channel is null) return new { error = "No channel" };
                    var color = args.TryGetProperty("color", out var cv) ? new Color((uint)cv.GetInt32()) : new Color(0x7289da);
                    var embed = new EmbedBuilder()
                        .WithTitle(Str("title"))
                        .WithDescription(Str("description"))
                        .WithColor(color)
                        .Build();
                    await channel.SendMessageAsync(embed: embed);
                    return new { success = true };
                }

                case "send_autonomous_message":
                {
                    var cid = channel?.Id ?? _cfg.ChannelId;
                    await _queue.QueueMessage(cid, Str("content"), (int)Dbl("delay"));
                    return new { success = true };
                }

                case "get_user_info":
                {
                    var uidStr2 = Str("user_id");
                    if (!ulong.TryParse(uidStr2, out var uid2))
                        return new { error = $"Invalid user_id '{uidStr2}' — must be a numeric Discord snowflake ID." };
                    var user = await _discord.GetUserAsync(uid2);
                    if (user is null) return new { error = $"User {uid2} not found." };
                    return new { name = user.Username, id = user.Id.ToString() };
                }

                case "read_channel_history":
                {
                    if (channel is null) return new { error = "No channel" };
                    var limit = Int("limit", 10);
                    var msgs = new List<object>();
                    await foreach (var page in channel.GetMessagesAsync(limit))
                        foreach (var m in page)
                            msgs.Add(new { author = m.Author.Username, content = m.Content });
                    return new { messages = msgs };
                }

                case "search_yt_music":
                {
                    var query = Str("query");
                    var results = new List<object>();
                    var count = 0;
                    await foreach (var r in _yt.Search.GetVideosAsync(query))
                    {
                        if (count++ >= 5) break;
                        results.Add(new { title = r.Title, artist = r.Author.ChannelTitle, url = $"https://music.youtube.com/watch?v={r.Id}" });
                    }
                    return new { results };
                }

                case "read_file":
                {
                    var p = Path.GetFullPath(Str("file_path"));
                    if (!p.Contains("ash-bot")) return new { error = "Access denied" };
                    if (!File.Exists(p)) return new { error = "File not found" };
                    return new { content = File.ReadAllText(p)[..Math.Min(5000, (int)new FileInfo(p).Length)] };
                }

                case "list_files":
                {
                    var p = Path.GetFullPath(Str("path"));
                    if (!p.Contains("ash-bot")) return new { error = "Access denied" };
                    if (!Directory.Exists(p)) return new { error = "Directory not found" };
                    var files = Directory.GetFiles(p, "*", SearchOption.AllDirectories)
                        .Select(f => Path.GetRelativePath(p, f)).Take(50);
                    return new { files };
                }

                case "write_file":
                {
                    var workspace = Path.GetFullPath(_cfg.WorkspacePath);
                    Directory.CreateDirectory(workspace);
                    var p = Path.Combine(workspace, Str("filename"));
                    var mode = Str("mode", "write");
                    if (mode == "append") File.AppendAllText(p, "\n" + Str("content"));
                    else File.WriteAllText(p, Str("content"));
                    return new { success = true };
                }

                case "memory_search":
                    return new { results = _memory.SearchMemories(Str("query")) };

                case "memory_get":
                {
                    var key = Str("key");
                    var person = _memory.FindPersonById(key) ?? _memory.FindPersonByName(key);
                    if (person is not null) return new { found = true, profile = person };
                    var fallback = _memory.SearchMemories(key, 3);
                    return new { found = false, closest_matches = fallback };
                }

                case "memory_update":
                    return new { result = _memory.AddPersonMemory(Str("person"), Str("memory")) };

                case "schedule_task":
                {
                    var delaySecs = (int)(Dbl("delay_minutes") * 60);
                    var cid = channel?.Id ?? _cfg.ChannelId;
                    await _queue.QueueMessage(cid, Str("message"), delaySecs);
                    return new { success = true, scheduled_in_minutes = Dbl("delay_minutes") };
                }

                case "check_health":
                {
                    var proc = Process.GetCurrentProcess();
                    var uptime = (int)(DateTime.UtcNow - _startTime).TotalMinutes;
                    var ramMb = (int)(proc.WorkingSet64 / 1024 / 1024);
                    var ollamaOk = await _ollama.IsAvailableAsync();
                    return new
                    {
                        uptime_minutes = uptime, ram_mb = ramMb,
                        discord_connected = _discord.ConnectionState == ConnectionState.Connected,
                        ollama_running = ollamaOk,
                        pending_messages = _queue.PendingCount,
                        tool_count = ToolDefinitions.All.Count
                    };
                }

                case "run_code":
                {
                    var code = Str("code");
                    var timeout = Math.Min(Int("timeout", 10), 30);
                    var tmp = Path.GetTempFileName() + ".csx";
                    File.WriteAllText(tmp, code);
                    try
                    {
                        // Use dotnet-script or Roslyn scripting
                        var result = await RunProcessAsync("dotnet-script", tmp, timeout);
                        return new { stdout = result.stdout[..Math.Min(2000, result.stdout.Length)], stderr = result.stderr[..Math.Min(500, result.stderr.Length)], returncode = result.exitCode };
                    }
                    catch (TimeoutException)
                    {
                        return new { error = $"Code timed out after {timeout}s" };
                    }
                    finally { File.Delete(tmp); }
                }

                case "web_browse":
                {
                    var url = Str("url");
                    var focus = Str("prompt");
                    var text = await FetchPageText(url, 6000);
                    if (!string.IsNullOrEmpty(focus))
                    {
                        var focusMsg = new AshBot.AI.OllamaMessage("user", $"From this page, focus on: {focus}\n\n{text}");
                        var resp = await _ollama.ChatAsync([focusMsg]);
                        return new { url, content = resp.Content };
                    }
                    return new { url, content = text };
                }

                case "deep_research":
                {
                    var topic = Str("topic");
                    _log.LogInformation("🔍 [DEEP RESEARCH] Ash is researching: {Topic}", topic);
                    var searchResults = await DuckDuckGoSearch(topic, 3);
                    var tasks = searchResults.Select(async r =>
                    {
                        try
                        {
                            var text = await FetchPageText(r.Url, 2500);
                            return (object)new { url = r.Url, title = r.Title, content = text };
                        }
                        catch { return (object)new { url = r.Url, title = r.Title, content = "" }; }
                    });
                    var pages = await Task.WhenAll(tasks);
                    var synthPrompt = $"You are a technical analyst. Synthesize a report on '{topic}' based on these sources:\n\n{JsonSerializer.Serialize(pages)}\n\nProvide actionable insights and clear summaries.";
                    var synthMsg = new AshBot.AI.OllamaMessage("user", synthPrompt);
                    var resp2 = await _ollama.ChatAsync([synthMsg]);
                    return new { topic, report = resp2.Content, sources = searchResults.Select(r => r.Url) };
                }

                case "view_image":
                {
                    using var http = _http.CreateClient();
                    var bytes = await http.GetByteArrayAsync(Str("image_url"));
                    var desc = await _ollama.DescribeImageAsync(bytes, Str("prompt", "Describe this image"));
                    return new { description = desc };
                }

                case "patch_core":
                    // In C# we can't safely patch compiled source at runtime.
                    // Instead, allow patching personality/config files.
                    return new { error = "patch_core is not supported in the C# runtime. Use write_file to modify personality or config files, then use reboot_ash to restart." };

                case "restore_backup":
                    return new { error = "restore_backup is not supported in the C# runtime." };

                case "patch_requirements":
                    return new { error = "patch_requirements is not supported in the C# runtime. NuGet packages are managed at build time." };

                case "install_library":
                    return new { error = "install_library is not supported in the C# runtime. Add NuGet packages to AshBot.csproj instead." };

                case "list_backups":
                    return new { backups = Array.Empty<object>(), note = "Backup system not applicable in C# runtime." };

                case "reboot_ash":
                {
                    var reason = Str("reason", "No reason given");
                    _log.LogWarning("🔄 Reboot requested: {Reason}", reason);
                    // Restart via the start-ash batch file if it exists, otherwise just re-exec
                    var batPath = Path.Combine(AppContext.BaseDirectory, "start-ash.bat");
                    if (File.Exists(batPath))
                        Process.Start(new ProcessStartInfo("cmd.exe", $"/c start \"\" \"{batPath}\"") { CreateNoWindow = true });
                    else
                        Process.Start(new ProcessStartInfo(Environment.ProcessPath ?? "AshBot") { UseShellExecute = true });
                    Environment.Exit(0);
                    return new { success = true };
                }

                default:
                    return new { error = $"Unknown tool: {name}" };
            }
        }
        catch (Exception ex)
        {
            _log.LogError("Tool '{Name}' failed: {Ex}", name, ex.Message);
            return new { error = ex.Message };
        }
    }

    private async Task<string> FetchPageText(string url, int maxChars)
    {
        using var http = _http.CreateClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AshBot/1.0");
        var html = await http.GetStringAsync(url);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var removeNodes = doc.DocumentNode.SelectNodes("//script|//style|//nav|//footer|//header");
        if (removeNodes != null)
            foreach (var node in removeNodes.ToList())
                node.Remove();
        var text = doc.DocumentNode.InnerText;
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
        return text[..Math.Min(maxChars, text.Length)];
    }

    private record SearchResult(string Title, string Url, string Snippet);

    private async Task<List<SearchResult>> DuckDuckGoSearch(string query, int maxResults)
    {
        using var http = _http.CreateClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AshBot/1.0");

        // DuckDuckGo HTML search — no API key required
        var url = $"https://html.duckduckgo.com/html/?q={Uri.EscapeDataString(query)}";
        var html = await http.GetStringAsync(url);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var results = new List<SearchResult>();
        var resultNodes = doc.DocumentNode.SelectNodes("//div[@class='result__body']");
        if (resultNodes == null) return results;
        foreach (var node in resultNodes)
        {
            var titleNode = node.SelectSingleNode(".//a[@class='result__a']");
            var snippetNode = node.SelectSingleNode(".//a[@class='result__snippet']");
            var href = titleNode?.GetAttributeValue("href", "") ?? "";
            // DDG wraps URLs; extract uddg param
            if (href.Contains("uddg="))
            {
                // Extract uddg parameter manually (no System.Web dependency)
                var match = System.Text.RegularExpressions.Regex.Match(href, @"[?&]uddg=([^&]+)");
                if (match.Success) href = Uri.UnescapeDataString(match.Groups[1].Value);
            }
            if (!string.IsNullOrEmpty(href))
            {
                results.Add(new SearchResult(
                    titleNode?.InnerText.Trim() ?? "",
                    href,
                    snippetNode?.InnerText.Trim() ?? ""));
                if (results.Count >= maxResults) break;
            }
        }
        return results;
    }

    private async Task<(string stdout, string stderr, int exitCode)> RunProcessAsync(string exe, string args, int timeoutSecs)
    {
        var psi = new ProcessStartInfo(exe, args)
        {
            RedirectStandardOutput = true, RedirectStandardError = true,
            UseShellExecute = false, CreateNoWindow = true
        };
        using var proc = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start process");
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSecs));
        try
        {
            var stdoutTask = proc.StandardOutput.ReadToEndAsync(cts.Token);
            var stderrTask = proc.StandardError.ReadToEndAsync(cts.Token);
            await proc.WaitForExitAsync(cts.Token);
            return (await stdoutTask, await stderrTask, proc.ExitCode);
        }
        catch (OperationCanceledException)
        {
            proc.Kill(true);
            throw new TimeoutException();
        }
    }
}
