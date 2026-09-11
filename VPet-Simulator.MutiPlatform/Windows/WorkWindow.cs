using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using LinePutScript.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using VPet_Simulator.Core.MutiPlatform;
using VPet_Simulator.Core.MutiPlatform.Display.Shell;

namespace VPet_Simulator.MutiPlatform.Windows;

/// <summary>
/// 工作菜单
/// </summary>
/// 对应 Windows 版的 winWorkMenu, 但只做它的前半段: 挑一份工作/学习/玩耍开工。
///
/// 后半段(日程表、打工中介套餐)没做, 理由写在 OpenSchedule 那儿 —— 简单说是
/// ScheduleTask 通篇拿着 IMainWindow, 硬搬过来只会拆出一个没人用的壳。
internal sealed class WorkWindow : VPetWindow
{
    private readonly PetWindow host;
    private readonly TabControl tabs = new TabControl();

    internal WorkWindow(PetWindow host)
    {
        this.host = host;
        Title = LocalizeCore.Translate("工作");
        CanResize = true;
        SizeToContent = SizeToContent.Manual;
        Width = 680;
        Height = 520;
        Body = BuildRoot();
    }

    private Control BuildRoot()
    {
        var pet = host.HostPet;
        if (pet == null)
            return BodyText(LocalizeCore.Translate("桌宠还没准备好"));

        pet.WorkList(out var works, out var studies, out var plays);
        AddTab(LocalizeCore.Translate("工作"), works);
        AddTab(LocalizeCore.Translate("学习"), studies);
        AddTab(LocalizeCore.Translate("玩耍"), plays);
        return tabs;
    }

    private void AddTab(string header, List<GraphHelper.Work> list)
    {
        var grid = new PagedWrapPanel<GraphHelper.Work>(BuildWorkCell) { PageSize = 9 };
        // 等级要求低的排前面: 玩家等级低时能选的都在最前, 不用翻页找
        grid.SetSource(list.OrderBy(x => x.LevelLimit).ThenByDescending(x => x.MoneyBase).ToList());
        tabs.Items.Add(new TabItem { Header = header, Content = grid });
    }

    private Control BuildWorkCell(GraphHelper.Work work)
    {
        var save = host.HostGameSave.GameSave;
        bool locked = save.Level < work.LevelLimit;

        var cell = new Border
        {
            Width = 190,
            Height = 180,
            Margin = new Thickness(4),
        };
        cell.Classes.Add("vpet-card");

        var panel = new DockPanel { LastChildFill = true };
        var top = new StackPanel { Spacing = 4 };
        DockPanel.SetDock(top, Dock.Top);
        panel.Children.Add(top);

        top.Children.Add(new TextBlock
        {
            Text = work.NameTrans,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center,
            FontSize = 15,
        });

        var detail = new TextBlock
        {
            Text = Describe(work),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
        };
        detail.Classes.Add("vpet-hint");
        top.Children.Add(detail);

        if (locked)
        {
            var need = new TextBlock
            {
                Text = LocalizeCore.Translate("需要等级 {0}", work.LevelLimit),
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = Brushes.OrangeRed,
                FontSize = 12,
            };
            top.Children.Add(need);
        }

        var start = PrimaryButton(LocalizeCore.Translate("开始"), () => Start(work));
        start.IsEnabled = !locked;
        start.VerticalAlignment = VerticalAlignment.Bottom;
        panel.Children.Add(start);

        cell.Child = panel;
        return cell;
    }

    /// <summary>
    /// 一份工作的收益与消耗
    /// </summary>
    private static string Describe(GraphHelper.Work work)
    {
        var parts = new List<string>
        {
            LocalizeCore.Translate("时长 {0} 分钟", work.Time),
        };
        if (work.MoneyBase != 0)
            parts.Add(work.Type == GraphHelper.Work.WorkType.Study
                ? LocalizeCore.Translate("经验 {0:f1}/分钟", work.MoneyBase)
                : LocalizeCore.Translate("收益 {0:f1}/分钟", work.MoneyBase));
        //消耗是每分钟扣的, 与 Windows 版工作计时器上显示的是同一批数
        if (work.StrengthFood != 0)
            parts.Add(LocalizeCore.Translate("饱腹 -{0:f1}", work.StrengthFood));
        if (work.StrengthDrink != 0)
            parts.Add(LocalizeCore.Translate("口渴 -{0:f1}", work.StrengthDrink));
        if (work.Feeling != 0)
            parts.Add(work.Feeling > 0
                ? LocalizeCore.Translate("心情 +{0:f1}", work.Feeling)
                : LocalizeCore.Translate("心情 {0:f1}", work.Feeling));
        return string.Join("\n", parts);
    }

    private async void Start(GraphHelper.Work work)
    {
        var pet = host.HostPet;
        if (pet == null)
            return;
        //能不能开工由 PetMain 自己判(体力、状态、是不是已经在干别的),
        //这里只负责把结果告诉玩家 —— 判定逻辑不该在界面里再抄一遍
        if (!pet.StartWork(work))
        {
            await DialogService.ShowAsync(
                LocalizeCore.Translate("现在没法开始 {0}\n可能是体力不够, 或者桌宠正在忙别的", work.NameTrans),
                Title, this);
            return;
        }
        Close();
    }
}
