using System.Text.Json.Nodes;

namespace AshBot.Tools;

/// <summary>Mirrors the DISCORD_TOOLS list from ash_tools.py as Ollama-format JSON tool objects.</summary>
public static class ToolDefinitions
{
    public static readonly List<JsonObject> All = [
        T("manage_reaction", "Add or remove emoji reaction on a message",
            Obj(P("message_id","string","Message ID"), P("emoji","string","Emoji"), P("action","string","add or remove")),
            ["message_id","emoji","action"]),
        T("send_dm", "Send a private message to a user",
            Obj(P("user_id","string","User ID"), P("content","string","Message content")),
            ["user_id","content"]),
        T("search_yt_music", "Search YouTube Music",
            Obj(P("query","string","Search query"), P("filter","string","songs, albums, artists, or videos")),
            ["query"]),
        T("run_code", "Execute a C# script snippet safely. Returns stdout/stderr.",
            Obj(P("code","string","C# script code"), P("timeout","integer","Max seconds, default 10 max 30")),
            ["code"]),
        T("check_health", "Check Ash health: uptime, RAM, Discord status, Ollama status",
            Obj(), []),
        T("memory_search", "Search long-term memory",
            Obj(P("query","string","Search query")), ["query"]),
        T("memory_get", "Retrieve a full community profile for a person by name or Discord ID. Returns all known info: role, context, projects, memories.",
            Obj(P("key","string","Person name, username, or Discord ID")), ["key"]),
        T("memory_update", "Add a new memory or observation about a person. Use after learning something new about someone in the community.",
            Obj(P("person","string","Person name, username, or Discord ID"), P("memory","string","What to remember about them")),
            ["person","memory"]),
        T("schedule_task", "Schedule a message to send after a delay",
            Obj(P("message","string","Message to send"), P("delay_minutes","number","Delay in minutes")),
            ["message","delay_minutes"]),
        T("reboot_ash", "Restart Ash",
            Obj(P("reason","string","Reason for reboot")), []),
        T("read_file", "Read a file from the ash-bot workspace",
            Obj(P("file_path","string","File path")), ["file_path"]),
        T("list_files", "List files in a directory",
            Obj(P("path","string","Directory path")), ["path"]),
        T("write_file", "Write to ash-workspace",
            Obj(P("filename","string","Filename"), P("content","string","File content"), P("mode","string","write or append")),
            ["filename","content"]),
        T("get_user_info", "Get Discord user info",
            Obj(P("user_id","string","User ID")), ["user_id"]),
        T("read_channel_history", "Read recent channel message history",
            Obj(P("limit","integer","Number of messages")), ["limit"]),
        T("send_autonomous_message", "Speak unprompted in the channel",
            Obj(P("content","string","Message to send"), P("delay","number","Delay in seconds")),
            ["content"]),
        T("view_image", "Analyze an image from a URL",
            Obj(P("image_url","string","Image URL"), P("prompt","string","What to focus on")),
            ["image_url"]),
        T("deep_research", "Multi-source web research on a topic — searches, fetches, synthesizes",
            Obj(P("topic","string","Research topic")), ["topic"]),
        T("web_browse", "Fetch and read the content of a specific URL",
            Obj(P("url","string","URL to browse"), P("prompt","string","Optional focus prompt")),
            ["url"]),
        T("send_embed", "Send a rich formatted Discord embed",
            Obj(P("title","string","Embed title"), P("description","string","Embed body"), P("color","integer","Color integer")),
            ["title","description"]),
    ];

    private static JsonObject T(string name, string desc, JsonObject properties, string[] required) =>
        new JsonObject
        {
            ["type"] = "function",
            ["function"] = new JsonObject
            {
                ["name"] = name,
                ["description"] = desc,
                ["parameters"] = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = properties,
                    ["required"] = new JsonArray(required.Select(r => (JsonNode?)JsonValue.Create(r)).ToArray())
                }
            }
        };

    private static JsonObject Obj(params JsonObject[] props)
    {
        var obj = new JsonObject();
        foreach (var p in props)
            obj[p["_name"]!.GetValue<string>()] = new JsonObject
            {
                ["type"] = p["type"]!.GetValue<string>(),
                ["description"] = p["description"]!.GetValue<string>()
            };
        return obj;
    }

    private static JsonObject P(string name, string type, string description) =>
        new JsonObject { ["_name"] = name, ["type"] = type, ["description"] = description };
}
