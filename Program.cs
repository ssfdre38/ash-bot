using AshBot;
using AshBot.AI;
using AshBot.Bot;
using AshBot.Codex;
using AshBot.Memory;
using AshBot.State;
using AshBot.Tools;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OllamaClient = AshBot.AI.OllamaClient;

// ─── Singleton lock (prevent duplicate instances) ───────────────────────────
const string MutexName = "Global\\AshBotSingleton";
using var mutex = new Mutex(true, MutexName, out var isNewInstance);
if (!isNewInstance)
{
    Console.Error.WriteLine("❌ Another instance of AshBot is already running.");
    return;
}

Console.WriteLine("🚀 Starting Ash (C# edition)...");

// ─── Config ──────────────────────────────────────────────────────────────────
var cfg = AshConfig.Load();
if (string.IsNullOrWhiteSpace(cfg.DiscordToken) || cfg.DiscordToken == "YOUR_DISCORD_TOKEN_HERE")
{
    Console.Error.WriteLine("❌ Discord token not set. Edit appsettings.json or set ASH_DISCORD_TOKEN env var.");
    return;
}

// ─── Ensure workspace dirs exist ─────────────────────────────────────────────
Directory.CreateDirectory(cfg.WorkspacePath);
Directory.CreateDirectory(cfg.PersonalityPath);

// ─── Services ────────────────────────────────────────────────────────────────
var services = new ServiceCollection()
    .AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Information))
    .AddHttpClient()
    .AddSingleton(cfg)
    .AddSingleton(_ =>
    {
        var config = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds
                           | GatewayIntents.GuildMessages
                           | GatewayIntents.GuildMembers
                           | GatewayIntents.MessageContent
                           | GatewayIntents.DirectMessages,
            LogLevel = LogSeverity.Warning   // suppress routine gateway noise
        };
        return new DiscordSocketClient(config);
    })
    .AddSingleton(sp => new OllamaClient(cfg.OllamaUrl, cfg.OllamaModel, sp.GetRequiredService<ILogger<OllamaClient>>()))
    .AddSingleton(sp => new MemoryManager(
        Path.Combine(AppContext.BaseDirectory, "memories.json"),
        sp.GetRequiredService<ILogger<MemoryManager>>()))
    .AddSingleton(sp => new PersonalityManager(
        Path.GetFullPath(cfg.PersonalityPath),
        sp.GetRequiredService<ILogger<PersonalityManager>>()))
    .AddSingleton(sp => new CodexManager(
        Path.Combine(AppContext.BaseDirectory, "ash_codex.db"),
        sp.GetRequiredService<ILogger<CodexManager>>()))
    .AddSingleton(sp => new AutonomousMessageQueue(
        sp.GetRequiredService<DiscordSocketClient>(),
        sp.GetRequiredService<ILogger<AutonomousMessageQueue>>()))
    .AddSingleton(sp => new StateTracker(
        Path.Combine(AppContext.BaseDirectory, cfg.WorkspacePath, "processing_state.json"),
        sp.GetRequiredService<ILogger<StateTracker>>()))
    .AddSingleton(sp => new ToolExecutor(
        cfg,
        sp.GetRequiredService<DiscordSocketClient>(),
        sp.GetRequiredService<MemoryManager>(),
        sp.GetRequiredService<AutonomousMessageQueue>(),
        sp.GetRequiredService<OllamaClient>(),
        sp.GetRequiredService<IHttpClientFactory>(),
        sp.GetRequiredService<ILogger<ToolExecutor>>()))
    .AddSingleton(sp => new MessageHandler(
        cfg,
        sp.GetRequiredService<DiscordSocketClient>(),
        sp.GetRequiredService<OllamaClient>(),
        sp.GetRequiredService<MemoryManager>(),
        sp.GetRequiredService<PersonalityManager>(),
        sp.GetRequiredService<AutonomousMessageQueue>(),
        sp.GetRequiredService<StateTracker>(),
        sp.GetRequiredService<ToolExecutor>(),
        sp.GetRequiredService<ILogger<MessageHandler>>()))
    .AddSingleton(sp => new AshDiscordBot(
        cfg,
        sp.GetRequiredService<DiscordSocketClient>(),
        sp.GetRequiredService<MessageHandler>(),
        sp.GetRequiredService<AutonomousMessageQueue>(),
        sp.GetRequiredService<StateTracker>(),
        sp.GetRequiredService<OllamaClient>(),
        sp.GetRequiredService<PersonalityManager>(),
        sp.GetRequiredService<MemoryManager>(),
        sp.GetRequiredService<ToolExecutor>(),
        sp.GetRequiredService<ILogger<AshDiscordBot>>()))
    .BuildServiceProvider();

