using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Collections.Generic;

namespace VPet_Simulator.Core.MutiPlatform.Display.Shell;

/// <summary>
/// 翻页条
/// </summary>
/// 对应 Windows 版 Panuon 的 Pagination: 属性名 (CurrentPage / MaxPage / Items*) 与事件名
/// (CurrentPageChanged) 都照它的来, 移植过来的 XAML 和代码隐藏可以原样用.
/// 外观是一排页码按钮: 上一页 / 1 … 当前页附近 … 末页 / 下一页, 与 Panuon 的默认样式一致.
public class Pagination : TemplatedControl
{
    public static readonly StyledProperty<int> CurrentPageProperty =
        AvaloniaProperty.Register<Pagination, int>(nameof(CurrentPage), 1, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);
    public static readonly StyledProperty<int> MaxPageProperty =
        AvaloniaProperty.Register<Pagination, int>(nameof(MaxPage), 1);
    public static readonly StyledProperty<Thickness> ItemsPaddingProperty =
        AvaloniaProperty.Register<Pagination, Thickness>(nameof(ItemsPadding), new Thickness(7, 0));
    public static readonly StyledProperty<IBrush?> ItemsBackgroundProperty =
        AvaloniaProperty.Register<Pagination, IBrush?>(nameof(ItemsBackground));
    public static readonly StyledProperty<IBrush?> ItemsSelectedBackgroundProperty =
        AvaloniaProperty.Register<Pagination, IBrush?>(nameof(ItemsSelectedBackground));
    public static readonly StyledProperty<IBrush?> ItemsSelectedForegroundProperty =
        AvaloniaProperty.Register<Pagination, IBrush?>(nameof(ItemsSelectedForeground));
    public static readonly StyledProperty<CornerRadius> ItemsCornerRadiusProperty =
        AvaloniaProperty.Register<Pagination, CornerRadius>(nameof(ItemsCornerRadius), new CornerRadius(2));

    /// <summary>
    /// 当前页 (从 1 开始)
    /// </summary>
    public int CurrentPage
    {
        get => GetValue(CurrentPageProperty);
        set => SetValue(CurrentPageProperty, value);
    }

    /// <summary>
    /// 总页数
    /// </summary>
    public int MaxPage
    {
        get => GetValue(MaxPageProperty);
        set => SetValue(MaxPageProperty, value);
    }

    public Thickness ItemsPadding
    {
        get => GetValue(ItemsPaddingProperty);
        set => SetValue(ItemsPaddingProperty, value);
    }

    public IBrush? ItemsBackground
    {
        get => GetValue(ItemsBackgroundProperty);
        set => SetValue(ItemsBackgroundProperty, value);
    }

    public IBrush? ItemsSelectedBackground
    {
        get => GetValue(ItemsSelectedBackgroundProperty);
        set => SetValue(ItemsSelectedBackgroundProperty, value);
    }

    public IBrush? ItemsSelectedForeground
    {
        get => GetValue(ItemsSelectedForegroundProperty);
        set => SetValue(ItemsSelectedForegroundProperty, value);
    }

    public CornerRadius ItemsCornerRadius
    {
        get => GetValue(ItemsCornerRadiusProperty);
        set => SetValue(ItemsCornerRadiusProperty, value);
    }

    /// <summary>
    /// 翻页时触发, 参数带旧页码与新页码 (对应 Panuon 的 SelectedValueChangedRoutedEventArgs)
    /// </summary>
    public event EventHandler<SelectedValueChangedRoutedEventArgs<int>>? CurrentPageChanged;

    private StackPanel? panel;

    static Pagination()
    {
        CurrentPageProperty.Changed.AddClassHandler<Pagination>((p, e) =>
        {
            p.Rebuild();
            p.CurrentPageChanged?.Invoke(p, new SelectedValueChangedRoutedEventArgs<int>(
                e.GetOldValue<int>(), e.GetNewValue<int>()));
        });
        MaxPageProperty.Changed.AddClassHandler<Pagination>((p, _) =>
        {
            if (p.CurrentPage > p.MaxPage && p.MaxPage > 0)
                p.CurrentPage = p.MaxPage;
            else
                p.Rebuild();
        });
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        panel = e.NameScope.Find<StackPanel>("PART_Items");
        Rebuild();
    }

    /// <summary>
    /// 重画页码按钮: 首尾各留一个, 当前页前后各两个, 中间省略
    /// </summary>
    private void Rebuild()
    {
        if (panel == null)
            return;
        panel.Children.Clear();
        int max = Math.Max(1, MaxPage);
        int current = Math.Clamp(CurrentPage, 1, max);
        panel.Children.Add(MakeButton("<", current > 1, () => CurrentPage = current - 1, false));
        int last = 0;
        foreach (var page in Pages(current, max))
        {
            if (page - last > 1)
                panel.Children.Add(new TextBlock { Text = "…", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Margin = new Thickness(2, 0) });
            int target = page;
            panel.Children.Add(MakeButton(page.ToString(), true, () => CurrentPage = target, page == current));
            last = page;
        }
        panel.Children.Add(MakeButton(">", current < max, () => CurrentPage = current + 1, false));
    }

    private static IEnumerable<int> Pages(int current, int max)
    {
        var set = new SortedSet<int> { 1, max };
        for (int i = current - 2; i <= current + 2; i++)
            if (i >= 1 && i <= max)
                set.Add(i);
        return set;
    }

    private Button MakeButton(string text, bool enabled, Action click, bool selected)
    {
        var button = new Button
        {
            Content = text,
            IsEnabled = enabled,
            Padding = ItemsPadding,
            CornerRadius = ItemsCornerRadius,
            BorderThickness = new Thickness(0),
            Margin = new Thickness(1, 0),
            Background = selected ? ItemsSelectedBackground : ItemsBackground,
            Foreground = selected ? ItemsSelectedForeground : Foreground,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
            FontSize = FontSize,
        };
        button.Click += (_, _) => click();
        return button;
    }

    /// <summary>
    /// 从一整份列表里取出某一页那一段
    /// </summary>
    public static List<T> Slice<T>(IReadOnlyList<T> all, int page, int pageSize)
    {
        var result = new List<T>();
        if (pageSize <= 0)
            return result;
        int start = (page - 1) * pageSize;
        for (int i = start; i < Math.Min(start + pageSize, all.Count); i++)
            result.Add(all[i]);
        return result;
    }
}

/// <summary>
/// 带新旧值的事件参数, 对应 Panuon.WPF.SelectedValueChangedRoutedEventArgs
/// </summary>
public class SelectedValueChangedRoutedEventArgs<T> : RoutedEventArgs
{
    public SelectedValueChangedRoutedEventArgs(T oldValue, T newValue)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }

    public T OldValue { get; }
    public T NewValue { get; }
}
