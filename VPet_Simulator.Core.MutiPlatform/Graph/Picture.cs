using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using SkiaSharp;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VPet_Simulator.Core;

namespace VPet_Simulator.Core.MutiPlatform.Graph;

public class Picture : IAvaloniaImageGraph, IPictureGraphBase
{
    private GraphCore? _graphCore;

    public Picture(GraphCore graphCore, string path, GraphInfo graphInfo, int length = 1000, bool isLoop = false)
    {
        GraphInfo = graphInfo;
        IsLoop = isLoop;
        Length = length;
        _graphCore = graphCore;
        Path = path;
        if (!_graphCore.CommUIElements.ContainsKey("Image.Picture"))
        {
            _graphCore.CommUIElements["Image.Picture"] = new Image { Width = 500, Height = 500 };
        }
        IsReady = true;
    }

    public static void LoadGraph(GraphCore graph, FileSystemInfo path, LinePutScript.ILine info)
    {
        if (path is not FileInfo file)
        {
            PNGAnimation.LoadGraph(graph, path, info);
            return;
        }

        if (file.Extension != ".png")
            return;

        try
        {
            using var stream = File.OpenRead(file.FullName);
            using var codec = SKCodec.Create(stream);
            if (codec != null && codec.FrameCount > 1)
            {
                APNGAnimation.LoadGraph(graph, file, info);
                return;
            }
        }
        catch
        {
        }

        int length = info.GetInt("length");
        if (length == 0)
        {
            var nameParts = file.Name.Split('.');
            if (nameParts.Length > 1 && !int.TryParse(nameParts[nameParts.Length - 2].Split('_').Last(), out length))
                length = 1000;
        }

        bool isLoop = info[(LinePutScript.gbol)"loop"];
        graph.AddGraph(new Picture(graph, file.FullName, new GraphInfo(file, info), length, isLoop));
    }

    public string? Path { get; set; }
    public bool IsLoop { get; set; }
    public int Length { get; set; }
    public GraphInfo GraphInfo { get; private set; }
    public bool IsReady { get; set; }
    public bool IsFail => false;
    public string FailMessage => string.Empty;
    public object? Control => ControlState;
    public long LastUseTimeTicks { get; private set; } = DateTime.UtcNow.Ticks;

    public GraphTaskControl? ControlState { get; private set; }

    public void Run(Decorator parent, Action? endAction = null)
    {
        if (ControlState?.PlayState == true)
        {
            ControlState.SetContinue();
            ControlState.EndAction = endAction;
            return;
        }

        ControlState = new GraphTaskControl(endAction);
        LastUseTimeTicks = DateTime.UtcNow.Ticks;

        Dispatcher.UIThread.Post(() =>
        {
            var image = parent.Child as Image;
            if (image == null)
            {
                image = (_graphCore?.CommUIElements["Image.Picture"] as Image) ?? new Image();
                parent.Child = image;
            }
            image.Width = 500;
            if (Path != null)
            {
                image.Source = new Bitmap(Path);
            }
            Task.Run(() => RunCore(ControlState));
        });
    }

    private void RunCore(GraphTaskControl control)
    {
        Thread.Sleep(Length);
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

    public Task Run(Image image, Action? endAction = null)
    {
        if (ControlState?.PlayState == true)
        {
            ControlState.EndAction = null;
            ControlState.Type = GraphTaskControl.ControlType.Stop;
        }

        ControlState = new GraphTaskControl(endAction);
        LastUseTimeTicks = DateTime.UtcNow.Ticks;
        Dispatcher.UIThread.Post(() =>
        {
            if (Path != null)
            {
                image.Source = new Bitmap(Path);
            }
            image.Width = 500;
        });

        return Task.Run(() => RunCore(ControlState));
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
    }

    public bool Equals(object? other)
    {
        return ReferenceEquals(this, other);
    }

    public void Dispose()
    {
        _graphCore = null;
    }
}
