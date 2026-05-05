using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace AshBot.AI;

public record OllamaMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string? Content = null,
    [property: JsonPropertyName("images")] List<string>? Images = null,
    [property: JsonPropertyName("tool_calls")] List<OllamaToolCall>? ToolCalls = null
);

public record OllamaToolCall(
    [property: JsonPropertyName("function")] OllamaToolFunction Function
);

public record OllamaToolFunction(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("arguments")] JsonElement Arguments
);

public record OllamaChatResponse(
    [property: JsonPropertyName("message")] OllamaMessage Message,
    [property: JsonPropertyName("done")] bool Done
);

public class OllamaClient
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly string _model;
    private readonly ILogger<OllamaClient> _log;

    public OllamaClient(string baseUrl, string model, ILogger<OllamaClient> log, IHttpClientFactory? factory = null)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _model = model;
        _log = log;
        _http = factory?.CreateClient("ollama") ?? new HttpClient { BaseAddress = new Uri(_baseUrl) };
        _http.Timeout = Timeout.InfiniteTimeSpan; // Ollama can take 20+ min on CPU (matches Python behaviour)
    }

    public string Model => _model;

    /// <summary>Non-streaming chat with optional tool definitions. Returns the full response message.</summary>
    public async Task<OllamaMessage> ChatAsync(List<OllamaMessage> messages, List<JsonObject>? tools = null)
    {
        var body = new JsonObject
        {
            ["model"] = _model,
            ["messages"] = JsonSerializer.SerializeToNode(messages),
            ["stream"] = false,
            ["options"] = new JsonObject { ["temperature"] = 0.8, ["num_ctx"] = 8192 }
        };
        if (tools is { Count: > 0 })
            body["tools"] = JsonSerializer.SerializeToNode(tools);

        var response = await _http.PostAsJsonAsync($"{_baseUrl}/api/chat", body);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaChatResponse>();
        return result?.Message ?? new OllamaMessage("assistant", "");
    }

    /// <summary>Streaming chat — yields content tokens.</summary>
    public async IAsyncEnumerable<string> StreamChatAsync(List<OllamaMessage> messages)
    {
        var body = new JsonObject
        {
            ["model"] = _model,
            ["messages"] = JsonSerializer.SerializeToNode(messages),
            ["stream"] = true,
            ["options"] = new JsonObject { ["temperature"] = 0.8, ["num_ctx"] = 8192 }
        };

        var req = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/chat")
        {
            Content = JsonContent.Create(body)
        };
        using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
        resp.EnsureSuccessStatusCode();

        await using var stream = await resp.Content.ReadAsStreamAsync();
        using var reader = new System.IO.StreamReader(stream);
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var chunk = JsonSerializer.Deserialize<OllamaChatResponse>(line);
            var token = chunk?.Message?.Content;
            if (!string.IsNullOrEmpty(token)) yield return token;
        }
    }

    /// <summary>Vision: send image bytes + prompt, returns description.</summary>
    public async Task<string> DescribeImageAsync(byte[] imageBytes, string prompt = "Describe this image")
    {
        var b64 = Convert.ToBase64String(imageBytes);
        var msg = new OllamaMessage("user", prompt, [b64]);
        var response = await ChatAsync([msg]);
        return response.Content ?? "";
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var resp = await _http.GetAsync($"{_baseUrl}/api/tags");
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    /// <summary>Returns the list of locally installed model names.</summary>
    public async Task<List<string>> GetInstalledModelsAsync()
    {
        try
        {
            var resp = await _http.GetAsync($"{_baseUrl}/api/tags");
            if (!resp.IsSuccessStatusCode) return [];
            var json = await resp.Content.ReadFromJsonAsync<JsonObject>();
            var models = new List<string>();
            if (json?["models"] is JsonArray arr)
                foreach (var m in arr)
                    if (m?["name"]?.GetValue<string>() is string name)
                        models.Add(name);
            return models;
        }
        catch { return []; }
    }

    /// <summary>Pulls a model from Ollama Hub, writing progress lines to the provided callback.</summary>
    public async Task PullModelAsync(string model, Action<string> onProgress)
    {
        var body = new JsonObject { ["name"] = model, ["stream"] = true };
        var req = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/pull")
        {
            Content = JsonContent.Create(body)
        };
        using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
        resp.EnsureSuccessStatusCode();

        await using var stream = await resp.Content.ReadAsStreamAsync();
        using var reader = new System.IO.StreamReader(stream);
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var node = JsonSerializer.Deserialize<JsonObject>(line);
            var status = node?["status"]?.GetValue<string>() ?? "";
            var completed = node?["completed"]?.GetValue<long>() ?? 0;
            var total = node?["total"]?.GetValue<long>() ?? 0;
            var progress = total > 0 ? $"{status} ({completed * 100 / total}%)" : status;
            onProgress(progress);
        }
    }
}

