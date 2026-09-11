using Avalonia.Controls;
using Avalonia.Layout;
using System;
using System.Collections.Generic;

namespace VPet_Simulator.Core.MutiPlatform.Display.Shell;

/// <summary>
/// 分页的流式面板
/// </summary>
/// 图库和背包用的就是这个: 上面一片按行铺开的格子, 下面一条翻页. 与直接把几百个
/// 格子丢进 ScrollViewer 的区别是每次只建当前页那些控件 —— 图库一页二十张图,
/// 全建出来是几百个 Image, 光解码就要好几秒。
public class PagedWrapPanel<T> : DockPanel
{
    private readonly WrapPanel items = new WrapPanel { Orientation = Orientation.Horizontal };
    private readonly Pagination pagination = new Pagination();
    private readonly Func<T, Control> build;

    private IReadOnlyList<T> source = Array.Empty<T>();

    /// <summary>
    /// 每页几项
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// 当前页显示的项
    /// </summary>
    public IReadOnlyList<T> CurrentPage { get; private set; } = Array.Empty<T>();

    /// <param name="build">怎么把一项画成一个格子</param>
    public PagedWrapPanel(Func<T, Control> build)
    {
        this.build = build;
        SetDock(pagination, Dock.Bottom);
        Children.Add(pagination);
        Children.Add(new ScrollViewer { Content = items });
        pagination.PageChanged += _ => Refresh();
    }

    /// <summary>
    /// 换一批数据, 回到第一页
    /// </summary>
    public void SetSource(IReadOnlyList<T> value)
    {
        source = value ?? Array.Empty<T>();
        pagination.Reset(source.Count, PageSize);
        Refresh();
    }

    /// <summary>
    /// 重画当前页
    /// </summary>
    /// 数据没换但内容变了(比如收藏状态)时调它
    public void Refresh()
    {
        CurrentPage = Pagination.Slice(source, pagination.Page, PageSize);
        items.Children.Clear();
        foreach (var item in CurrentPage)
            items.Children.Add(build(item));
    }
}
