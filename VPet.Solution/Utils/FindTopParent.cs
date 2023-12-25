using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HKW.WPF.Extensions;

public static partial class WPFExtensions
{
    /// <summary>
    /// 判断是否是顶级元素, 它必须是 <see cref="Window"/>, <see cref="Page"/>, <see cref="UserControl"/> 中的一个
    /// </summary>
    /// <param name="element">元素</param>
    public static bool IsTopParent(this FrameworkElement element)
    {
        if (element is Window || element is Page || element is UserControl)
            return true;
        return false;
    }

    /// <summary>
    /// 寻找它的顶级元素, 它肯定是 <see cref="Window"/>, <see cref="Page"/>, <see cref="UserControl"/> 中的一个
    /// </summary>
    /// <param name="element">元素</param>
    public static FrameworkElement FindTopParent(this FrameworkElement element)
    {
        if (element.IsTopParent())
            return element;
        var parent = element.Parent as FrameworkElement;
        while (parent is not null)
        {
            if (parent.IsTopParent())
                return parent;
            parent = parent.Parent as FrameworkElement;
        }
        return null!;
    }

    /// <summary>
    /// 尝试寻找它的顶级元素, 它肯定是 <see cref="Window"/>, <see cref="Page"/>, <see cref="UserControl"/> 中的一个
    /// </summary>
    /// <param name="element">元素</param>
    /// <param name="topParent">顶级元素</param>
    /// <returns>成功为 <see langword="true"/> 失败为 <see langword="false"/></returns>
    public static bool TryFindTopParent(
        this FrameworkElement element,
        out FrameworkElement topParent
    )
    {
        var result = element.FindTopParent();
        if (result is null)
        {
            topParent = null;
            return false;
        }
        topParent = result;
        return true;
    }

    /// <summary>
    /// 在视觉树上寻找它的顶级元素, 它肯定是 <see cref="Window"/>, <see cref="Page"/>, <see cref="UserControl"/> 中的一个
    /// <para>
    /// 用于 <see cref="FindTopParent"/> 无法获取到顶级元素的情况下使用, 此元素通常位于 <see cref="DataTemplate"/> 中
    /// </para>
    /// </summary>
    /// <param name="element">元素</param>
    public static FrameworkElement FindTopParentOnVisualTree(this FrameworkElement element)
    {
        if (element.TryFindTopParent(out var top))
            return top;
        var temp = (DependencyObject)element;
        while ((temp = VisualTreeHelper.GetParent(temp)) is not null)
        {
            if (temp is FrameworkElement fe && fe.TryFindTopParent(out var topParent))
                return topParent;
        }
        return null!;
    }

    /// <summary>
    /// 尝试在视觉树上寻找它的顶级元素, 它肯定是 <see cref="Window"/>, <see cref="Page"/>, <see cref="UserControl"/> 中的一个
    /// </summary>
    /// <param name="element">元素</param>
    /// <param name="topParent">顶级元素</param>
    /// <returns>成功为 <see langword="true"/> 失败为 <see langword="false"/></returns>
    public static bool FindTopParentOnVisualTree(
        this FrameworkElement element,
        out FrameworkElement topParent
    )
    {
        var result = element.FindTopParentOnVisualTree();
        if (result is null)
        {
            topParent = null;
            return false;
        }
        topParent = result;
        return true;
    }

    /// <summary>
    /// 寻找它的顶级元素, 它肯定是 <see cref="Window"/>, <see cref="Page"/>, <see cref="UserControl"/> 中的一个
    /// </summary>
    /// <param name="element">元素</param>
    public static TParent FindTopParent<TParent>(this FrameworkElement element)
        where TParent : FrameworkElement
    {
        var type = typeof(TParent);
        if (type != typeof(Window) && type != typeof(Page) && type != typeof(UserControl))
            throw new Exception("TParent type must be Window, Page or UserControl");
        return (TParent)FindTopParent(element);
    }
}
