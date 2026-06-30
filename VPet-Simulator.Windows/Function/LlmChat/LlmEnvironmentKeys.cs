using System;

namespace VPet_Simulator.Windows;

internal static class LlmEnvironmentKeys
{
    public static string GetDeepSeekApiKey()
    {
        return GetFirst("DS_API", "DEEPSEEK_API_KEY");
    }

    public static string GetMiMoApiKey()
    {
        return GetFirst("MIMO_API", "MIMO_API_KEY");
    }

    public static string ResolveApiKey(LlmChatProvider provider, string configuredApiKey)
    {
        if (!string.IsNullOrWhiteSpace(configuredApiKey))
            return configuredApiKey;
        return provider switch
        {
            LlmChatProvider.DeepSeek => GetDeepSeekApiKey(),
            LlmChatProvider.MiMo => GetMiMoApiKey(),
            _ => configuredApiKey ?? ""
        };
    }

    public static string ResolveMiMoVoiceApiKey(string configuredApiKey)
    {
        return string.IsNullOrWhiteSpace(configuredApiKey) ? GetMiMoApiKey() : configuredApiKey;
    }

    private static string GetFirst(params string[] names)
    {
        foreach (string name in names)
        {
            string value = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
            value = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User);
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
            value = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Machine);
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }
        return "";
    }
}
