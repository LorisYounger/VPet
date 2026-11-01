using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using LinePutScript;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using VPet_Simulator.Core;

namespace VPet_Simulator.Core.MutiPlatform.Graph;

public class FoodAnimation : IAvaloniaRunImageGraph
{
    private readonly GraphCore _graphCore;
    private IAvaloniaGraph? _frontLayer;
    private IAvaloniaGraph? _backLayer;

    public FoodAnimation(GraphCore graphCore, GraphInfo graphInfo, string frontLayer,
        string backLayer, ILine animations, bool isLoop = false)
    {
        IsLoop = isLoop;
        GraphInfo = graphInfo;
        _graphCore = graphCore;
        FrontLayerName = frontLayer;
        BackLayerName = backLayer;
        Animations = new List<Animation>();

        int i = 0;
        ISub? sub = animations.Find("a" + i);
        while (sub != null)
        {
            Animations.Add(new Animation(sub));
            sub = animations.Find("a" + ++i);
        }

        IsReady = true;
    }

    public static void LoadGraph(GraphCore graph, FileSystemInfo path, ILine info)
    {
        bool isLoop = info[(gbol)"loop"];
        var fa = new FoodAnimation(graph, new GraphInfo(path, info), info[(gstr)"front_lay"]!, info[(gstr)"back_lay"]!, info, isLoop);
        graph.AddGraph(fa);
    }

    public string FrontLayerName { get; }
    public string BackLayerName { get; }
    public List<Animation> Animations { get; }
    public int FrameCount => Animations.Count;

    public bool IsLoop { get; set; }
    public GraphInfo GraphInfo { get; private set; }
    public bool IsReady { get; private set; }
    public bool IsFail => false;
    public string FailMessage => string.Empty;
    public object? Control => ControlState;
    public string? Path { get; private set; }
    public long LastUseTimeTicks { get; private set; } = DateTime.UtcNow.Ticks;
    public GraphTaskControl? ControlState { get; private set; }

    public class Animation
    {
        public Thickness MarginWithImage;
        public double Rotate;
        public double Opacity = 1;
        public bool IsVisible = true;
        public double Width;
        public int Time;

        public Animation(ISub sub)
        {
            var strs = sub.GetInfos();
            Time = int.Parse(strs[0]);
            if (strs.Length == 1)
            {
                IsVisible = false;
            }
            else
            {
                Width = double.Parse(strs[3]);
                MarginWithImage = new Thickness(double.Parse(strs[1]), double.Parse(strs[2]), 0, 0);
                if (strs.Length > 4)
                {
                    Rotate = double.Parse(strs[4]);
                    if (strs.Length > 5)
                        Opacity = double.Parse(strs[5]);
                }
            }
        }

        public void Apply(Image target)
        {
            target.IsVisible = IsVisible;
            if (!IsVisible) return;
            target.Margin = MarginWithImage;
            target.RenderTransform = new RotateTransform(Rotate);
            target.Opacity = Opacity;
            target.Width = Width;
            target.Height = Width;
        }
    }

    private async Task RunCore(Image target, GraphTaskControl control)
    {
        target.Tag = control;
        bool OwnsTarget() => ReferenceEquals(ControlState, control) && ReferenceEquals(target.Tag, control);
        int index = 0;
        while (Animations.Count > 0 && OwnsTarget() && control.PlayState)
        {
            var frame = Animations[index];
            frame.Apply(target);
            await Task.Delay(frame.Time);
            if (!OwnsTarget() || !control.PlayState) break;
            if (++index == Animations.Count)
            {
                if (!IsLoop && control.Type != GraphTaskControl.ControlType.Continue) break;
                if (!IsLoop) control.Type = GraphTaskControl.ControlType.Status_Quo;
                index = 0;
            }
        }
        if (!OwnsTarget()) return;
        _frontLayer?.Stop(true);
        _backLayer?.Stop(true);
        var endAction = control.EndAction;
        control.Type = GraphTaskControl.ControlType.Status_Stoped;
        endAction?.Invoke();
    }

    private static Grid CreateGrid()
    {
        var grid = new Grid
        {
            Width = 500,
            Height = 500,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left
        };
        grid.Children.Add(new Image { Name = "Back" });
        grid.Children.Add(new Image
        {
            Name = "Food",
            RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            IsVisible = false,
        });
        grid.Children.Add(new Image { Name = "Front" });
        return grid;
    }

    public void Run(Decorator parent, Action? endAction = null, Action? firstFrame = null) => Run(parent, null, endAction, firstFrame);

    public void Run(Decorator parent, IImage? image, Action? endAction = null, Action? firstFrame = null)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Invoke(() => Run(parent, image, endAction, firstFrame));
            return;
        }
        Stop(true);
        var control = new GraphTaskControl(endAction);
        ControlState = control;
        LastUseTimeTicks = DateTime.UtcNow.Ticks;

        var first = _graphCore.CommUIElements.TryGetValue("FoodGrid1", out var firstControl)
            ? (Grid)firstControl : (Grid)(_graphCore.CommUIElements["FoodGrid1"] = CreateGrid());
        var second = _graphCore.CommUIElements.TryGetValue("FoodGrid2", out var secondControl)
            ? (Grid)secondControl : (Grid)(_graphCore.CommUIElements["FoodGrid2"] = CreateGrid());
        var grid = ReferenceEquals(parent.Child, first) ? first
            : ReferenceEquals(parent.Child, second) ? second
            : first.Parent == null ? first : second;
        parent.Tag = this;
        parent.Child = grid;

        var front = (Image)grid.Children[2];
        var food = (Image)grid.Children[1];
        var back = (Image)grid.Children[0];

        var frontLayer = _graphCore.FindGraph(FrontLayerName, GraphInfo.Animat, GraphInfo.ModeType);
        var backLayer = _graphCore.FindGraph(BackLayerName, GraphInfo.Animat, GraphInfo.ModeType);

        front.Source = null;
        back.Source = null;
        food.Source = image;
        _frontLayer = frontLayer;
        _backLayer = backLayer;
        int remaining = (frontLayer is IAvaloniaImageGraph ? 1 : 0)
            + (backLayer is IAvaloniaImageGraph ? 1 : 0);
        void StartWhenReady()
        {
            if (!ReferenceEquals(ControlState, control) || !control.PlayState) return;
            if (Animations.Count > 0) Animations[0].Apply(food);
            firstFrame?.Invoke();
            _ = RunCore(food, control);
        }
        void LayerReady()
        {
            if (--remaining == 0) StartWhenReady();
        }
        if (remaining == 0)
        {
            StartWhenReady();
        }
        else
        {
            void FailedBeforeFirstFrame()
            {
                if (!ReferenceEquals(ControlState, control) || !control.PlayState) return;
                var completed = control.EndAction;
                Stop(true);
                completed?.Invoke();
            }
            bool frontReady = false, backReady = false;
            if (frontLayer is IAvaloniaImageGraph f)
                _ = f.Run(front, () => { if (!frontReady) FailedBeforeFirstFrame(); },
                    () => { frontReady = true; LayerReady(); });
            if (control.PlayState && backLayer is IAvaloniaImageGraph b)
                _ = b.Run(back, () => { if (!backReady) FailedBeforeFirstFrame(); },
                    () => { backReady = true; LayerReady(); });
        }

    }

    public void Stop(bool stopEndAction)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Invoke(() => Stop(stopEndAction));
            return;
        }
        _frontLayer?.Stop(true);
        _backLayer?.Stop(true);
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
        Animations.Clear();
    }
}
