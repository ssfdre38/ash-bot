using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace AshBot.State;

public record StateData(string Status, ulong Mid, ulong Cid, string User, string Summary, long Ts);

public class StateTracker
{
    private readonly string _path;
    private readonly ILogger<StateTracker> _log;
    private Dictionary<string, object?> _state = new();

    public StateTracker(string path, ILogger<StateTracker> log)
    {
        _path = path;
        _log = log;
        Load();
    }

    private void Load()
    {
        if (File.Exists(_path))
        {
            try { _state = JsonSerializer.Deserialize<Dictionary<string, object?>>( File.ReadAllText(_path)) ?? new(); }
            catch { _state = new(); }
        }
    }

    private void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, JsonSerializer.Serialize(_state));
    }

    public void StartThought(ulong mid, ulong cid, string user, string text)
    {
        _state = new Dictionary<string, object?>
        {
            ["status"] = "in_progress", ["mid"] = mid.ToString(), ["cid"] = cid.ToString(),
            ["user"] = user, ["summary"] = text[..Math.Min(50, text.Length)],
            ["ts"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        Save();
        _log.LogInformation("[STATE] Flagged thought IN_PROGRESS (MSG-{Mid})", mid);
    }

    public void FinishThought()
    {
        if (_state.TryGetValue("status", out var s) && s?.ToString() == "in_progress")
            _log.LogInformation("[STATE] Flagged thought COMPLETED (MSG-{Mid})", _state.GetValueOrDefault("mid"));
        _state = new Dictionary<string, object?> { ["status"] = "idle", ["ts"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds() };
        Save();
    }

    public Dictionary<string, object?>? GetStale(int timeoutSecs = 1200)
    {
        if (_state.TryGetValue("status", out var s) && s?.ToString() == "in_progress")
        {
            if (_state.TryGetValue("ts", out var tsObj) && tsObj is JsonElement el)
            {
                var ts = el.GetInt64();
                if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - ts > timeoutSecs)
                    return _state;
            }
        }
        return null;
    }

    public bool IsInProgress => _state.TryGetValue("status", out var s) && s?.ToString() == "in_progress";
    public ulong? StaleCid => _state.TryGetValue("cid", out var c) ? ulong.TryParse(c?.ToString(), out var id) ? id : null : null;
}
