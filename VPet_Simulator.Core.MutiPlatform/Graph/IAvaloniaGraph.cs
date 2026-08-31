using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Threading.Tasks;
using VPet_Simulator.Core;

namespace VPet_Simulator.Core.MutiPlatform.Graph;

public interface IAvaloniaGraph : IGraphBase
{
    void Run(Decorator parent, Action? endAction = null);
}

public interface IAvaloniaRunImageGraph : IAvaloniaGraph
{
    void Run(Decorator parent, IImage? image, Action? endAction = null);
}

public interface IAvaloniaImageGraph : IAvaloniaGraph
{
    Task Run(Image image, Action? endAction = null);
}
