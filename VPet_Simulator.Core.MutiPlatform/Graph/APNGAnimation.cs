using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using SkiaSharp;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using VPet_Simulator.Core;

namespace VPet_Simulator.Core.MutiPlatform.Graph;

public class APNGAnimation : IAvaloniaRunImageGraph, IFrameSequenceGraphBase
{
    private readonly GraphCore _graphCore;
    private Bitmap? _bitmap;

    public APNGAnimation(GraphCore graphCore, string path, GraphInfo graphInfo, bool isLoop = false)
    {
        _graphCore = graphCore;
        Path = path;
        GraphInfo = graphInfo;
        IsLoop = isLoop;
        if (!_graphCore.CommUIElements.ContainsKey("Image.APNGAnimation"))
        {
            _graphCore.CommUIElements["Image.APNGAnimation"] = new Image { Height = 500 };
        }
        Task.Run(Startup);
    }

    public static void LoadGraph(GraphCore graph, FileSystemInfo path, LinePutScript.ILine info)
    {
        if (path is not FileInfo file || path.Extension.ToLowerInvariant() != ".png")
            return;

        bool isLoop = info[(LinePutScript.gbol)"loop"];
        graph.AddGraph(new APNGAnimation(graph, file.FullName, new GraphInfo(path, info), isLoop));
    }

    public bool IsLoop { get; set; }
    public bool IsReady { get; private set; }
    public bool IsFail { get; private set; }
    public string FailMessage { get; private set; } = string.Empty;
    public GraphInfo GraphInfo { get; private set; }
    public object? Control => ControlState;
    public string? Path { get; private set; }
    public long LastUseTimeTicks { get; private set; } = DateTime.UtcNow.Ticks;

    public GraphTaskControl? ControlState { get; private set; }
    public int FrameCount { get; private set; }
    public int FrameWidth { get; private set; }
    public int FrameHeight { get; private set; }

    private async Task Startup()
    {
        try
        {
            if (Path == null || !File.Exists(Path))
                throw new FileNotFoundException($"Can not find file: {Path}");

            using (var stream = File.OpenRead(Path))
            using (var codec = SKCodec.Create(stream))
            {
                FrameCount = Math.Max(1, codec?.FrameCount ?? 1);
            }

            _bitmap?.Dispose();
            _bitmap = new Bitmap(Path);
            FrameWidth = _bitmap.PixelSize.Width;
            FrameHeight = _bitmap.PixelSize.Height;

            IsReady = true;
            IsFail = false;
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            IsFail = true;
            FailMessage = $"--APNGAnimation--{GraphInfo}--\nPath: {Path}\n{ex.Message}";
        }
    }

    public void Run(Decorator parent, Action? endAction = null)
    {
        Run(parent, null, endAction);
    }

    public void Run(Decorator parent, IImage? image, Action? endAction = null)
    {
        if (ControlState?.PlayState == true)
        {
            ControlState.EndAction = null;
            ControlState.Type = GraphTaskControl.ControlType.Stop;
        }

        var control = new GraphTaskControl(endAction);
        ControlState = control;
        LastUseTimeTicks = DateTime.UtcNow.Ticks;

        Dispatcher.UIThread.Post(() =>
        {
            var img = parent.Child as Image;
            if (img == null)
            {
                img = (_graphCore.CommUIElements["Image.APNGAnimation"] as Image) ?? new Image();
                parent.Child = img;
            }

            img.Source = image ?? _bitmap;
            img.Height = 500;
            Task.Run(() => RunCore(control));
        });
    }

    private void RunCore(GraphTaskControl control)
    {
        Thread.Sleep(100);
        switch (control.Type)
        {
            case GraphTaskControl.ControlType.Stop:
                control.EndAction?.Invoke();
                return;
            case GraphTaskControl.ControlType.Status_Stoped:
                return;
            case GraphTaskControl.ControlType.Continue:
                control.Type = GraphTaskControl.ControlType.Status_Quo;
                RunCore(control);
                return;
            case GraphTaskControl.ControlType.Status_Quo:
                if (IsLoop)
                {
                    Task.Run(() => RunCore(control));
                }
                else
                {
                    control.Type = GraphTaskControl.ControlType.Status_Stoped;
                    control.EndAction?.Invoke();
                }
                return;
        }
    }

    public void Stop(bool stopEndAction)
    {
        if (ControlState == null)
            return;
        if (stopEndAction)
            ControlState.EndAction = null;
        ControlState.Type = GraphTaskControl.ControlType.Stop;
    }

    public void SetContinue()
    {
        if (ControlState != null)
            ControlState.Type = GraphTaskControl.ControlType.Continue;
    }

    public void Touch()
    {
        LastUseTimeTicks = DateTime.UtcNow.Ticks;
    }

    public void CleanupIdleCache(long nowTicks)
    {
        if (LastUseTimeTicks >= nowTicks || ControlState?.PlayState == true)
            return;

        _bitmap?.Dispose();
        _bitmap = null;
        IsReady = false;
    }

    public bool Equals(object? other)
    {
        return ReferenceEquals(this, other);
    }

    public void Dispose()
    {
        _bitmap?.Dispose();
        _bitmap = null;
    }
}
