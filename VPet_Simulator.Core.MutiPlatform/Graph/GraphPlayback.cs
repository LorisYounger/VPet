using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Threading.Tasks;
using VPet_Simulator.Core;

namespace VPet_Simulator.Core.MutiPlatform.Graph;

/// <summary>One owner per pooled Image. All ownership checks and writes run on the UI thread.</summary>
internal static class GraphPlayback
{
    public static async Task Run(Image target, IGraphBase graph, GraphTaskControl control, int count, Func<int, Task<IImage?>> getFrame, Func<int, int> duration, Action? firstFrame = null)
    {
        Dispatcher.UIThread.VerifyAccess();
        target.Tag = control;
        bool OwnsTarget() => ReferenceEquals(graph.Control, control) && ReferenceEquals(target.Tag, control);
        int index = 0;
        // Decode the next frame while the current frame is displayed, without blocking a worker.
        Task<IImage?>? pending = count > 0 ? getFrame(0) : null;
        while (pending != null && OwnsTarget() && control.PlayState)
        {
            var frame = await pending;
            if (!OwnsTarget() || !control.PlayState || frame == null)
                break;
            target.Source = frame;
            firstFrame?.Invoke();
            firstFrame = null;
            var delay = Task.Delay(duration(index));
            int next = index + 1;
            pending = next < count ? getFrame(next) : null;
            await delay;
            if (!OwnsTarget() || !control.PlayState)
                break;
            if (next == count)
            {
                if (!graph.IsLoop && control.Type != GraphTaskControl.ControlType.Continue)
                    break;
                if (!graph.IsLoop)
                    control.Type = GraphTaskControl.ControlType.Status_Quo;
                next = 0;
                pending = getFrame(next);
            }
            index = next;
        }
        // A replaced task must never run its old state-machine continuation.
        if (!OwnsTarget())
            return;
        var endAction = control.EndAction;
        control.Type = GraphTaskControl.ControlType.Status_Stoped;
        endAction?.Invoke();
    }
}
