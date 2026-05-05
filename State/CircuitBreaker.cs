using Microsoft.Extensions.Logging;

namespace AshBot.State;

public class CircuitBreaker
{
    private readonly int _failureThreshold;
    private readonly TimeSpan _recoveryTimeout;
    private readonly Dictionary<string, List<DateTime>> _failures = new();
    private readonly Dictionary<string, DateTime> _openUntil = new();
    private readonly ILogger<CircuitBreaker> _log;

    public CircuitBreaker(ILogger<CircuitBreaker> log, int failureThreshold = 5, int recoveryTimeoutSecs = 60)
    {
        _log = log;
        _failureThreshold = failureThreshold;
        _recoveryTimeout = TimeSpan.FromSeconds(recoveryTimeoutSecs);
    }

    public async Task<T> ExecuteAsync<T>(string name, Func<Task<T>> func)
    {
        var now = DateTime.UtcNow;
        if (_openUntil.TryGetValue(name, out var until) && until > now)
        {
            var remaining = (int)(until - now).TotalSeconds;
            throw new InvalidOperationException($"Circuit '{name}' is OPEN — retry in {remaining}s");
        }

        try
        {
            var result = await func();
            _failures.Remove(name);
            _openUntil.Remove(name);
            return result;
        }
        catch (Exception)
        {
            var ts = _failures.GetValueOrDefault(name, []);
            ts.Add(now);
            _failures[name] = ts.Where(t => now - t < _recoveryTimeout).ToList();
            if (_failures[name].Count >= _failureThreshold)
            {
                _openUntil[name] = now + _recoveryTimeout;
                _log.LogWarning("[CircuitBreaker] '{Name}' TRIPPED after {Threshold} failures — pausing {Secs}s",
                    name, _failureThreshold, (int)_recoveryTimeout.TotalSeconds);
            }
            throw;
        }
    }
}
