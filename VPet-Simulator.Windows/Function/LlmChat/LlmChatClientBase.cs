using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace VPet_Simulator.Windows;

internal abstract class LlmChatClientBase
{
    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    protected static HttpClient CreateHttpClient(LlmChatConfig config)
    {
        return new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(Math.Max(5, config.TimeoutSeconds))
        };
    }

    protected static string JoinUrl(string baseUrl, string path)
    {
        return (baseUrl ?? "").TrimEnd('/') + "/" + path.TrimStart('/');
    }

    protected static StringContent JsonContent(object value)
    {
        return new StringContent(JsonSerializer.Serialize(value, JsonOptions), Encoding.UTF8, "application/json");
    }

    protected static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        string detail = await response.Content.ReadAsStringAsync(cancellationToken);
        if (detail.Length > 300)
            detail = detail[..300];
        throw new InvalidOperationException($"{(int)response.StatusCode} {response.ReasonPhrase}: {detail}");
    }

    protected static void ValidateModel(LlmChatConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.Model))
            throw new InvalidOperationException("请先填写模型名称");
    }

    protected static void ValidateApiKey(LlmChatConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.ApiKey))
            throw new InvalidOperationException("请先填写 API Key");
    }

    protected static string ReadString(JsonElement element, params string[] path)
    {
        JsonElement current = element;
        foreach (string name in path)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(name, out current))
                return "";
        }
        return current.ValueKind == JsonValueKind.String ? current.GetString() ?? "" : "";
    }
}
