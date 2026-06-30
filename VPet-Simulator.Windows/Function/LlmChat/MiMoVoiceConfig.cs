using LinePutScript;
using LinePutScript.Dictionary;
using System;

namespace VPet_Simulator.Windows;

internal enum MiMoAsrSubmitMode
{
    FillInput,
    AutoSend
}

internal class MiMoVoiceConfig
{
    public string ApiKey { get; set; } = "";
    public string BaseUrl { get; set; } = "https://api.xiaomimimo.com/v1";
    public int TimeoutSeconds { get; set; } = 60;
    public bool TtsEnabled { get; set; }
    public string TtsModel { get; set; } = "mimo-v2.5-tts";
    public string TtsVoice { get; set; } = "冰糖";
    public string TtsStylePrompt { get; set; } = "";
    public bool TtsSegmentEnabled { get; set; } = true;
    public int TtsMaxSentences { get; set; } = 2;
    public int TtsSoftMaxChars { get; set; } = 80;
    public bool TtsCutAtCommaAfterLimit { get; set; } = true;
    public bool AsrEnabled { get; set; }
    public string AsrModel { get; set; } = "mimo-v2.5-asr";
    public string AsrLanguage { get; set; } = "auto";
    public int AsrInputDeviceNumber { get; set; } = -1;
    public string AsrInputDeviceName { get; set; } = "";
    public MiMoAsrSubmitMode AsrSubmitMode { get; set; } = MiMoAsrSubmitMode.FillInput;
    public string PushToTalkHotkey { get; set; } = "Ctrl+Alt+M";

    public string ChatCompletionsUrl => BaseUrl.TrimEnd('/') + "/chat/completions";

    public static MiMoVoiceConfig Load(LPS_D set)
    {
        var line = set["MiMoVoice"];
        return new MiMoVoiceConfig
        {
            ApiKey = LlmEnvironmentKeys.ResolveMiMoVoiceApiKey(line.GetString("apiKey", "")),
            BaseUrl = line.GetString("baseUrl", "https://api.xiaomimimo.com/v1"),
            TimeoutSeconds = Math.Clamp(line.GetInt("timeoutSeconds", 60), 5, 600),
            TtsEnabled = line.GetBool("ttsEnabled"),
            TtsModel = line.GetString("ttsModel", "mimo-v2.5-tts"),
            TtsVoice = line.GetString("ttsVoice", "冰糖"),
            TtsStylePrompt = line.GetString("ttsStylePrompt", ""),
            TtsSegmentEnabled = GetBool(line, "ttsSegmentEnabled", true),
            TtsMaxSentences = Math.Clamp(line.GetInt("ttsMaxSentences", 2), 1, 10),
            TtsSoftMaxChars = Math.Clamp(line.GetInt("ttsSoftMaxChars", 80), 20, 1000),
            TtsCutAtCommaAfterLimit = GetBool(line, "ttsCutAtCommaAfterLimit", true),
            AsrEnabled = line.GetBool("asrEnabled"),
            AsrModel = line.GetString("asrModel", "mimo-v2.5-asr"),
            AsrLanguage = NormalizeLanguage(line.GetString("asrLanguage", "auto")),
            AsrInputDeviceNumber = Math.Max(-1, line.GetInt("asrInputDeviceNumber", -1)),
            AsrInputDeviceName = line.GetString("asrInputDeviceName", ""),
            AsrSubmitMode = Enum.TryParse(line.GetString("asrSubmitMode", "FillInput"), true, out MiMoAsrSubmitMode mode)
                ? mode
                : MiMoAsrSubmitMode.FillInput,
            PushToTalkHotkey = line.GetString("pushToTalkHotkey", "Ctrl+Alt+M")
        };
    }

    public void Save(LPS_D set)
    {
        var line = set["MiMoVoice"];
        line[(gstr)"apiKey"] = ApiKey ?? "";
        line[(gstr)"baseUrl"] = string.IsNullOrWhiteSpace(BaseUrl) ? "https://api.xiaomimimo.com/v1" : BaseUrl.TrimEnd('/');
        line[(gint)"timeoutSeconds"] = Math.Clamp(TimeoutSeconds, 5, 600);
        line.SetBool("ttsEnabled", TtsEnabled);
        line[(gstr)"ttsModel"] = string.IsNullOrWhiteSpace(TtsModel) ? "mimo-v2.5-tts" : TtsModel.Trim();
        line[(gstr)"ttsVoice"] = string.IsNullOrWhiteSpace(TtsVoice) ? "冰糖" : TtsVoice.Trim();
        line[(gstr)"ttsStylePrompt"] = TtsStylePrompt ?? "";
        line.SetBool("ttsSegmentEnabled", TtsSegmentEnabled);
        line[(gint)"ttsMaxSentences"] = Math.Clamp(TtsMaxSentences, 1, 10);
        line[(gint)"ttsSoftMaxChars"] = Math.Clamp(TtsSoftMaxChars, 20, 1000);
        line.SetBool("ttsCutAtCommaAfterLimit", TtsCutAtCommaAfterLimit);
        line.SetBool("asrEnabled", AsrEnabled);
        line[(gstr)"asrModel"] = string.IsNullOrWhiteSpace(AsrModel) ? "mimo-v2.5-asr" : AsrModel.Trim();
        line[(gstr)"asrLanguage"] = NormalizeLanguage(AsrLanguage);
        line[(gint)"asrInputDeviceNumber"] = Math.Max(-1, AsrInputDeviceNumber);
        line[(gstr)"asrInputDeviceName"] = AsrInputDeviceName ?? "";
        line[(gstr)"asrSubmitMode"] = AsrSubmitMode.ToString();
        line[(gstr)"pushToTalkHotkey"] = string.IsNullOrWhiteSpace(PushToTalkHotkey) ? "Ctrl+Alt+M" : PushToTalkHotkey.Trim();
    }

    private static string NormalizeLanguage(string language)
    {
        return language?.Trim().ToLowerInvariant() switch
        {
            "zh" => "zh",
            "en" => "en",
            _ => "auto"
        };
    }

    private static bool GetBool(ILine line, string key, bool defaultValue)
    {
        return bool.TryParse(line.GetString(key, defaultValue.ToString()), out bool value) ? value : defaultValue;
    }
}
