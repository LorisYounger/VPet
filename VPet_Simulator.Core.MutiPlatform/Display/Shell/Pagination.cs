using Avalonia.Controls;
using Avalonia.Layout;
using LinePutScript.Localization;
using System;
using System.Collections.Generic;

namespace VPet_Simulator.Core.MutiPlatform.Display.Shell;

/// <summary>
/// 翻页条
/// </summary>
/// 对应 Windows 版 Panuon 的 Pagination. 图库和背包动辄几百项, 一次全画出来
/// 既慢又不好找 —— 分页是必须的。
///
/// 只做"上一页 / 页码 / 下一页", 不做页码列表: 桌宠的窗口都很窄, 一排页码按钮
/// 挤不下, 而且玩家在图库里更常用的是搜索而不是跳到第 17 页。
public class Pagination : StackPanel
{
    private readonly Button previous;
    private readonly Button next;
    private readonly TextBlock label;

    private int page = 1;
    private int pageCount = 1;

    /// <summary>
    /// 当前页 (从 1 开始)
    /// </summary>
    public int Page
    {
        get => page;
        set
        {
            var clamped = Math.Clamp(value, 1, Math.Max(1, pageCount));
            if (clamped == page)
                return;
            page = clamped;
            Refresh();
            PageChanged?.Invoke(page);
        }
    }

    /// <summary>
    /// 总页数
    /// </summary>
    public int PageCount
    {
        get => pageCount;
        set
        {
            pageCount = Math.Max(1, value);
            if (page > pageCount)
                page = pageCount;
            Refresh();
        }
    }

    /// <summary>
    /// 翻页时触发
    /// </summary>
    public event Action<int>? PageChanged;

    public Pagination()
    {
        Orientation = Orientation.Horizontal;
        Spacing = 8;
        HorizontalAlignment = HorizontalAlignment.Center;

        previous = VPetWindow.SecondaryButton("<", () => Page--);
        next = VPetWindow.SecondaryButton(">", () => Page++);
        label = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        label.Classes.Add("vpet-body");

        Children.Add(previous);
        Children.Add(label);
        Children.Add(next);
        Refresh();
    }

    /// <summary>
    /// 按每页几项算出总页数并回到第一页
    /// </summary>
    public void Reset(int itemCount, int pageSize)
    {
        pageCount = pageSize <= 0 ? 1 : Math.Max(1, (itemCount + pageSize - 1) / pageSize);
        page = 1;
        Refresh();
    }

    /// <summary>
    /// 从一整份列表里取出当前页那一段
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

    private void Refresh()
    {
        label.Text = LocalizeCore.Translate("{0} / {1}", page, pageCount);
        previous.IsEnabled = page > 1;
        next.IsEnabled = page < pageCount;
        // 只有一页就整个藏起来, 免得占地方
        IsVisible = pageCount > 1;
    }
}
