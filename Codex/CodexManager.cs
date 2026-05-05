using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace AshBot.Codex;

public class CodexManager
{
    private readonly string _dbPath;
    private readonly ILogger<CodexManager> _log;

    public CodexManager(string dbPath, ILogger<CodexManager> log)
    {
        _dbPath = dbPath;
        _log = log;
        InitDb();
        _log.LogInformation("CodexManager initialized: {Path}", dbPath);
    }

    private SqliteConnection Open() => new SqliteConnection($"Data Source={_dbPath}");

    private void InitDb()
    {
        using var conn = Open();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS codex_entries (
                id TEXT PRIMARY KEY,
                title TEXT NOT NULL,
                category TEXT NOT NULL,
                content TEXT NOT NULL,
                confidence REAL DEFAULT 0.5,
                created_at INTEGER NOT NULL,
                updated_at INTEGER NOT NULL,
                times_referenced INTEGER DEFAULT 0,
                marked_for_training INTEGER DEFAULT 0,
                training_priority REAL DEFAULT 0.5,
                ash_decision_reason TEXT,
                ash_decision_confidence REAL
            );
            CREATE TABLE IF NOT EXISTS codex_tags (
                entry_id TEXT NOT NULL,
                tag TEXT NOT NULL,
                PRIMARY KEY (entry_id, tag)
            );
            """;
        cmd.ExecuteNonQuery();
    }

    public void AddEntry(string title, string category, string content, double confidence = 0.5, string[]? tags = null)
    {
        var id = Guid.NewGuid().ToString("N")[..12];
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        using var conn = Open();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT OR REPLACE INTO codex_entries (id, title, category, content, confidence, created_at, updated_at)
            VALUES ($id, $title, $cat, $content, $conf, $now, $now)
            """;
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$title", title);
        cmd.Parameters.AddWithValue("$cat", category);
        cmd.Parameters.AddWithValue("$content", content);
        cmd.Parameters.AddWithValue("$conf", confidence);
        cmd.Parameters.AddWithValue("$now", now);
        cmd.ExecuteNonQuery();

        if (tags is not null)
        {
            foreach (var tag in tags)
            {
                using var tagCmd = conn.CreateCommand();
                tagCmd.CommandText = "INSERT OR IGNORE INTO codex_tags (entry_id, tag) VALUES ($eid, $tag)";
                tagCmd.Parameters.AddWithValue("$eid", id);
                tagCmd.Parameters.AddWithValue("$tag", tag);
                tagCmd.ExecuteNonQuery();
            }
        }
    }

    public List<Dictionary<string, object>> Search(string query, int limit = 5)
    {
        var q = $"%{query}%";
        using var conn = Open();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, title, category, content, confidence FROM codex_entries
            WHERE title LIKE $q OR content LIKE $q OR category LIKE $q
            ORDER BY confidence DESC LIMIT $limit
            """;
        cmd.Parameters.AddWithValue("$q", q);
        cmd.Parameters.AddWithValue("$limit", limit);
        var results = new List<Dictionary<string, object>>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            results.Add(new Dictionary<string, object>
            {
                ["id"] = reader.GetString(0), ["title"] = reader.GetString(1),
                ["category"] = reader.GetString(2), ["content"] = reader.GetString(3),
                ["confidence"] = reader.GetDouble(4)
            });
        return results;
    }
}