// ─── Ollama: kill stale instances, then start fresh if needed ────────────────
var ollamaClient = services.GetRequiredService<OllamaClient>();
System.Diagnostics.Process? ollamaProc = null;

static void KillExistingOllama()
{
    int killed = 0;
    foreach (var p in System.Diagnostics.Process.GetProcesses())
    {
        try
        {
            if (p.ProcessName.Contains("ollama", StringComparison.OrdinalIgnoreCase))
            {
                p.Kill(true);
                killed++;
            }
        }
        catch { }
    }
    if (killed > 0) Console.WriteLine($"🧹 Cleaned up {killed} stale Ollama process(es).");
}

if (!await ollamaClient.IsAvailableAsync())
{
    Console.WriteLine("⚠️  Ollama not detected — killing stale instances and starting fresh...");
    KillExistingOllama();
    await Task.Delay(500);

    try
    {
        ollamaProc = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo("ollama", "serve")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = false,
                RedirectStandardError = false
            }
        };
        ollamaProc.Start();
        Console.WriteLine("🦙 Ollama process started — waiting for it to be ready...");

        // Wait up to 20s for Ollama to come up
        for (int i = 0; i < 20; i++)
        {
            await Task.Delay(1000);
            if (await ollamaClient.IsAvailableAsync())
            {
                Console.WriteLine($"✅ Ollama ready after {i + 1}s.");
                break;
            }
            if (i == 19) Console.Error.WriteLine("⚠️  Ollama did not respond in time — continuing anyway.");
        }

        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            try { ollamaProc?.Kill(true); } catch { }
        };
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"❌ Failed to start Ollama: {ex.Message}");
        Console.Error.WriteLine("   Make sure 'ollama' is in your PATH.");
    }
}
else
{
    Console.WriteLine("✅ Ollama already running.");
}

// ─── Ensure configured model is available ────────────────────────────────────
var installedModels = await ollamaClient.GetInstalledModelsAsync();
bool modelFound = installedModels.Any(m => m.StartsWith(cfg.OllamaModel, StringComparison.OrdinalIgnoreCase));
if (!modelFound)
{
    Console.WriteLine($"📦 Model '{cfg.OllamaModel}' not found locally — pulling from Ollama Hub...");
    Console.WriteLine("   This may take a while on first run (several GB). Grab a coffee. ☕");
    try
    {
        var lastStatus = "";
        await ollamaClient.PullModelAsync(cfg.OllamaModel, status =>
        {
            if (status != lastStatus)
            {
                Console.Write($"\r   {status,-60}");
                lastStatus = status;
            }
        });
        Console.WriteLine($"\n✅ Model '{cfg.OllamaModel}' ready.");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"\n❌ Failed to pull model: {ex.Message}");
        Console.Error.WriteLine($"   Try manually: ollama pull {cfg.OllamaModel}");
    }
}
else
{
    Console.WriteLine($"✅ Model '{cfg.OllamaModel}' ready.");
}

// ─── Run ─────────────────────────────────────────────────────────────────────
var bot = services.GetRequiredService<AshDiscordBot>();
// Wire back-reference so MessageHandler can flip status
services.GetRequiredService<MessageHandler>().Bot = bot;
await bot.StartAsync();
