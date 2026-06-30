using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace VPet_Simulator.Windows;

internal interface ILlmChatClient
{
    Task<string> StreamChatAsync(
        LlmChatConfig config,
        IReadOnlyList<LlmChatMessage> history,
        string userText,
        string systemPrompt,
        CancellationToken cancellationToken,
        Action<string, bool> onDelta);
}
