using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using System;
using System.Globalization;

namespace VPet_Simulator.Core.MutiPlatform.Display.Shell;

/// <summary>
/// 环形进度
/// </summary>
/// 对应 Panuon 的 pu:RingProgressBar, 属性名照它的取: Foreground 是已完成那段, BorderBrush/BorderThickness 是底环,
/// Minimum/Maximum/Value 与 ProgressBar 同, IsPercentVisible 决定中间画不画百分比.
/// 自己画而不是找控件库: 就是两段弧, 引一整个库不划算。
public class RingProgress : TemplatedControl
{
    /// <summary>当前值</summary>
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<RingProgress, double>(nameof(Value));

    /// <summary>最小值</summary>
    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<RingProgress, double>(nameof(Minimum));

    /// <summary>最大值</summary>
    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<RingProgress, double>(nameof(Maximum), 100d);

    /// <summary>中间要不要写百分比</summary>
    public static readonly StyledProperty<bool> IsPercentVisibleProperty =
        AvaloniaProperty.Register<RingProgress, bool>(nameof(IsPercentVisible), true);

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public bool IsPercentVisible
    {
        get => GetValue(IsPercentVisibleProperty);
        set => SetValue(IsPercentVisibleProperty, value);
    }

    static RingProgress()
    {
        // 这几个属性一变就得重画
        AffectsRender<RingProgress>(ValueProperty, MinimumProperty, MaximumProperty, BorderThicknessProperty,
            ForegroundProperty, BorderBrushProperty, IsPercentVisibleProperty, FontSizeProperty);
    }

    public override void Render(DrawingContext context)
    {
        var size = Math.Min(Bounds.Width, Bounds.Height);
        if (size <= 0)
            return;
        var thickness = Math.Min(BorderThickness.Left, size / 2);
        var radius = (size - thickness) / 2;
        var center = new Point(Bounds.Width / 2, Bounds.Height / 2);

        if (BorderBrush != null)
        {
            var pen = new Pen(BorderBrush, thickness);
            context.DrawEllipse(null, pen, center, radius, radius);
        }

        var range = Maximum - Minimum;
        var ratio = range <= 0 ? 0 : Math.Clamp((Value - Minimum) / range, 0, 1);
        if (IsPercentVisible)
        {
            var text = new FormattedText(ratio.ToString("p0", CultureInfo.CurrentCulture), CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, new Typeface(FontFamily, FontStyle, FontWeight), FontSize, Foreground);
            context.DrawText(text, new Point(center.X - text.Width / 2, center.Y - text.Height / 2));
        }

        if (Foreground == null || ratio <= 0)
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
