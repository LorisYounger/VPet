using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using VPet_Simulator.Core;

namespace VPet_Simulator.Core.MutiPlatform.Graph;

public class APNGAnimation : IAvaloniaRunImageGraph
{
    private readonly GraphCore _graphCore;
    /// <summary>
    /// 单帧或解码失败时用的静态图
    /// </summary>
    private Bitmap? _bitmap;
    /// <summary>
    /// 逐帧解码出来的动画帧
    /// </summary>
    private readonly List<Bitmap> _frames = new();
    /// <summary>
    /// 每帧的显示时长(毫秒), 与 _frames 一一对应
    /// </summary>
    private readonly List<int> _frameTimes = new();

    public APNGAnimation(GraphCore graphCore, string path, GraphInfo graphInfo, bool isLoop = false)
    {
        _graphCore = graphCore;
        Path = path;
        GraphInfo = graphInfo;
        IsLoop = isLoop;
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
        // 与 Windows 版一样先等内存降下来再解码: 一个 MOD 里可能有上百个 APNG,
        // 同时开工会把进程直接顶到几个 G
        while (Function.MemoryUsage() > PNGAnimation.MaxLoadMemory)
        {
            await Task.Delay(100);
        }

        try
        {
            if (Path == null || !File.Exists(Path))
                throw new FileNotFoundException($"Can not find file: {Path}");

            DecodeFrames();

            _bitmap?.Dispose();
            _bitmap = _frames.Count > 0 ? null : new Bitmap(Path);
            if (_frames.Count > 0)
            {
                FrameWidth = _frames[0].PixelSize.Width;
                FrameHeight = _frames[0].PixelSize.Height;
            }
            else if (_bitmap != null)
            {
                FrameWidth = _bitmap.PixelSize.Width;
                FrameHeight = _bitmap.PixelSize.Height;
            }

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

    /// <summary>
    /// 逐帧解码 APNG
    /// </summary>
    /// Windows 版是手写 APNG chunk 解析(acTL/fcTL/fdAT)再自行合成; 跨平台侧直接用
    /// SkiaSharp 的 SKCodec, 它原生支持动画 PNG 并且会按 RequiredFrame 处理帧间的
    /// 依赖(上一帧作为底图), 不需要自己实现 DisposeOp/BlendOp.
    ///
    /// 解码失败或只有单帧时保持 _frames 为空, 由调用方退回单张静态图显示 ——
    /// 宁可显示不动, 也不要因为某个畸形 PNG 让整只桌宠起不来.
    private void DecodeFrames()
    {
        foreach (var frame in _frames)
            frame.Dispose();
        _frames.Clear();
        _frameTimes.Clear();

        using var stream = File.OpenRead(Path!);
        using var codec = SKCodec.Create(stream);
        if (codec == null)
            return;

        var frameInfos = codec.FrameInfo;
        FrameCount = Math.Max(1, frameInfos?.Length ?? codec.FrameCount);
        if (frameInfos == null || frameInfos.Length <= 1)
            return;

        var imageInfo = new SKImageInfo(codec.Info.Width, codec.Info.Height,
            SKColorType.Bgra8888, SKAlphaType.Premul);

        // 上一帧的像素要留着, 因为后面的帧可能只记录了增量
        using var previous = new SKBitmap(imageInfo);
        for (int i = 0; i < frameInfos.Length; i++)
        {
            var current = new SKBitmap(imageInfo);
            var required = frameInfos[i].RequiredFrame;
            if (required >= 0)
            {
                // 需要以某一帧为底: 把上一帧的像素拷进来再让 codec 往上叠
                previous.CopyTo(current);
            }

            var options = required >= 0
                ? new SKCodecOptions(i, required)
                : new SKCodecOptions(i);
            if (codec.GetPixels(imageInfo, current.GetPixels(), options) != SKCodecResult.Success)
            {
                current.Dispose();
                break;
            }

            current.CopyTo(previous);
            _frames.Add(ToAvaloniaBitmap(current, imageInfo));
            // Duration 为 0 的帧按 100 毫秒处理, 与常见看图器的做法一致
            _frameTimes.Add(frameInfos[i].Duration > 0 ? frameInfos[i].Duration : 100);
            current.Dispose();
        }

        if (_frames.Count <= 1)
        {
            foreach (var frame in _frames)
                frame.Dispose();
            _frames.Clear();
            _frameTimes.Clear();
        }
    }

    /// <summary>
    /// 把解码出来的像素直接包成 Avalonia 位图
    /// </summary>
    /// 走内存拷贝而不是重新编码成 PNG 再读回来, 省掉每帧一次的编解码开销
    private static Bitmap ToAvaloniaBitmap(SKBitmap source, SKImageInfo info)
    {
        return new Bitmap(
            Avalonia.Platform.PixelFormat.Bgra8888,
            Avalonia.Platform.AlphaFormat.Premul,
            source.GetPixels(),
            new Avalonia.PixelSize(info.Width, info.Height),
            new Avalonia.Vector(96, 96),
            source.RowBytes);
    }

    public void Run(Decorator parent, Action? endAction = null, Action? firstFrame = null)
    {
        Run(parent, null, endAction, firstFrame);
    }

    public void Run(Decorator parent, IImage? image, Action? endAction = null, Action? firstFrame = null)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Invoke(() => Run(parent, image, endAction, firstFrame));
            return;
        }
        Touch();
        if (!IsReady)
        {
            // 用后台线程回调而不是就地调用: 结束动作往往是"重新显示默认动画",
            // 就地调用会同步递归回到这里, 动画迟迟不就绪时会直接栈溢出
            if (endAction != null)
                Task.Run(endAction);
            return;
        }
        // Replace the playback immediately; a pending Stop is not proof the old task exited.
        ControlState?.Stop();
        var control = new GraphTaskControl(endAction);
        ControlState = control;

        if (ReferenceEquals(parent.Tag, this) && parent.Child is Image reuse)
        {
            _ = RunCore(reuse, image, control, firstFrame);
            return;
        }

        var img = GraphImagePool.Attach(parent, _graphCore, "APNGAnimation");
        // Tag 是双缓冲判断"这一层正在放哪个动画"的依据, 必须回写
        parent.Tag = this;
        img.Width = 500;
        _ = RunCore(img, image, control, firstFrame);
    }

