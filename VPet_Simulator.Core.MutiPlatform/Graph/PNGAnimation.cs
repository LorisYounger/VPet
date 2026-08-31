using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VPet_Simulator.Core;

namespace VPet_Simulator.Core.MutiPlatform.Graph;

public class PNGAnimation : IAvaloniaImageGraph, IFrameSequenceGraphBase
{
    private readonly GraphCore _graphCore;
    private readonly List<Bitmap> _frames = new();
    private int _nowId;

    public static int MaxLoadMemory = 2000;

    public PNGAnimation(GraphCore graphCore, string path, FileInfo[] paths, GraphInfo graphInfo, bool isLoop = false)
    {
        _graphCore = graphCore;
        Path = path;
        GraphInfo = graphInfo;
        IsLoop = isLoop;
        Animations = new List<string>(paths.Select(p => p.FullName));
        if (!_graphCore.CommUIElements.ContainsKey("Image.PNGAnimation"))
        {
            _graphCore.CommUIElements["Image.PNGAnimation"] = new Image { Height = 500 };
        }
        Task.Run(() => Startup(paths));
    }

    public static void LoadGraph(GraphCore graph, FileSystemInfo path, LinePutScript.ILine info)
    {
        if (path is not DirectoryInfo dir)
        {
            Picture.LoadGraph(graph, path, info);
            return;
        }

        var files = dir.GetFiles("*.png");
        if (files.Length == 0)
            return;
        if (files.Length == 1)
        {
            Picture.LoadGraph(graph, files[0], info);
            return;
        }

        bool isLoop = info[(LinePutScript.gbol)"loop"];
        graph.AddGraph(new PNGAnimation(graph, dir.FullName, files, new GraphInfo(path, info), isLoop));
    }

    public List<string> Animations { get; }
    public bool IsLoop { get; set; }
    public GraphInfo GraphInfo { get; private set; }
    public bool IsReady { get; private set; }
    public bool IsFail { get; private set; }
    public string FailMessage { get; private set; } = string.Empty;
    public object? Control => ControlState;
    public string? Path { get; private set; }
    public long LastUseTimeTicks { get; private set; } = DateTime.UtcNow.Ticks;
    public GraphTaskControl? ControlState { get; private set; }

    public int FrameCount => _frames.Count;
    public int FrameWidth { get; private set; }
    public int FrameHeight { get; private set; }

    private async Task Startup(FileInfo[] paths)
    {
        try
        {
            while (GC.GetGCMemoryInfo().MemoryLoadBytes / 1024.0 / 1024.0 > MaxLoadMemory)
            {
                await Task.Delay(100);
            }

            Array.Sort(paths, (a, b) => string.CompareOrdinal(a.Name, b.Name));
            _frames.Clear();
            foreach (var file in paths)
            {
                var frame = new Bitmap(file.FullName);
                _frames.Add(frame);
            }

            if (_frames.Count > 0)
            {
                FrameWidth = _frames[0].PixelSize.Width;
                FrameHeight = _frames[0].PixelSize.Height;
            }

            IsReady = true;
            IsFail = false;
        }
        catch (Exception ex)
        {
            IsFail = true;
            FailMessage = $"--PNGAnimation--{GraphInfo}--\nPath: {Path}\n{ex.Message}";
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

        _nowId = 0;
        var control = new GraphTaskControl(endAction);
        ControlState = control;
        LastUseTimeTicks = DateTime.UtcNow.Ticks;

        Dispatcher.UIThread.Post(() =>
        {
            var img = parent.Child as Image;
            if (img == null)
            {
                img = (_graphCore.CommUIElements["Image.PNGAnimation"] as Image) ?? new Image();
                parent.Child = img;
            }

            if (_frames.Count > 0)
            {
                img.Source = _frames[0];
                img.Height = 500;
            }
            Task.Run(() => RunCore(img, control));
        });
    }

    public Task Run(Image image, Action? endAction = null)
    {
        if (ControlState?.PlayState == true)
        {
            ControlState.EndAction = null;
            ControlState.Type = GraphTaskControl.ControlType.Stop;
        }

        _nowId = 0;
        var control = new GraphTaskControl(endAction);
        ControlState = control;
        LastUseTimeTicks = DateTime.UtcNow.Ticks;

        Dispatcher.UIThread.Post(() =>
        {
            if (_frames.Count > 0)
            {
                image.Source = _frames[0];
                image.Height = 500;
            }
        });

        return Task.Run(() => RunCore(image, control));
    }

    private void RunCore(Image image, GraphTaskControl control)
    {
        if (_frames.Count == 0)
        {
            control.Type = GraphTaskControl.ControlType.Status_Stoped;
            control.EndAction?.Invoke();
            return;
        }

        Thread.Sleep(100);

        switch (control.Type)
        {
            case GraphTaskControl.ControlType.Stop:
                control.EndAction?.Invoke();
                return;
            case GraphTaskControl.ControlType.Status_Stoped:
                return;
            case GraphTaskControl.ControlType.Status_Quo:
            case GraphTaskControl.ControlType.Continue:
                _nowId++;
                if (_nowId >= _frames.Count)
                {
                    if (IsLoop)
                    {
                        _nowId = 0;
                    }
                    else if (control.Type == GraphTaskControl.ControlType.Continue)
                    {
                        control.Type = GraphTaskControl.ControlType.Status_Quo;
                        _nowId = 0;
                    }
                    else
                    {
                        control.Type = GraphTaskControl.ControlType.Status_Stoped;
                        control.EndAction?.Invoke();
                        return;
                    }
                }

                var index = _nowId;
                Dispatcher.UIThread.Post(() => image.Source = _frames[index]);
                RunCore(image, control);
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

        foreach (var frame in _frames)
        {
            frame.Dispose();
        }
        _frames.Clear();
        IsReady = false;
        IsFail = false;
    }

    public bool Equals(object? other)
    {
        return ReferenceEquals(this, other);
    }

    public void Dispose()
    {
        foreach (var frame in _frames)
        {
            frame.Dispose();
        }
        _frames.Clear();
    }
}
