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
    /// <summary>
    /// 每帧的文件路径, 启动时就确定
    /// </summary>
    private readonly List<string> _framePaths = new();
    /// <summary>
    /// 每帧的显示时长(毫秒), 与 _framePaths 一一对应
    /// </summary>
    private readonly List<int> _frameTimes = new();
    /// <summary>
    /// 已解码的帧, 按需填充
    /// </summary>
    /// 默认宠物有六千多张 png, 全部一次性解码成 Bitmap 需要一两 GB 内存, 而且启动
    /// 要等很久. Windows 版是把整组帧合成一张雪碧图缓存到磁盘来解决的; 这里采取
    /// 更简单的办法 —— 播到哪一帧才解码哪一帧, 空闲超时后由 CleanupIdleCache 释放.
    private readonly Dictionary<int, Bitmap> _frames = new();
    private readonly object _framesLock = new();
    private int _nowId;

    public static int MaxLoadMemory = 2000;

    public PNGAnimation(GraphCore graphCore, string path, FileInfo[] paths, GraphInfo graphInfo, bool isLoop = false)
    {
        _graphCore = graphCore;
        Path = path;
        GraphInfo = graphInfo;
        IsLoop = isLoop;
        Animations = new List<string>(paths.Select(p => p.FullName));
        Task.Run(() => Startup(paths));
    }

    public static void LoadGraph(GraphCore graph, FileSystemInfo path, LinePutScript.ILine info)
    {
        if (path is not DirectoryInfo dir)
        {
            Picture.LoadGraph(graph, path, info);
            return;
        }

        //跨平台: macOS 往不支持扩展属性的盘 (exFAT/FAT 移动硬盘、网络共享) 上写文件, 或解压在 mac 上打的包时, 会给每个文件
        //配一个 "._原名" 的隐藏附属文件 (AppleDouble). 它也叫 *.png 却不是图片, 按文件名排序还会排在正常帧前面当成第一帧.
        //Windows 上不会出现这种文件, 这里跳过
        var files = dir.GetFiles("*.png").Where(f => !f.Name.StartsWith("._", StringComparison.Ordinal)).ToArray();
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

    public int FrameCount => _framePaths.Count;
    public int FrameWidth { get; private set; }
    public int FrameHeight { get; private set; }

    private async Task Startup(FileInfo[] paths)
    {
        try
        {
            // 必须用进程自身的内存占用, 不能用 GC.GetGCMemoryInfo().MemoryLoadBytes ——
            // 那是整机的物理内存占用, 在内存本来就用得多的机器上会永远大于阈值,
            // 于是每个动画都卡在这里永远不就绪, 桌宠一辈子起不来
            while (Function.MemoryUsage() > MaxLoadMemory)
            {
                await Task.Delay(100);
            }

            Array.Sort(paths, (a, b) => string.CompareOrdinal(a.Name, b.Name));
            _framePaths.Clear();
            _frameTimes.Clear();
            foreach (var file in paths)
            {
                _framePaths.Add(file.FullName);
                // 每帧的时长写在文件名最后一个下划线之后, 例如 walk_100.png 表示 100 毫秒.
                // 与 Windows 版一致用 int.Parse: 文件名不合规范时应当让整个动画标记为
                // 失败并显示错误, 而不是悄悄按默认速度播放, 否则 MOD 作者发现不了问题
                var noExtFileName = System.IO.Path.GetFileNameWithoutExtension(file.Name);
                _frameTimes.Add(int.Parse(noExtFileName.Substring(noExtFileName.LastIndexOf('_') + 1)));
            }

            // 启动阶段一张图都不解码. 默认宠物有六百多组动画, 哪怕每组只解首帧,
            // 按原图 1000x1000 算也要两三 GB 内存, 会把内存水位撑爆导致集体卡住.
            // 尺寸等真正解码第一帧时再回填.
            IsReady = true;
            IsFail = false;
            await Task.CompletedTask;
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
        Touch();
        //播放中途发现有帧解不出来的动画 (IsFail, 见 GetFrame) 已经从动画表里摘掉了, 手上还拿着它的调用方也按没就绪处理
        if (!IsReady || IsFail)
        {
            // 用后台线程回调而不是就地调用: 结束动作往往是"重新显示默认动画",
            // 就地调用会同步递归回到这里, 动画迟迟不就绪时会直接栈溢出
            if (endAction != null)
                Task.Run(endAction);
            return;
        }
        if (ControlState?.PlayState == true)
        {//如果当前正在运行,重置状态
            // 必须走 Stop(回调) 让当前帧循环自己收尾后再重新进来, 而不是直接抢占:
            // 否则新旧两个循环会同时往同一个 Image 上写 Source
            ControlState.Stop(() => Run(parent, image, endAction));
            return;
        }

        _nowId = 0;
        var control = new GraphTaskControl(endAction);
        ControlState = control;

        Dispatcher.UIThread.Post(() =>
        {
            if (ReferenceEquals(parent.Tag, this) && parent.Child is Image reuse)
            {
                Task.Run(() => RunCore(reuse, control));
                return;
            }

            var img = GraphImagePool.Attach(parent, _graphCore, "PNGAnimation");
            // Tag 是双缓冲判断"这一层正在放哪个动画"的依据, 必须回写
            parent.Tag = this;
            if (_framePaths.Count > 0)
            {
                img.Source = GetFrame(0);
            }
            img.Width = 500;
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
            if (_framePaths.Count > 0)
            {
                image.Source = GetFrame(0);
                image.Height = 500;
            }
        });

        return Task.Run(() => RunCore(image, control));
    }

    /// <summary>
    /// 播放单帧并推进到下一帧
    /// </summary>
    /// 与 Windows 版 PNGAnimation.Animation.Run 的语义一一对应:
    /// 先显示当前帧, 再按该帧自己的时长等待, 最后才判断是否继续.
    private void RunCore(Image image, GraphTaskControl control)
    {
        if (_framePaths.Count == 0)
        {
            control.Type = GraphTaskControl.ControlType.Status_Stoped;
            control.EndAction?.Invoke();
            return;
        }

        var index = _nowId;
        var frame = GetFrame(index);
        if (frame == null)
        {//这一帧解不出来: 整个动画已标记失败并摘掉 (见 GetFrame), 本次播放按结束处理, 桌宠会换别的动画
            control.Type = GraphTaskControl.ControlType.Status_Stoped;
            control.EndAction?.Invoke();
            return;
        }
        //先显示该帧
        Dispatcher.UIThread.Post(() => image.Source = frame);
        //然后等待这一帧自己的时长
        Thread.Sleep(_frameTimes[index]);

        //判断是否要下一步
        switch (control.Type)
        {
            case GraphTaskControl.ControlType.Stop:
                control.EndAction?.Invoke();
                return;
            case GraphTaskControl.ControlType.Status_Stoped:
                return;
            case GraphTaskControl.ControlType.Status_Quo:
            case GraphTaskControl.ControlType.Continue:
                if (++_nowId >= _framePaths.Count)
                {
                    if (IsLoop)
                    {
                        _nowId = 0;
                        // 循环动画必须重新起一个线程, 否则一直递归下去会栈溢出
                        Task.Run(() => RunCore(image, control));
                        return;
                    }
                    else if (control.Type == GraphTaskControl.ControlType.Continue)
                    {
                        control.Type = GraphTaskControl.ControlType.Status_Quo;
                        _nowId = 0;
                    }
                    else
                    {
                        control.Type = GraphTaskControl.ControlType.Status_Stoped;
                        control.EndAction?.Invoke(); //运行结束动画时事件
                        return;
                    }
                }
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

    /// <summary>
    /// 取得指定帧, 没解码过就现解并缓存; 解不出来返回 null
    /// </summary>
    /// 跨平台: Windows 版启动时就把整组帧解码拼成雪碧图, 坏帧 (0 字节 / 不是图片 / 头部损坏) 在那一步被 catch 住,
    /// 整组动画 IsFail, Main.Load_2_WaitGraph 把它从动画表里摘掉并记进 ErrorMessage, 宿主弹 "动画加载错误", 桌宠换别的动画接着跑.
    /// 这边按帧惰性解码, 坏帧要到播放时才暴露 —— 以前是在 UI 线程上直接抛 NullReferenceException (Skia 认不出格式时解码器为 null)
    /// 把程序带崩. 现在照 Windows 的结果处理: 标记失败 (FailMessage 格式相同, 多记一个文件名), 从动画表里摘掉并通知宿主提示
    private Bitmap? GetFrame(int index)
    {
        lock (_framesLock)
        {
            if (_frames.TryGetValue(index, out var cached))
                return cached;
            if (IsFail)
                return null;
            try
            {
                // 按渲染分辨率降采样解码, 而不是把原图整张读进来.
                // 素材是 1000x1000 的, 而桌宠实际只显示几百像素, 直接解原图既慢又占内存.
                // Windows 版是通过预先合成缩放后的雪碧图达到同样目的.
                using var stream = File.OpenRead(_framePaths[index]);
                var width = _graphCore.Resolution > 0 ? _graphCore.Resolution : 500;
                var frame = Bitmap.DecodeToWidth(stream, width);
                _frames[index] = frame;
                if (FrameWidth == 0)
                {
                    FrameWidth = frame.PixelSize.Width;
                    FrameHeight = frame.PixelSize.Height;
                }
                return frame;
            }
            catch (Exception e)
            {
                IsFail = true;
                FailMessage = $"--PNGAnimation--{GraphInfo}--\nPath: {Path}\n{System.IO.Path.GetFileName(_framePaths[index])}: {e.Message}";
            }
        }
        // 出了帧锁再通知: 宿主那边会写日志、弹窗
        _graphCore.RemoveFailedGraph(this);
        return null;
    }

    public void CleanupIdleCache(long nowTicks)
    {
        if (LastUseTimeTicks >= nowTicks || ControlState?.PlayState == true)
            return;

        // 只丢弃解码后的位图, 帧路径和时长留着 —— 它们很轻, 而且丢了就得重新扫目录.
        // 注意这里不能像之前那样把 IsReady 置回 false: 调用方看到未就绪会立刻回调
        // 结束动作, 而结束动作往往又是"重新显示默认动画", 于是同步递归到栈溢出.
        // 位图不能在这里直接 Dispose: 最后一帧可能还挂在隐藏层的 Image 上, 交给
        // GraphImagePool.ReleaseBitmaps 先摘再放 (见那边的注释).
        List<Bitmap> old;
        lock (_framesLock)
        {
            old = new List<Bitmap>(_frames.Values);
            _frames.Clear();
        }
        GraphImagePool.ReleaseBitmaps(_graphCore, old);
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
        lock (_framesLock)
        {
            foreach (var frame in _frames.Values)
            {
                frame.Dispose();
            }
            _frames.Clear();
        }
    }
}