    private Task RunCore(Image target, IImage? overrideImage, GraphTaskControl control, Action? firstFrame)
    {
        bool still = overrideImage != null || _frames.Count == 0;
        return GraphPlayback.Run(target, this, control, still ? 1 : _frames.Count,
            index => Task.FromResult<IImage?>(still ? overrideImage ?? _bitmap : _frames[index]),
            index => still ? 100 : _frameTimes[index], firstFrame);
    }

    public void Stop(bool stopEndAction)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Invoke(() => Stop(stopEndAction));
            return;
        }
        if (ControlState == null)
            return;
        if (stopEndAction)
            ControlState.EndAction = null;
        ControlState.Type = GraphTaskControl.ControlType.Stop;
    }

    public void SetContinue()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Invoke(SetContinue);
            return;
        }
        if (ControlState?.PlayState == true)
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

        // 与 PNGAnimation 一样, 静态图可能还挂在隐藏层的 Image 上, 先摘再放
        if (_bitmap != null)
        {
            GraphImagePool.ReleaseBitmaps(_graphCore, new[] { _bitmap });
            _bitmap = null;
        }
        IsReady = false;
    }

    /// <summary>
    /// 动画的相等性一律按引用判断
    /// </summary>
    /// 双缓冲靠 graph.Equals(层的 Tag) 判断"这一层是不是正在放同一个动画",
    /// 必须是引用相等. 写成 override 而不是新方法, 免得从 object 静态类型调用时
    /// 走到不同的实现上去.
    public override bool Equals(object? other) => ReferenceEquals(this, other);

    public override int GetHashCode() => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);

    public void Dispose()
    {
        _bitmap?.Dispose();
        _bitmap = null;
    }
}
