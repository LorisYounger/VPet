using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace VPet_Simulator.Windows;

internal class OllamaChatClient : LlmChatClientBase, ILlmChatClient
{
    public async Task PreloadAsync(LlmChatConfig config, CancellationToken cancellationToken)
    {
        ValidateModel(config);
        using HttpClient client = CreateHttpClient(config);
        var request = new
        {
            model = config.Model,
            messages = Array.Empty<object>(),
            stream = false,
            keep_alive = config.GetOllamaKeepAlive(),
            think = config.OllamaThink,
            options = BuildOptions(config)
        };

        using HttpResponseMessage response = await client.PostAsync(JoinUrl(config.BaseUrl, "/api/chat"), JsonContent(request), cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task UnloadAsync(LlmChatConfig config, CancellationToken cancellationToken)
    {
        ValidateModel(config);
        using HttpClient client = CreateHttpClient(config);
        var request = new
        {
            model = config.Model,
            messages = Array.Empty<object>(),
            stream = false,
            keep_alive = 0
        };

        using HttpResponseMessage response = await client.PostAsync(JoinUrl(config.BaseUrl, "/api/chat"), JsonContent(request), cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
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
        if (config.OllamaPreloadBeforeSend)
            await PreloadAsync(config, cancellationToken);

        using HttpClient client = CreateHttpClient(config);
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
            keep_alive = config.GetOllamaKeepAlive(),
            think = config.OllamaThink,
            options = BuildOptions(config)
        };

        using HttpResponseMessage response = await client.PostAsync(JoinUrl(config.BaseUrl, "/api/chat"), JsonContent(request), cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        string result = "";
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            string line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line))
                continue;
            using var doc = JsonDocument.Parse(line);
            string thinking = ReadString(doc.RootElement, "message", "thinking");
            if (!string.IsNullOrEmpty(thinking))
                onDelta(thinking, true);

            string delta = ReadString(doc.RootElement, "message", "content");
            if (!string.IsNullOrEmpty(delta))
            {
                result += delta;
                onDelta(delta, false);
            }
        }
        return result;
    }

    private static object BuildOptions(LlmChatConfig config)
    {
        return config.OllamaContextLength > 0
            ? new
            {
                temperature = config.Temperature,
                num_predict = config.MaxTokens,
                num_ctx = config.OllamaContextLength
            }
            : new
            {
                temperature = config.Temperature,
                num_predict = config.MaxTokens,
                num_ctx = (int?)null
            };
    }
}
