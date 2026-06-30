using LinePutScript;
using LinePutScript.Dictionary;
using LinePutScript.Localization.WPF;
using System;

namespace VPet_Simulator.Windows;

internal enum LlmChatProvider
{
    Ollama,
    DeepSeek,
    MiMo,
    OpenAI
}

internal class LlmChatConfig
{
    public LlmChatProvider Provider { get; set; } = LlmChatProvider.Ollama;
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "qwen3:30b-instruct";
    public string ApiKey { get; set; } = "";
    public string SystemPrompt { get; set; } = "请使用口语化的、可爱的、女性化的、调皮的语言风格和我交流. 你是一只桌宠, 你的名字叫{Name}, 你喜欢玩 虚拟主播模拟器. 你需要尝试体会我与你对话中的感情、产生自己的情感并尽可能给出可爱的回答";
    public double Temperature { get; set; } = 0.8;
    public int MaxTokens { get; set; } = 1024;
    public int HistoryTurns { get; set; } = 8;
    public int TimeoutSeconds { get; set; } = 60;
    public bool BubbleAutoCloseEnabled { get; set; } = true;
    public int BubbleAutoCloseSeconds { get; set; } = 12;
    public bool OllamaPreloadBeforeSend { get; set; } = true;
    public int OllamaKeepAliveMinutes { get; set; } = 5;
    public int OllamaContextLength { get; set; } = 0;
    public bool OllamaThink { get; set; } = false;

    public static LlmChatConfig Load(Setting setting)
    {
        var line = setting["LLMChat"];
        var config = new LlmChatConfig();
        if (Enum.TryParse(line.GetString("provider", nameof(LlmChatProvider.Ollama)), true, out LlmChatProvider provider))
        {
            config.Provider = provider;
        }
        config.BaseUrl = line.GetString("baseUrl", DefaultBaseUrl(config.Provider));
        config.Model = line.GetString("model", DefaultModel(config.Provider));
        config.ApiKey = LlmEnvironmentKeys.ResolveApiKey(config.Provider, line.GetString("apiKey", ""));
        config.SystemPrompt = line.GetString("systemPrompt", config.SystemPrompt);
        config.Temperature = Clamp(line.GetDouble("temperature", config.Temperature), 0, 2);
        config.MaxTokens = Math.Max(1, line.GetInt("maxTokens", config.MaxTokens));
        config.HistoryTurns = Math.Max(0, line.GetInt("historyTurns", config.HistoryTurns));
        config.TimeoutSeconds = Math.Max(5, line.GetInt("timeoutSeconds", config.TimeoutSeconds));
        if (bool.TryParse(line.GetString("bubbleAutoCloseEnabled", config.BubbleAutoCloseEnabled.ToString()), out bool autoClose))
            config.BubbleAutoCloseEnabled = autoClose;
        config.BubbleAutoCloseSeconds = Math.Clamp(line.GetInt("bubbleAutoCloseSeconds", config.BubbleAutoCloseSeconds), 3, 120);
        if (bool.TryParse(line.GetString("ollamaPreloadBeforeSend", config.OllamaPreloadBeforeSend.ToString()), out bool preloadBeforeSend))
            config.OllamaPreloadBeforeSend = preloadBeforeSend;
        config.OllamaKeepAliveMinutes = LoadOllamaKeepAliveMinutes(line, config.OllamaKeepAliveMinutes);
        config.OllamaContextLength = Math.Max(0, line.GetInt("ollamaContextLength", config.OllamaContextLength));
        if (bool.TryParse(line.GetString("ollamaThink", config.OllamaThink.ToString()), out bool think))
            config.OllamaThink = think;

        if (string.IsNullOrWhiteSpace(config.BaseUrl))
        {
            config.BaseUrl = DefaultBaseUrl(config.Provider);
        }
        if (string.IsNullOrWhiteSpace(config.Model))
        {
            config.Model = DefaultModel(config.Provider);
        }
        return config;
    }

    public void Save(Setting setting)
    {
        var line = setting["LLMChat"];
        line.SetString("provider", Provider.ToString());
        line.SetString("baseUrl", BaseUrl?.Trim() ?? "");
        line.SetString("model", Model?.Trim() ?? "");
        line.SetString("apiKey", ApiKey ?? "");
        line.SetString("systemPrompt", SystemPrompt ?? "");
        line.SetDouble("temperature", Clamp(Temperature, 0, 2));
        line.SetInt("maxTokens", Math.Max(1, MaxTokens));
        line.SetInt("historyTurns", Math.Max(0, HistoryTurns));
        line.SetInt("timeoutSeconds", Math.Max(5, TimeoutSeconds));
        line.SetBool("bubbleAutoCloseEnabled", BubbleAutoCloseEnabled);
        line.SetInt("bubbleAutoCloseSeconds", Math.Clamp(BubbleAutoCloseSeconds, 3, 120));
        line.SetBool("ollamaPreloadBeforeSend", OllamaPreloadBeforeSend);
        line.SetInt("ollamaKeepAliveMinutes", Math.Min(Math.Max(OllamaKeepAliveMinutes, -1), 10080));
        line.SetInt("ollamaContextLength", Math.Max(0, OllamaContextLength));
        line.SetBool("ollamaThink", OllamaThink);
    }

