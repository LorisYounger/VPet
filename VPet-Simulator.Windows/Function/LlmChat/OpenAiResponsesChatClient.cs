using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace VPet_Simulator.Windows;

internal class OpenAiResponsesChatClient : LlmChatClientBase, ILlmChatClient
{
    private string previousResponseId = "";

    public async Task<string> StreamChatAsync(
        LlmChatConfig config,
        IReadOnlyList<LlmChatMessage> history,
        string userText,
        string systemPrompt,
        CancellationToken cancellationToken,
        Action<string, bool> onDelta)
    {
        ValidateModel(config);
        ValidateApiKey(config);
        using HttpClient client = CreateHttpClient(config);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);

        object request = string.IsNullOrWhiteSpace(previousResponseId)
            ? new
            {
                model = config.Model,
                stream = true,
                instructions = systemPrompt,
                input = BuildInput(history, userText),
                temperature = config.Temperature,
                max_output_tokens = config.MaxTokens
            }
            : new
            {
                model = config.Model,
                stream = true,
                instructions = systemPrompt,
                input = userText,
                previous_response_id = previousResponseId,
                temperature = config.Temperature,
                max_output_tokens = config.MaxTokens
            };

        using HttpResponseMessage response = await client.PostAsync(JoinUrl(config.BaseUrl, "/v1/responses"), JsonContent(request), cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        string result = "";
        await foreach (string data in OpenAiCompatibleChatClient.ReadSseDataAsync(response, cancellationToken))
        {
            if (data == "[DONE]")
                break;
            using var doc = JsonDocument.Parse(data);
            string type = ReadString(doc.RootElement, "type");
            if (type.EndsWith(".delta", StringComparison.OrdinalIgnoreCase))
            {
                string delta = ReadString(doc.RootElement, "delta");
                if (!string.IsNullOrEmpty(delta))
                {
                    result += delta;
                    onDelta(delta, false);
                }
            }
            else if (type == "response.completed")
            {
                string id = ReadString(doc.RootElement, "response", "id");
                if (!string.IsNullOrWhiteSpace(id))
                    previousResponseId = id;
            }
        }
        return result;
    }

    private static object[] BuildInput(IReadOnlyList<LlmChatMessage> history, string userText)
    {
        var input = new List<object>();
        foreach (var message in history)
        {
            input.Add(new
            {
                role = message.Role == "assistant" ? "assistant" : "user",
                content = message.Content
            });
        }
        input.Add(new { role = "user", content = userText });
        return input.ToArray();
    }
}
