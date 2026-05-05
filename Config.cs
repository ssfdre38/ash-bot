using Microsoft.Extensions.Configuration;

namespace AshBot;

public class AshConfig
{
    public string DiscordToken { get; set; } = "";
    public ulong ChannelId { get; set; }
    public ulong GuildId { get; set; }
    public ulong AdminUserId { get; set; }
    public string OllamaModel { get; set; } = "ssfdre38/gemma4-turbo";
    public string OllamaUrl { get; set; } = "http://localhost:11434";
    public int MaxHistory { get; set; } = 10;
    public int InitiativeIntervalHours { get; set; } = 4;
    public string PersonalityPath { get; set; } = "personality";
    public string WorkspacePath { get; set; } = "ash-workspace";

    public static AshConfig Load()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var cfg = new AshConfig();
        config.GetSection("Ash").Bind(cfg);

        // Allow environment variable overrides
        var envToken = Environment.GetEnvironmentVariable("ASH_DISCORD_TOKEN");
        if (!string.IsNullOrEmpty(envToken)) cfg.DiscordToken = envToken;

        var envModel = Environment.GetEnvironmentVariable("ASH_OLLAMA_MODEL");
        if (!string.IsNullOrEmpty(envModel)) cfg.OllamaModel = envModel;

        var envOllamaUrl = Environment.GetEnvironmentVariable("ASH_OLLAMA_URL");
        if (!string.IsNullOrEmpty(envOllamaUrl)) cfg.OllamaUrl = envOllamaUrl;

        if (ulong.TryParse(Environment.GetEnvironmentVariable("ASH_CHANNEL_ID"), out var envChannel))
            cfg.ChannelId = envChannel;

        if (ulong.TryParse(Environment.GetEnvironmentVariable("ASH_GUILD_ID"), out var envGuild))
            cfg.GuildId = envGuild;

        if (ulong.TryParse(Environment.GetEnvironmentVariable("ASH_ADMIN_USER_ID"), out var envAdmin))
            cfg.AdminUserId = envAdmin;

        return cfg;
    }
}
