using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using LinePutScript.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using VPet_Simulator.Core.MutiPlatform.Display.Shell;
using VPet_Simulator.Unified.Services;

namespace VPet_Simulator.MutiPlatform.Windows;

/// <summary>
/// 角色面板
/// </summary>
/// 对应 Windows 版的 winCharacterPanel: 上面是当前状态, 下面是年度报告。
///
/// 分档的阈值全在共享后端的 StatsSummary 里, 与 Windows 版是同一批数 —— 这样
/// 同一份存档在两个平台上算出来的报告是一样的。排行百分位这边一律是 0,
/// 因为跨平台版还没接 Steam(见 PetWindow.HostSteam 的说明), 相关档位会退到最低。
internal sealed class CharacterWindow : VPetWindow
{
    private readonly PetWindow host;

    internal CharacterWindow(PetWindow host)
    {
        this.host = host;
        Title = LocalizeCore.Translate("桌宠状态");
        CanResize = true;
        SizeToContent = SizeToContent.Manual;
        Width = 640;
        Height = 620;
        Body = BuildRoot();
    }

    private Control BuildRoot()
    {
        var tabs = new TabControl();
        tabs.Items.Add(new TabItem
        {
            Header = LocalizeCore.Translate("状态"),
            Content = new ScrollViewer { Content = BuildStatus() },
        });
        tabs.Items.Add(new TabItem
        {
            Header = LocalizeCore.Translate("年度报告"),
            Content = new ScrollViewer { Content = BuildReport() },
        });
        return tabs;
    }

    // ---- 状态 ----

