using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace VPet_Simulator.Windows;

internal class OpenAiCompatibleChatClient : LlmChatClientBase, ILlmChatClient
{
    private readonly string providerName;
    private readonly bool useApiKeyHeader;

    public OpenAiCompatibleChatClient(string providerName, bool useApiKeyHeader = false)
    {
        this.providerName = providerName;
        this.useApiKeyHeader = useApiKeyHeader;
    }

    public async Task<string> StreamChatAsync(
        LlmChatConfig config,
        IReadOnlyList<LlmChatMessage> history,
        string userText,
        string systemPrompt,
        CancellationToken cancellationToken,
        Action<string, bool> onDelta)
    {
        ValidateModel(config);
        config.ApiKey = LlmEnvironmentKeys.ResolveApiKey(config.Provider, config.ApiKey);
        ValidateApiKey(config);
        using HttpClient client = CreateHttpClient(config);
        if (useApiKeyHeader)
            client.DefaultRequestHeaders.TryAddWithoutValidation("api-key", config.ApiKey);
        else
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);

        var messages = new List<object>();
        if (!string.IsNullOrWhiteSpace(systemPrompt))
            messages.Add(new { role = "system", content = systemPrompt });
        foreach (var message in history)
            messages.Add(new { role = message.Role, content = message.Content });
        messages.Add(new { role = "user", content = userText });

        var request = new
        {
            model = config.Model,
            stream = true,
            messages,
            temperature = config.Temperature,
            max_tokens = config.MaxTokens
        };

        using HttpResponseMessage response = await client.PostAsync(JoinUrl(config.BaseUrl, "/chat/completions"), JsonContent(request), cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        string result = "";
        await foreach (string data in ReadSseDataAsync(response, cancellationToken))
        {
            if (data == "[DONE]")
                break;
            string delta = ParseDelta(data);
            if (!string.IsNullOrEmpty(delta))
            {
                result += delta;
                onDelta(delta, false);
            }
        }
        return result;
    }

    private static string ParseDelta(string data)
    {
        using var doc = JsonDocument.Parse(data);
        if (doc.RootElement.TryGetProperty("choices", out var choices)
            && choices.ValueKind == JsonValueKind.Array
            && choices.GetArrayLength() > 0)
        {
            return ReadString(choices[0], "delta", "content");
        }
        return "";
    }

    public static async IAsyncEnumerable<string> ReadSseDataAsync(
        HttpResponseMessage response,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            string line = await reader.ReadLineAsync(cancellationToken);
            if (line == null)
                yield break;
            if (!line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                continue;
            string data = line[5..].Trim();
            if (!string.IsNullOrEmpty(data))
                yield return data;
        }
    }
}
