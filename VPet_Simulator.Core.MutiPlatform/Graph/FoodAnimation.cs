using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using LinePutScript;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using VPet_Simulator.Core;

namespace VPet_Simulator.Core.MutiPlatform.Graph;

public class FoodAnimation : IAvaloniaRunImageGraph, IFoodAnimationGraphBase
{
    private readonly GraphCore _graphCore;
    private int _nowId;

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
            Animations.Add(new Animation(this, sub));
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
        private readonly FoodAnimation _parent;
        public Thickness MarginWithImage;
        public double Rotate;
        public double Opacity = 1;
        public bool IsVisible = true;
        public double Width;
        public int Time;

        public Animation(FoodAnimation parent, ISub sub)
        {
            _parent = parent;
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

        public void Run(Image target, GraphTaskControl control)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (IsVisible)
                {
                    target.IsVisible = true;
                    target.Margin = MarginWithImage;
                    target.RenderTransform = new RotateTransform(Rotate);
                    target.Opacity = Opacity;
                    target.Width = Width;
                    target.Height = Width;
                }
                else
                {
                    target.IsVisible = false;
                }
            });

            Thread.Sleep(Time);

            switch (control.Type)
            {
                case GraphTaskControl.ControlType.Stop:
                    control.EndAction?.Invoke();
                    return;
                case GraphTaskControl.ControlType.Status_Stoped:
                    return;
                case GraphTaskControl.ControlType.Status_Quo:
                case GraphTaskControl.ControlType.Continue:
                    if (++_parent._nowId >= _parent.Animations.Count)
                    {
                        if (_parent.IsLoop)
                        {
                            _parent._nowId = 0;
                            Task.Run(() => _parent.Animations[0].Run(target, control));
                            return;
                        }
                        if (control.Type == GraphTaskControl.ControlType.Continue)
                        {
                            control.Type = GraphTaskControl.ControlType.Status_Quo;
                            _parent._nowId = 0;
                        }
                        else
                        {
                            control.Type = GraphTaskControl.ControlType.Status_Stoped;
                            control.EndAction?.Invoke();
                            return;
                        }
                    }
                    _parent.Animations[_parent._nowId].Run(target, control);
                    return;
            }
        }
    }

    public static Grid FoodGrid { get; } = CreateGrid();

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

    public void Run(Decorator parent, Action? endAction = null) => Run(parent, null, endAction);

    public void Run(Decorator parent, IImage? image, Action? endAction = null)
    {
        if (ControlState?.PlayState == true)
        {
            ControlState.Stop(() => Run(parent, image, endAction));
            return;
        }

        _nowId = 0;
        var control = new GraphTaskControl(endAction);
        ControlState = control;
        LastUseTimeTicks = DateTime.UtcNow.Ticks;

        Dispatcher.UIThread.Post(() =>
        {
            if (parent.Child != FoodGrid)
            {
                parent.Child = FoodGrid;
            }

            var front = (Image)FoodGrid.Children[2];
            var food = (Image)FoodGrid.Children[1];
            var back = (Image)FoodGrid.Children[0];

            var frontLayer = _graphCore.FindGraph(FrontLayerName, GraphInfo.Animat, GraphInfo.ModeType);
            var backLayer = _graphCore.FindGraph(BackLayerName, GraphInfo.Animat, GraphInfo.ModeType);

            if (frontLayer is IAvaloniaImageGraph f)
                _ = f.Run(front);
            if (backLayer is IAvaloniaImageGraph b)
                _ = b.Run(back);

            if (image is Bitmap bmp)
            {
                food.Source = bmp;
            }

            if (Animations.Count > 0)
            {
                Task.Run(() => Animations[0].Run(food, control));
            }
            else
            {
                control.EndAction?.Invoke();
            }
        });
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
        Animations.Clear();
    }
}