    public string GetSystemPrompt(MainWindow mw)
    {
        string basePrompt = (SystemPrompt ?? "")
            .Replace("{Name}", mw.Core?.Save?.Name ?? "VPet")
            .Replace("{name}", mw.Core?.Save?.Name ?? "VPet")
            .Translate();
        string moodContext = BuildMoodContext(mw);
        if (string.IsNullOrWhiteSpace(moodContext))
            return basePrompt;
        return basePrompt + Environment.NewLine + moodContext;
    }

    internal static string BuildMoodContext(MainWindow mw)
    {
        var save = mw?.Core?.Save;
        if (save == null)
            return "";
        double max = Math.Max(1, save.FeelingMax);
        double value = Math.Min(Math.Max(save.Feeling, 0), max);
        double ratio = value / max;
        string moodText;
        string styleText;
        if (ratio >= 0.85)
        {
            moodText = "非常开心";
            styleText = "语气更轻快、亲近、活泼，可以更愿意撒娇和开玩笑";
        }
        else if (ratio >= 0.6)
        {
            moodText = "心情不错";
            styleText = "语气自然明亮，带一点轻松和可爱";
        }
        else if (ratio >= 0.35)
        {
            moodText = "心情普通";
            styleText = "语气保持温和，少一点过度兴奋";
        }
        else if (ratio >= 0.15)
        {
            moodText = "有点低落";
            styleText = "语气更软、更需要陪伴，可以委屈但不要过度消极";
        }
        else
        {
            moodText = "心情很差";
            styleText = "语气疲惫、委屈、需要安慰，避免太欢脱或强行开心";
        }
        return $"当前桌宠状态: 心情 {value:f0}/{max:f0} ({ratio:P0}), 状态为「{moodText}」。请把这个状态作为说话风格参考: {styleText}。不要直接复述这些数值, 除非用户问到状态。";
    }

    public static string DefaultBaseUrl(LlmChatProvider provider)
    {
        return provider switch
        {
            LlmChatProvider.DeepSeek => "https://api.deepseek.com",
            LlmChatProvider.MiMo => "https://api.xiaomimimo.com/v1",
            LlmChatProvider.OpenAI => "https://api.openai.com",
            _ => DefaultOllamaBaseUrl(),
        };
    }

    public static string DefaultModel(LlmChatProvider provider)
    {
        return provider switch
        {
            LlmChatProvider.DeepSeek => "deepseek-v4-flash",
            LlmChatProvider.MiMo => "mimo-v2.5-pro",
            LlmChatProvider.OpenAI => "gpt-4.1-mini",
            _ => "qwen3:30b-instruct",
        };
    }

    public object GetOllamaKeepAlive()
    {
        if (OllamaKeepAliveMinutes < 0)
            return -1;
        return Math.Max(0, OllamaKeepAliveMinutes) + "m";
    }

    private static int LoadOllamaKeepAliveMinutes(ILine line, int defaultValue)
    {
        int minutes = line.GetInt("ollamaKeepAliveMinutes", int.MinValue);
        if (minutes != int.MinValue)
            return Math.Min(Math.Max(minutes, -1), 10080);
        if (bool.TryParse(line.GetString("ollamaKeepLoaded", "False"), out bool keepLoaded) && keepLoaded)
            return -1;

        string keepAlive = line.GetString("ollamaKeepAlive", defaultValue + "m").Trim();
        if (int.TryParse(keepAlive, out minutes))
            return Math.Min(Math.Max(minutes, -1), 10080);
        if (keepAlive.Equals("-1", StringComparison.OrdinalIgnoreCase))
            return -1;
        if (keepAlive.EndsWith("m", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(keepAlive[..^1], out minutes))
        {
            return Math.Min(Math.Max(minutes, -1), 10080);
        }
        if (keepAlive.EndsWith("h", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(keepAlive[..^1], out int hours))
        {
            return Math.Min(Math.Max(hours * 60, -1), 10080);
        }
        return defaultValue;
    }

    private static string DefaultOllamaBaseUrl()
    {
        string host = Environment.GetEnvironmentVariable("OLLAMA_HOST");
        if (string.IsNullOrWhiteSpace(host))
            return "http://localhost:11434";
        host = host.Trim();
        if (host.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || host.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return host;
        }
        return "http://" + host;
    }

    private static double Clamp(double value, double min, double max)
    {
        if (double.IsNaN(value))
            return min;
        return Math.Min(Math.Max(value, min), max);
    }
}
