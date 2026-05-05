using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace AshBot.Memory;

public class MemoryData
{
    [JsonPropertyName("last_updated")] public string LastUpdated { get; set; } = "";
    [JsonPropertyName("people")] public Dictionary<string, PersonRecord> People { get; set; } = new();
    [JsonPropertyName("projects")] public Dictionary<string, JsonElement> Projects { get; set; } = new();
    [JsonPropertyName("events")] public Dictionary<string, JsonElement> Events { get; set; } = new();
    [JsonPropertyName("technical")] public Dictionary<string, JsonElement> Technical { get; set; } = new();
    [JsonPropertyName("community")] public Dictionary<string, JsonElement> Community { get; set; } = new();
    [JsonPropertyName("ash_history")] public Dictionary<string, JsonElement> AshHistory { get; set; } = new();
}

public class PersonRecord
{
    [JsonPropertyName("discord_id")] public string? DiscordId { get; set; }
    [JsonPropertyName("names")] public List<string> Names { get; set; } = new();
    // "memories" matches the key used in memories.json (old "notes" field was never populated)
    [JsonPropertyName("memories")] public List<string> Memories { get; set; } = new();
    [JsonPropertyName("last_seen")] public string? LastSeen { get; set; }
    // Captures all other fields: role, context, location, projects, expertise, message_count, etc.
    [JsonExtensionData] public Dictionary<string, JsonElement>? Extra { get; set; }
}

public class MemoryManager
{
    private readonly string _path;
    private MemoryData _data;
    private readonly ILogger<MemoryManager> _log;
    private static readonly JsonSerializerOptions _opts = new() { WriteIndented = true };

    public MemoryManager(string path, ILogger<MemoryManager> log)
    {
        _path = path;
        _log = log;
        _data = Load();
    }

    private MemoryData Load()
    {
        if (File.Exists(_path))
        {
            try
            {
                var json = File.ReadAllText(_path);
                return JsonSerializer.Deserialize<MemoryData>(json) ?? Empty();
            }
            catch (Exception ex)
            {
                _log.LogWarning("Failed to load memories: {Ex}", ex.Message);
            }
        }
        return Empty();
    }

    public void Save()
    {
        _data.LastUpdated = DateTimeOffset.Now.ToString("o");
        File.WriteAllText(_path, JsonSerializer.Serialize(_data, _opts));
    }

    private static MemoryData Empty() => new() { LastUpdated = DateTimeOffset.Now.ToString("o") };

    public PersonRecord? FindPersonById(string discordId)
    {
        foreach (var (_, person) in _data.People)
            if (person.DiscordId == discordId) return person;
        return null;
    }

    public PersonRecord? FindPersonByName(string query)
    {
        var q = query.ToLowerInvariant();
        foreach (var (key, person) in _data.People)
        {
            if (key.ToLowerInvariant().Contains(q)) return person;
            if (person.Names.Any(n => n.ToLowerInvariant().Contains(q))) return person;
        }
        return null;
    }

    public void UpdateOrAddPerson(string discordId, string name)
    {
        var person = FindPersonById(discordId);
        if (person is null)
        {
            var key = name.ToLowerInvariant().Replace(" ", "_");
            person = new PersonRecord { DiscordId = discordId, Names = [name] };
            _data.People[key] = person;
        }
        else if (!person.Names.Contains(name))
        {
            person.Names.Add(name);
        }
        person.LastSeen = DateTimeOffset.Now.ToString("o");
        Save();
    }

    public string GetSummary()
    {
        var lines = new List<string>();
        foreach (var (key, person) in _data.People)
        {
            var mems = person.Memories.Count > 0 ? string.Join("; ", person.Memories.Take(2)) : "no memories";
            var role = person.Extra?.TryGetValue("role", out var r) == true ? $" [{r}]" : "";
            lines.Add($"- {key}{role} (aka {string.Join(", ", person.Names)}): {mems}");
        }
        return lines.Count > 0 ? string.Join("\n", lines) : "(no memories yet)";
    }

    public List<string> SearchMemories(string query, int limit = 5)
    {
        var q = query.ToLowerInvariant();
        var results = new List<(double score, string text)>();

        foreach (var (key, person) in _data.People)
        {
            double score = 0;

            if (key.ToLowerInvariant().Contains(q) || person.Names.Any(n => n.ToLowerInvariant().Contains(q)))
                score = 1.0;
            else if (person.Memories.Any(m => m.ToLowerInvariant().Contains(q)))
                score = 0.8;
            else if (person.Extra != null && person.Extra.Values.Any(v => FlattenElement(v).ToLowerInvariant().Contains(q)))
                score = 0.6;

            if (score > 0)
                results.Add((score, $"[person:{key}] {JsonSerializer.Serialize(person, _opts)}"));
        }

        // Search community, projects, technical sections
        foreach (var (key, val) in _data.Community)
            if (FlattenElement(val).ToLowerInvariant().Contains(q))
                results.Add((0.4, $"[community:{key}] {val}"));

        foreach (var (key, val) in _data.Projects)
            if (FlattenElement(val).ToLowerInvariant().Contains(q))
                results.Add((0.4, $"[project:{key}] {val}"));

        foreach (var (key, val) in _data.Technical)
            if (FlattenElement(val).ToLowerInvariant().Contains(q))
                results.Add((0.3, $"[technical:{key}] {val}"));

        return results
            .OrderByDescending(r => r.score)
            .Take(limit)
            .Select(r => r.text)
            .ToList();
    }

    /// <summary>Recursively flattens a JsonElement to a searchable string.</summary>
    private static string FlattenElement(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.String => el.GetString() ?? "",
        JsonValueKind.Array  => string.Join(" ", el.EnumerateArray().Select(FlattenElement)),
        JsonValueKind.Object => string.Join(" ", el.EnumerateObject().Select(p => FlattenElement(p.Value))),
        _                    => el.ToString()
    };

    /// <summary>Append a memory to a person's record. Creates a minimal record if person not found.</summary>
    public string AddPersonMemory(string nameOrId, string memoryText)
    {
        PersonRecord? person = FindPersonById(nameOrId) ?? FindPersonByName(nameOrId);
        string? foundKey = null;

        foreach (var (key, p) in _data.People)
            if (p == person) { foundKey = key; break; }

        if (person is null)
        {
            foundKey = nameOrId.ToLowerInvariant().Replace(" ", "_");
            person = new PersonRecord { Names = [nameOrId] };
            _data.People[foundKey] = person;
        }

        var timestamp = DateTimeOffset.Now.ToString("yyyy-MM-dd");
        person.Memories.Add($"{memoryText} ({timestamp})");
        person.LastSeen = timestamp;
        Save();
        return $"Memory saved for [{foundKey}]: {memoryText}";
    }

    public string GetRelevantContext(string message, string authorName, string discordId)
    {
        var parts = new List<string>();
        var person = FindPersonById(discordId) ?? FindPersonByName(authorName);
        if (person is not null)
            parts.Add($"[User: {authorName}]\n{JsonSerializer.Serialize(person, _opts)}");

        var search = SearchMemories(message, 3);
        if (search.Count > 0)
            parts.Add("[Related memories]\n" + string.Join("\n", search));

        return string.Join("\n\n", parts);
    }
}
