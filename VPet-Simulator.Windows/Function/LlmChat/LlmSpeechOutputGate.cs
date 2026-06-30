using System.Threading;
using System.Threading.Tasks;

namespace VPet_Simulator.Windows;

internal class LlmSpeechOutputGate
{
    public static LlmSpeechOutputGate Immediate { get; } = new();

    public virtual Task WaitReadyAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public virtual Task WaitPlaybackCompleteAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
