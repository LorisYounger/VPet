using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace VPet_Simulator.Windows;

internal class MiMoAudioClient
{
    private static readonly HttpClient httpClient = new()
    {
        Timeout = Timeout.InfiniteTimeSpan
    };

    public async Task<string> SynthesizeToWavFileAsync(MiMoVoiceConfig config, string text, CancellationToken cancellationToken)
    {
        ValidateCommon(config);
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("没有可合成的文本");

        string cleaned = CleanTextForSpeech(text);
        if (string.IsNullOrWhiteSpace(cleaned))
            throw new InvalidOperationException("清理后没有可合成的文本");

        var messages = new List<object>();
        if (!string.IsNullOrWhiteSpace(config.TtsStylePrompt))
            messages.Add(new { role = "user", content = config.TtsStylePrompt });
        messages.Add(new { role = "assistant", content = cleaned });

        var payload = new
        {
            model = string.IsNullOrWhiteSpace(config.TtsModel) ? "mimo-v2.5-tts" : config.TtsModel,
            messages,
            audio = new
            {
                format = "wav",
                voice = string.IsNullOrWhiteSpace(config.TtsVoice) ? "冰糖" : config.TtsVoice
            }
        };

        using var doc = await PostJsonAsync(config, payload, cancellationToken);
        string audioBase64 = TryReadString(doc.RootElement, "choices", 0, "message", "audio", "data");
        if (string.IsNullOrWhiteSpace(audioBase64))
            throw new InvalidOperationException("接口没有返回音频数据");

        byte[] bytes = Convert.FromBase64String(audioBase64);
        string dir = GetTempDirectory();
        string path = Path.Combine(dir, $"mimo_tts_{DateTime.Now:yyyyMMdd_HHmmss_fff}.wav");
        await File.WriteAllBytesAsync(path, bytes, cancellationToken);
        return path;
    }

    public async Task<string> RecognizeAsync(MiMoVoiceConfig config, string wavPath, CancellationToken cancellationToken)
    {
        ValidateCommon(config);
        if (string.IsNullOrWhiteSpace(wavPath) || !File.Exists(wavPath))
            throw new FileNotFoundException("录音文件不存在", wavPath);

        byte[] bytes = await File.ReadAllBytesAsync(wavPath, cancellationToken);
        string base64 = Convert.ToBase64String(bytes);
        if (Encoding.UTF8.GetByteCount(base64) > 10 * 1024 * 1024)
            throw new InvalidOperationException("录音超过 10MB, 请缩短录音时间");

        var payload = new
        {
            model = string.IsNullOrWhiteSpace(config.AsrModel) ? "mimo-v2.5-asr" : config.AsrModel,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "input_audio",
                            input_audio = new
                            {
                                data = $"data:audio/wav;base64,{base64}"
                            }
                        }
                    }
                }
            },
            asr_options = new
            {
                language = string.IsNullOrWhiteSpace(config.AsrLanguage) ? "auto" : config.AsrLanguage
            }
        };

        using var doc = await PostJsonAsync(config, payload, cancellationToken);
        string text = TryReadString(doc.RootElement, "choices", 0, "message", "content");
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("接口没有返回识别文本");
        return text.Trim();
    }

    public static string CleanTextForSpeech(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";

        string result = text;
        result = Regex.Replace(result, @"（[^（）\r\n]{1,80}）", "");
        result = Regex.Replace(result, @"\([^()\r\n]{1,80}\)", "");
        result = Regex.Replace(result, @"\*[^*\r\n]{1,80}\*", "");
        result = Regex.Replace(result, @"`([^`]*)`", "$1");
        result = Regex.Replace(result, @"\*\*([^*]+)\*\*", "$1");
        result = Regex.Replace(result, @"[_#>\[\]~]", "");
        result = Regex.Replace(result, @"[☆★✨💫⭐🌟]+", "");
        result = Regex.Replace(result, @"[～~]{2,}", "～");
        result = Regex.Replace(result, @"\s{2,}", " ");
        return result.Trim();
    }

    private static void ValidateCommon(MiMoVoiceConfig config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));
        config.ApiKey = LlmEnvironmentKeys.ResolveMiMoVoiceApiKey(config.ApiKey);
        if (string.IsNullOrWhiteSpace(config.ApiKey))
            throw new InvalidOperationException("请先填写 MiMo API Key");
        if (string.IsNullOrWhiteSpace(config.BaseUrl))
            throw new InvalidOperationException("请先填写 MiMo 接口地址");
    }

    private static async Task<JsonDocument> PostJsonAsync(MiMoVoiceConfig config, object payload, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, config.ChatCompletionsUrl);
        request.Headers.TryAddWithoutValidation("api-key", config.ApiKey);
        string json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(config.TimeoutSeconds, 5, 600)));
        using HttpResponseMessage response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token);
        string body = await response.Content.ReadAsStringAsync(timeoutCts.Token);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"{(int)response.StatusCode} {response.ReasonPhrase}: {Trim(body, 240)}");

        try
        {
            return JsonDocument.Parse(body);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("接口返回 JSON 解析失败", ex);
        }
    }

    private static string TryReadString(JsonElement root, params object[] path)
    {
        JsonElement current = root;
        foreach (object item in path)
        {
            if (item is string name)
            {
                if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(name, out current))
                    return "";
            }
            else if (item is int index)
            {
                if (current.ValueKind != JsonValueKind.Array || current.GetArrayLength() <= index)
                    return "";
                current = current[index];
            }
        }
        return current.ValueKind == JsonValueKind.String ? current.GetString() ?? "" : current.ToString();
    }

    internal static string GetTempDirectory()
    {
        string dir = Path.Combine(Path.GetTempPath(), "VPet", "MiMoVoice");
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static string Trim(string text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";
        return text.Length <= maxLength ? text : text[..maxLength];
    }
}