    private Control BuildStatus()
    {
        var save = host.HostGameSave.GameSave;
        var panel = new StackPanel { Spacing = 12, Margin = new Thickness(8) };

        panel.Children.Add(new TextBlock
        {
            Text = $"{save.Name}  Lv.{save.Level}",
            FontSize = 20,
            HorizontalAlignment = HorizontalAlignment.Center,
        });

        var money = new TextBlock
        {
            Text = LocalizeCore.Translate("金钱") + $": ${save.Money:f2}",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        panel.Children.Add(money);

        var rings = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 16,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        rings.Children.Add(Ring(LocalizeCore.Translate("体力"), save.Strength, save.StrengthMax));
        rings.Children.Add(Ring(LocalizeCore.Translate("饱腹"), save.StrengthFood, save.StrengthMax));
        rings.Children.Add(Ring(LocalizeCore.Translate("口渴"), save.StrengthDrink, save.StrengthMax));
        rings.Children.Add(Ring(LocalizeCore.Translate("心情"), save.Feeling, save.FeelingMax));
        panel.Children.Add(rings);

        panel.Children.Add(Row(LocalizeCore.Translate("经验"), $"{save.Exp:f0} / {save.LevelUpNeed()}"));
        panel.Children.Add(Row(LocalizeCore.Translate("健康"), $"{save.Health:f0}"));
        panel.Children.Add(Row(LocalizeCore.Translate("好感度"), $"{save.Likability:f1} / {save.LikabilityMax:f0}"));
        panel.Children.Add(Row(LocalizeCore.Translate("状态"), LocalizeCore.Translate(save.Mode.ToString())));
        panel.Children.Add(Row(LocalizeCore.Translate("主人"), save.HostName));

        var birthday = host.HostGameSave.Data.GetDateTime("birthday", DateTime.Now);
        panel.Children.Add(Row(LocalizeCore.Translate("认识于"), birthday.ToString("yyyy-MM-dd")));

        //防作弊一旦关掉就再也开不回来, 玩家有权知道
        if (!host.HostGameSave.HashCheck)
        {
            var warn = new TextBlock
            {
                Text = LocalizeCore.Translate("这份存档的防作弊校验已关闭"),
                Foreground = Brushes.OrangeRed,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            panel.Children.Add(warn);
        }
        return panel;
    }

    private static Control Ring(string name, double value, double max)
    {
        var box = new StackPanel { Spacing = 4 };
        box.Children.Add(new RingProgress
        {
            Width = 72,
            Height = 72,
            Value = value,
            Maximum = max <= 0 ? 1 : max,
            Thickness = 9,
            Foreground = Brushes.DodgerBlue,
            Background = Brushes.LightBlue,
        });
        var label = new TextBlock
        {
            Text = $"{name} {value:f0}",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        label.Classes.Add("vpet-hint");
        box.Children.Add(label);
        return box;
    }

    private static Control Row(string name, string value)
    {
        var row = new DockPanel { LastChildFill = true };
        var left = new TextBlock { Text = name, MinWidth = 90 };
        DockPanel.SetDock(left, Dock.Left);
        row.Children.Add(left);
        row.Children.Add(new TextBlock { Text = value, TextWrapping = TextWrapping.Wrap });
        return row;
    }

    // ---- 年度报告 ----

    private Control BuildReport()
    {
        var save = host.HostGameSave.GameSave;
        var stats = host.HostGameSave.Statistics;
        var panel = new StackPanel { Spacing = 8, Margin = new Thickness(8) };

        int Stat(string name) => stats?.Find(name) == null ? 0 : stats[(LinePutScript.gint)name];
        long Stat64(string name) => stats?.Find(name) == null ? 0 : stats[(LinePutScript.gi64)name];

        var totalSeconds = Stat("stat_total_time");
        var hours = StatsSummary.Hours(totalSeconds);
        var days = (DateTime.Now - host.HostGameSave.Data.GetDateTime("birthday", DateTime.Now)).TotalDays;
        var perDay = StatsSummary.HoursPerDay(hours, days);

        panel.Children.Add(Card(LocalizeCore.Translate("陪伴"), new[]
        {
            (LocalizeCore.Translate("累计时长"), $"{hours:f1} " + LocalizeCore.Translate("小时")),
            (LocalizeCore.Translate("平均每天"), $"{perDay:f1} " + LocalizeCore.Translate("小时")),
            (LocalizeCore.Translate("关系"), CompanionName(StatsSummary.CompanionTier(perDay))),
        }));

        panel.Children.Add(Card(LocalizeCore.Translate("学历"), new[]
        {
            (LocalizeCore.Translate("等级"), save.Level.ToString()),
            (LocalizeCore.Translate("相当于"), StudyName(StatsSummary.StudyTier(save.Level))),
            (LocalizeCore.Translate("学习时长"), $"{Stat("stat_study_time") / 60} " + LocalizeCore.Translate("分钟")),
        }));

        var workSeconds = Stat("stat_work_time");
        panel.Children.Add(Card(LocalizeCore.Translate("打工"), new[]
        {
            (LocalizeCore.Translate("打工时长"), $"{workSeconds / 60} " + LocalizeCore.Translate("分钟")),
            (LocalizeCore.Translate("占在线时长"), $"{StatsSummary.WorkRatio(workSeconds, totalSeconds):p1}"),
        }));

        int buyTimes = Stat(FeedingRules.BuyTimesStat);
        int autoBuy = Stat("stat_autobuy");
        panel.Children.Add(Card(LocalizeCore.Translate("消费"), new[]
        {
            (LocalizeCore.Translate("购买次数"), buyTimes.ToString()),
            (LocalizeCore.Translate("总花费"), $"${(stats?.Find(FeedingRules.TotalSpendStat) == null ? 0 : stats[(LinePutScript.gdbe)FeedingRules.TotalSpendStat]):f2}"),
            (LocalizeCore.Translate("自动购买占比"), $"{StatsSummary.AutoBuyRatio(autoBuy, buyTimes):p1}"),
        }));

        var (length, unit) = StatsSummary.LengthFromPixels(Stat64("stat_move_length"));
        panel.Children.Add(Card(LocalizeCore.Translate("日常"), new[]
        {
            (LocalizeCore.Translate("走过的路"), length + unit),
            (LocalizeCore.Translate("说话次数"), Stat("stat_say_times").ToString()),
            (LocalizeCore.Translate("摸头摸身体"), (Stat("stat_touch_head") + Stat("stat_touch_body")).ToString()),
            (LocalizeCore.Translate("睡觉时长"), $"{StatsSummary.Hours(Stat("stat_sleep_time")):f1} " + LocalizeCore.Translate("小时")),
        }));

        var hint = new TextBlock
        {
            Text = LocalizeCore.Translate("跨平台版还没接 Steam, 排行榜相关的评价暂时看不到"),
            TextWrapping = TextWrapping.Wrap,
        };
        hint.Classes.Add("vpet-hint");
        panel.Children.Add(hint);
        return panel;
    }

    private static Control Card(string title, IEnumerable<(string Name, string Value)> rows)
    {
        var card = new Border();
        card.Classes.Add("vpet-card");
        var panel = new StackPanel { Spacing = 4 };
        panel.Children.Add(new TextBlock { Text = title, FontSize = 16 });
        foreach (var (name, value) in rows)
            panel.Children.Add(Row(name, value));
        card.Child = panel;
        return card;
    }

    /// <summary>
    /// 陪伴档位的名字, 与 Windows 版逐字一致
    /// </summary>
    private static string CompanionName(int tier) => LocalizeCore.Translate(tier switch
    {
        1 => "同学",
        2 => "朋友",
        3 => "挚友",
        4 => "家人",
        _ => "女鹅",
    });

    /// <summary>
    /// 学历档位的名字
    /// </summary>
    private static string StudyName(int tier) => LocalizeCore.Translate(tier switch
    {
        1 => "小学学历",
        2 => "中学学历",
        3 => "大学学历",
        4 => "博士学历",
        _ => "虚拟桌宠模拟器砖家",
    });
}
