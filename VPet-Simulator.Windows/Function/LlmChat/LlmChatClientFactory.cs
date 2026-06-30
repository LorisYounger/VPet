namespace VPet_Simulator.Windows;

internal static class LlmChatClientFactory
{
    public static ILlmChatClient Create(LlmChatProvider provider)
    {
        return provider switch
        {
            LlmChatProvider.DeepSeek => new OpenAiCompatibleChatClient("DeepSeek"),
            LlmChatProvider.MiMo => new OpenAiCompatibleChatClient("MiMo", true),
            LlmChatProvider.OpenAI => new OpenAiResponsesChatClient(),
            _ => new OllamaChatClient(),
        };
    }
}
