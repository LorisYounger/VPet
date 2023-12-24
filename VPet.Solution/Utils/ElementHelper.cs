using HKW.WPF.Extensions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace HKW.WPF.Helpers;

/// <summary>
///
/// </summary>
public static class ElementHelper
{
    #region IsEnabled
    /// <summary>
    ///
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    public static bool GetIsEnabled(FrameworkElement element)
    {
        return (bool)element.GetValue(IsEnabledProperty);
    }

    /// <summary>
    ///
    /// </summary>
    public static void SetIsEnabled(FrameworkElement element, bool value)
    {
        element.SetValue(IsEnabledProperty, value);
    }

    /// <summary>
    /// 在按下指定按键时清除选中状态
    /// </summary>
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(ElementHelper),
            new FrameworkPropertyMetadata(default(bool), IsEnabledPropertyChangedCallback)
        );

    private static void IsEnabledPropertyChangedCallback(
        DependencyObject obj,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (obj is not FrameworkElement element)
            return;
        element.IsEnabled = GetIsEnabled(element);
    }
    #endregion

    #region ClearFocusOnKeyDown
    /// <summary>
    ///
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    public static string GetClearFocusOnKeyDown(FrameworkElement element)
    {
        return (string)element.GetValue(ClearFocusOnKeyDownProperty);
    }

    /// <summary>
    ///
    /// </summary>
    /// <exception cref="Exception">禁止使用此方法</exception>
    public static void SetClearFocusOnKeyDown(FrameworkElement element, string value)
    {
        element.SetValue(ClearFocusOnKeyDownProperty, value);
    }

    /// <summary>
    /// 在按下指定按键时清除选中状态
    /// </summary>
    public static readonly DependencyProperty ClearFocusOnKeyDownProperty =
        DependencyProperty.RegisterAttached(
            "ClearFocusOnKeyDown",
            typeof(string),
            typeof(ElementHelper),
            new FrameworkPropertyMetadata(default(string), ClearFocusOnKeyDownPropertyChanged)
        );

    private static void ClearFocusOnKeyDownPropertyChanged(
        DependencyObject obj,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (obj is not FrameworkElement element)
            return;
        var keyName = GetClearFocusOnKeyDown(element);
        if (Enum.TryParse<Key>(keyName, false, out _) is false)
            throw new Exception($"Unknown key {keyName}");
        element.KeyDown -= Element_KeyDown;
        element.KeyDown += Element_KeyDown;

        static void Element_KeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not FrameworkElement element)
                return;
            var key = (Key)Enum.Parse(typeof(Key), GetClearFocusOnKeyDown(element));
            if (e.Key == key)
            {
                // 清除控件焦点
                element.ClearFocus();
                // 清除键盘焦点
                Keyboard.ClearFocus();
            }
        }
    }
    #endregion

    #region UniformMinWidthGroup
    /// <summary>
    ///
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    public static string GetUniformMinWidthGroup(FrameworkElement element)
    {
        return (string)element.GetValue(UniformWidthGroupProperty);
    }

    /// <summary>
    ///
    /// </summary>
    public static void SetUniformMinWidthGroup(FrameworkElement element, string value)
    {
        element.SetValue(UniformWidthGroupProperty, value);
    }

    /// <summary>
    ///
    /// </summary>
    public static readonly DependencyProperty UniformWidthGroupProperty =
        DependencyProperty.RegisterAttached(
            "UniformMinWidthGroup",
            typeof(string),
            typeof(ElementHelper),
            new FrameworkPropertyMetadata(null, UniformMinWidthGroupPropertyChanged)
        );

    /// <summary>
    /// (TopParent ,(GroupName, UniformMinWidthGroupInfo))
    /// </summary>
    private readonly static Dictionary<
        FrameworkElement,
        Dictionary<string, UniformMinWidthGroupInfo>
    > _uniformMinWidthGroups = new();

    private static void UniformMinWidthGroupPropertyChanged(
        DependencyObject obj,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (obj is not FrameworkElement element)
            return;
        var groupName = GetUniformMinWidthGroup(element);
        var topParent = element.FindTopParentOnVisualTree();
        // 在设计器中会无法获取顶级元素, 会提示错误, 忽略即可
        if (topParent is null)
            return;
        if (_uniformMinWidthGroups.TryGetValue(topParent, out var groups) is false)
        {
            topParent.Dispatcher.ShutdownStarted += Dispatcher_ShutdownStarted;
            groups = _uniformMinWidthGroups[topParent] = new();
        }
        if (groups.TryGetValue(groupName, out var group) is false)
            group = groups[groupName] = new();
        group.Elements.Add(element);

        element.SizeChanged -= Element_SizeChanged;
        element.SizeChanged += Element_SizeChanged;

        #region TopParent

        static void Dispatcher_ShutdownStarted(object? sender, EventArgs e)
        {
            if (sender is not FrameworkElement element)
                return;
            _uniformMinWidthGroups.Remove(element);
        }
        #endregion

        #region Element
        static void Element_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is not FrameworkElement element)
                return;
            var groupName = GetUniformMinWidthGroup(element);
            var topParent = element.FindTopParentOnVisualTree();
            var groups = _uniformMinWidthGroups[topParent];
            var group = groups[groupName];
            var maxWidthElement = group.Elements.MaxBy(i => i.ActualWidth);
            if (maxWidthElement is null)
                return;

            if (maxWidthElement.ActualWidth == element.ActualWidth)
                maxWidthElement = element;
            if (maxWidthElement.ActualWidth > group.MaxWidth)
            {
                // 如果当前控件最大宽度的超过历史最大宽度, 表明非最大宽度列表中的控件超过了历史最大宽度
                foreach (var item in group.Elements)
                    item.MinWidth = maxWidthElement.ActualWidth;
                // 将当前控件最小宽度设为0
                maxWidthElement.MinWidth = 0;
                group.MaxWidthElements.Clear();
                // 设为最大宽度的唯一控件
                group.MaxWidthElements.Add(maxWidthElement);
                group.MaxWidth = maxWidthElement.ActualWidth;
            }
            else if (group.MaxWidthElements.Count == 1)
            {
                maxWidthElement = group.MaxWidthElements.First();
                // 当最大宽度控件只有一个时, 并且当前控件宽度小于历史最大宽度时, 表明需要降低全体宽度
                if (group.MaxWidth > maxWidthElement.ActualWidth)
                {
                    // 最小宽度设为0以自适应宽度
                    foreach (var item in group.Elements)
                        item.MinWidth = 0;
                    // 清空最大宽度列表, 让其刷新
                    group.MaxWidthElements.Clear();
                }
            }
            else
            {
                // 将 MaxWidth 设置为 double.MaxValue 时, 可以让首次加载时进入此处
                foreach (var item in group.Elements)
                {
                    // 当控件最小宽度为0(表示其为主导宽度的控件), 并且其宽度等于最大宽度, 加入最大宽度列表
                    if (item.MinWidth == 0 && item.ActualWidth == maxWidthElement.ActualWidth)
                    {
                        group.MaxWidthElements.Add(item);
                    }
                    else
                    {
                        // 如果不是, 则从最大宽度列表删除, 并设置最小宽度为当前最大宽度
                        group.MaxWidthElements.Remove(item);
                        item.MinWidth = maxWidthElement.ActualWidth;
                    }
                }
                group.MaxWidth = maxWidthElement.ActualWidth;
            }
        }
        #endregion
    }

    #endregion
}

/// <summary>
/// 统一最小宽度分组信息
/// </summary>
public class UniformMinWidthGroupInfo
{
    /// <summary>
    /// 最后一个最大宽度
    /// </summary>
    public double MaxWidth { get; set; } = double.MaxValue;

    /// <summary>
    /// 所有控件
    /// </summary>
    public HashSet<FrameworkElement> Elements { get; } = new();

    /// <summary>
    /// 最大宽度的控件
    /// </summary>
    public HashSet<FrameworkElement> MaxWidthElements { get; } = new();
}
