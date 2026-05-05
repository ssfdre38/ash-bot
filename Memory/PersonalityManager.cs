using Microsoft.Extensions.Logging;

namespace AshBot.Memory;

public class PersonalityManager
{
    private readonly string _path;
    private readonly ILogger<PersonalityManager> _log;
    private readonly Dictionary<string, DateTime> _lastLoaded = new();
    private string _cache = "";

    private static readonly string[] Files = ["soul.json", "identity.json", "ABILITIES.md", "USER.md"];

    public PersonalityManager(string path, ILogger<PersonalityManager> log)
    {
        _path = path;
        _log = log;
        LoadAll();
    }

    public string LoadAll()
    {
        var parts = new List<string>();
        foreach (var file in Files)
        {
            var p = Path.Combine(_path, file);
            if (File.Exists(p))
            {
                _lastLoaded[file] = File.GetLastWriteTime(p);
                parts.Add(File.ReadAllText(p));
            }
        }
        _cache = string.Join("\n\n", parts).Trim();
        _log.LogInformation("Personality loaded ({Chars} chars)", _cache.Length);
        return _cache;
    }

    public string CheckAndReload()
    {
        foreach (var file in Files)
        {
            var p = Path.Combine(_path, file);
            if (File.Exists(p))
            {
                var mtime = File.GetLastWriteTime(p);
                if (!_lastLoaded.TryGetValue(file, out var last) || mtime > last)
                    return LoadAll();
            }
        }
        return _cache;
    }

    public string Current => _cache;
}
