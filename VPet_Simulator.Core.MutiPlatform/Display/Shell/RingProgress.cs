using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;

namespace VPet_Simulator.Core.MutiPlatform.Display.Shell;

/// <summary>
/// 环形进度
/// </summary>
/// 对应 Windows 版角色面板里那几个 Panuon 的环形进度条(体力/饱腹/心情).
/// 自己画而不是找控件库: 就是两段弧, 引一整个库不划算。
public class RingProgress : Control
{
    /// <summary>当前值</summary>
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<RingProgress, double>(nameof(Value));

    /// <summary>最大值</summary>
    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<RingProgress, double>(nameof(Maximum), 100d);

    /// <summary>环的粗细</summary>
    public static readonly StyledProperty<double> ThicknessProperty =
        AvaloniaProperty.Register<RingProgress, double>(nameof(Thickness), 8d);

    /// <summary>已完成那段的颜色</summary>
    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        AvaloniaProperty.Register<RingProgress, IBrush?>(nameof(Foreground));

    /// <summary>底环的颜色</summary>
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<RingProgress, IBrush?>(nameof(Background));

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double Thickness
    {
        get => GetValue(ThicknessProperty);
        set => SetValue(ThicknessProperty, value);
    }

    public IBrush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    static RingProgress()
    {
        // 这几个属性一变就得重画
        AffectsRender<RingProgress>(ValueProperty, MaximumProperty, ThicknessProperty,
            ForegroundProperty, BackgroundProperty);
    }

    public override void Render(DrawingContext context)
    {
        var size = Math.Min(Bounds.Width, Bounds.Height);
        if (size <= 0)
            return;
        var thickness = Math.Min(Thickness, size / 2);
        var radius = (size - thickness) / 2;
        var center = new Point(Bounds.Width / 2, Bounds.Height / 2);

        if (Background != null)
        {
            var pen = new Pen(Background, thickness);
            context.DrawEllipse(null, pen, center, radius, radius);
        }

        if (Foreground == null || Maximum <= 0)
            return;
        var ratio = Math.Clamp(Value / Maximum, 0, 1);
        if (ratio <= 0)
            return;

        var foreground = new Pen(Foreground, thickness) { LineCap = PenLineCap.Round };
        if (ratio >= 1)
        {
            context.DrawEllipse(null, foreground, center, radius, radius);
            return;
        }

        // 从十二点方向顺时针画
        var start = new Point(center.X, center.Y - radius);
        var angle = ratio * Math.PI * 2;
        var end = new Point(
            center.X + radius * Math.Sin(angle),
            center.Y - radius * Math.Cos(angle));

        var figure = new PathFigure
        {
            StartPoint = start,
            IsClosed = false,
            Segments = new PathSegments
            {
                new ArcSegment
                {
                    Point = end,
                    Size = new Size(radius, radius),
                    SweepDirection = SweepDirection.Clockwise,
                    IsLargeArc = ratio > 0.5,
                },
            },
        };
        var geometry = new PathGeometry { Figures = new PathFigures { figure } };
        context.DrawGeometry(null, foreground, geometry);
    }
}
