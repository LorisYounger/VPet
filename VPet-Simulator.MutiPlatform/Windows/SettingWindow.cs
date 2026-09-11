using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using LinePutScript.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using VPet_Simulator.Core.MutiPlatform;
using VPet_Simulator.Core.MutiPlatform.Display.Shell;
using VPet_Simulator.Unified.Services;

namespace VPet_Simulator.MutiPlatform.Windows;

/// <summary>
/// 设置
/// </summary>
/// 对应 Windows 版的 winGameSetting. 那边有十来页, 这里按"能不能在跨平台上真的
/// 生效"筛过一遍(D-16: 没有跨平台等价物的项直接隐藏, 而不是摆一个点了没反应的开关)。
///
/// 分四页: 桌宠 / 系统 / 多开 / MOD。
internal sealed class SettingWindow : VPetWindow
{
    private readonly PetWindow host;
    private AppSettings Settings => host.HostSettings;

    internal SettingWindow(PetWindow host)
    {
        this.host = host;
        Title = LocalizeCore.Translate("设置");
        CanResize = true;
        SizeToContent = SizeToContent.Manual;
        Width = 620;
        Height = 560;
        Body = BuildRoot();
    }

    private Control BuildRoot()
    {
        var tabs = new TabControl();
        tabs.Items.Add(Tab(LocalizeCore.Translate("桌宠"), BuildPet()));
        tabs.Items.Add(Tab(LocalizeCore.Translate("系统"), BuildSystem()));
        tabs.Items.Add(Tab(LocalizeCore.Translate("多开"), BuildMultiPet()));
        tabs.Items.Add(Tab(LocalizeCore.Translate("MOD"), BuildMods()));
        return tabs;
    }

    private static TabItem Tab(string header, Control content)
        => new TabItem { Header = header, Content = new ScrollViewer { Content = content } };

    // ---- 桌宠 ----

    private Control BuildPet()
    {
        var panel = new StackPanel { Spacing = 12, Margin = new Thickness(8) };

        panel.Children.Add(Slider(LocalizeCore.Translate("缩放"), 0.5, 3, 0.25,
            Settings.ZoomLevel, x =>
            {
                Settings.ZoomLevel = x;
                host.HostSetZoomLevel(x);
            }, x => $"{x:p0}"));

        panel.Children.Add(Toggle(LocalizeCore.Translate("置顶"), Settings.TopMost, x =>
        {
            Settings.TopMost = x;
            host.Topmost = x;
        }));

        panel.Children.Add(Toggle(LocalizeCore.Translate("允许乱跑"), Settings.AllowMove, x =>
        {
            Settings.AllowMove = x;
            host.HostApplyMoveMode();
        }));

        panel.Children.Add(Toggle(LocalizeCore.Translate("智能移动"), Settings.SmartMove, x =>
        {
            Settings.SmartMove = x;
            host.HostApplyMoveMode();
        }));

        panel.Children.Add(Slider(LocalizeCore.Translate("智能移动间隔"), 10, 600, 10,
            Settings.SmartMoveInterval, x =>
            {
                Settings.SmartMoveInterval = (int)x;
                host.HostApplyMoveMode();
            }, x => $"{x:f0} " + LocalizeCore.Translate("秒")));

        panel.Children.Add(Toggle(LocalizeCore.Translate("启用数值计算"), Settings.EnableFunction, x =>
        {
            Settings.EnableFunction = x;
        }));

        panel.Children.Add(Slider(LocalizeCore.Translate("互动周期"), 1, 60, 1,
            Settings.InteractionCycle, x => Settings.InteractionCycle = (int)x,
            x => $"{x:f0}"));

        panel.Children.Add(Slider(LocalizeCore.Translate("长按判定"), 100, 2000, 50,
            Settings.PressLength, x => Settings.PressLength = (int)x,
            x => $"{x:f0} " + LocalizeCore.Translate("毫秒")));

        panel.Children.Add(SaveRow());
        return panel;
    }

    // ---- 系统 ----

    private Control BuildSystem()
    {
        var panel = new StackPanel { Spacing = 12, Margin = new Thickness(8) };

        //主题: 只列真的扫到的那些
        var themes = host.HostResources.Themes;
        if (themes.Count > 0)
        {
            var box = new ComboBox { MinWidth = 200 };
            box.ItemsSource = themes.Select(x => x.TranslateName).ToList();
            box.SelectedIndex = Math.Max(0, themes.FindIndex(x => x.XName == Settings.Theme));
            box.SelectionChanged += (_, _) =>
            {
                if (box.SelectedIndex < 0 || box.SelectedIndex >= themes.Count)
                    return;
                Settings.Theme = themes[box.SelectedIndex].XName;
                host.HostApplyTheme();
            };
            panel.Children.Add(Labeled(LocalizeCore.Translate("主题"), box));
        }

        //语言: 只列真的加载进来的
        var cultures = LocalizeCore.AvailableCultures.ToList();
        if (cultures.Count > 0)
        {
            var box = new ComboBox { MinWidth = 200 };
            box.ItemsSource = cultures;
            box.SelectedIndex = Math.Max(0, cultures.IndexOf(LocalizeCore.CurrentCulture));
            box.SelectionChanged += (_, _) =>
            {
                if (box.SelectedIndex < 0)
                    return;
                Settings.Language = cultures[box.SelectedIndex];
                LocalizeCore.LoadCulture(Settings.Language);
                DialogService.Notice(
                    LocalizeCore.Translate("语言已切换, 重开桌宠之后完全生效"), Title, owner: this);
            };
            panel.Children.Add(Labeled(LocalizeCore.Translate("语言"), box));
        }

        panel.Children.Add(Slider(LocalizeCore.Translate("自动存档间隔"), 1, 30, 1,
            Settings.AutoSaveInterval, x => Settings.AutoSaveInterval = (int)x,
            x => $"{x:f0} " + LocalizeCore.Translate("分钟")));

        panel.Children.Add(Slider(LocalizeCore.Translate("保留几份存档"), 3, 50, 1,
            Settings.BackupSaveMaxNum, x => Settings.BackupSaveMaxNum = (int)x,
            x => $"{x:f0}"));

        var paths = new TextBlock
        {
            Text = LocalizeCore.Translate("数据目录") + $": {AppPaths.DataRoot}\n"
                + LocalizeCore.Translate("MOD 目录") + $": {AppPaths.ModRoot}",
            TextWrapping = TextWrapping.Wrap,
        };
        paths.Classes.Add("vpet-hint");
        panel.Children.Add(paths);

        panel.Children.Add(SaveRow());
        return panel;
    }

    // ---- 多开 ----

    private Control BuildMultiPet()
    {
        var panel = new StackPanel { Spacing = 12, Margin = new Thickness(8) };
        var hint = new TextBlock
        {
            Text = LocalizeCore.Translate(
                "每只桌宠有自己的一套设置和存档。文件名与 Windows 版一致, 整个数据目录可以两边拷。"),
            TextWrapping = TextWrapping.Wrap,
        };
        hint.Classes.Add("vpet-hint");
        panel.Children.Add(hint);

        var list = new StackPanel { Spacing = 6 };
        panel.Children.Add(list);
        RefreshPets(list);

        var name = new TextBox
        {
            Watermark = LocalizeCore.Translate("新桌宠的名字"),
            MinWidth = 200,
        };
        var add = PrimaryButton(LocalizeCore.Translate("新开一只"), () =>
        {
            var prefix = MultiPetStore.Normalize(name.Text);
            if (string.IsNullOrEmpty(prefix))
            {
                DialogService.Notice(LocalizeCore.Translate("名字不能为空"), Title, owner: this);
                return;
            }
            if (App.OpenPets.Any(x => x.HostPrefixSave == prefix))
            {
                DialogService.Notice(LocalizeCore.Translate("这只已经开着了"), Title, owner: this);
                return;
            }
            App.OpenPet(prefix);
            name.Text = string.Empty;
            RefreshPets(list);
        });
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        row.Children.Add(name);
        row.Children.Add(add);
        panel.Children.Add(row);
        return panel;
    }

    private void RefreshPets(StackPanel list)
    {
        list.Children.Clear();
        foreach (var prefix in MultiPetStore.List(AppPaths.DataRoot))
        {
            var display = string.IsNullOrEmpty(prefix)
                ? LocalizeCore.Translate("默认")
                : MultiPetStore.DisplayName(prefix);
            var opened = App.OpenPets.Any(x => x.HostPrefixSave == prefix);

            var card = new Border();
            card.Classes.Add("vpet-card");
            var dock = new DockPanel { LastChildFill = true };

            var captured = prefix;
            var button = opened
                ? SecondaryButton(LocalizeCore.Translate("已开着"), () => { })
                : PrimaryButton(LocalizeCore.Translate("打开"), () =>
                {
                    App.OpenPet(captured);
                    RefreshPets(list);
                });
            button.IsEnabled = !opened;
            DockPanel.SetDock(button, Dock.Right);
            dock.Children.Add(button);

            dock.Children.Add(new TextBlock
            {
                Text = display,
                VerticalAlignment = VerticalAlignment.Center,
            });
            card.Child = dock;
            list.Children.Add(card);
        }
    }

    // ---- MOD ----

    private Control BuildMods()
    {
        var panel = new StackPanel { Spacing = 8, Margin = new Thickness(8) };
        if (host.HostMods.Count == 0)
        {
            panel.Children.Add(BodyText(LocalizeCore.Translate("没有扫描到 MOD")));
            return panel;
        }

        foreach (var mod in host.HostMods)
        {
            var card = new Border();
            card.Classes.Add("vpet-card");
            var box = new StackPanel { Spacing = 4 };

            var head = new DockPanel { LastChildFill = true };
            var captured = mod;
            var on = ModSwitchStore.IsOn(Settings, mod.Name);
            var toggle = new ToggleSwitch { IsChecked = on };
            // Core / PCat 是永远启用的, 关不掉
            toggle.IsEnabled = !ModSwitchStore.AlwaysOn.Contains(mod.Name);
            toggle.IsCheckedChanged += (_, _) =>
            {
                if (toggle.IsChecked == true)
                    ModSwitchStore.On(Settings, captured.Name);
                else
                    ModSwitchStore.Off(Settings, captured.Name);
                Settings.Save();
                DialogService.Notice(
                    LocalizeCore.Translate("重开桌宠之后生效"), Title, owner: this);
            };
            DockPanel.SetDock(toggle, Dock.Right);
            head.Children.Add(toggle);
            head.Children.Add(new TextBlock
            {
                Text = LocalizeCore.Translate(mod.Name),
                FontSize = 15,
                VerticalAlignment = VerticalAlignment.Center,
            });
            box.Children.Add(head);

            var info = new TextBlock
            {
                Text = string.Join("  ", new[]
                {
                    LocalizeCore.Translate(mod.Intro),
                    mod.Author,
                    string.Join("/", mod.Tag),
                }.Where(x => !string.IsNullOrWhiteSpace(x))),
                TextWrapping = TextWrapping.Wrap,
            };
            info.Classes.Add("vpet-hint");
            box.Children.Add(info);

            foreach (var warning in mod.Warnings)
            {
                box.Children.Add(new TextBlock
                {
                    Text = warning,
                    Foreground = Avalonia.Media.Brushes.OrangeRed,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 12,
                });
            }

            card.Child = box;
            panel.Children.Add(card);
        }
        return panel;
    }

    // ---- 控件帮手 ----

    private Control SaveRow()
        => ButtonRow(PrimaryButton(LocalizeCore.Translate("保存设置"), () =>
        {
            Settings.Save();
            DialogService.Notice(LocalizeCore.Translate("设置已保存"), Title, owner: this);
        }));

    private static Control Labeled(string name, Control control)
    {
        var dock = new DockPanel { LastChildFill = true };
        var label = new TextBlock { Text = name, MinWidth = 120, VerticalAlignment = VerticalAlignment.Center };
        DockPanel.SetDock(label, Dock.Left);
        dock.Children.Add(label);
        dock.Children.Add(control);
        return dock;
    }

    private static Control Toggle(string name, bool value, Action<bool> set)
    {
        var toggle = new ToggleSwitch { IsChecked = value };
        toggle.IsCheckedChanged += (_, _) => set(toggle.IsChecked == true);
        return Labeled(name, toggle);
    }

    private static Control Slider(string name, double min, double max, double step,
        double value, Action<double> set, Func<double, string> format)
    {
        var slider = new Slider
        {
            Minimum = min,
            Maximum = max,
            TickFrequency = step,
            IsSnapToTickEnabled = true,
            Value = Math.Clamp(value, min, max),
            MinWidth = 220,
        };
        var text = new TextBlock
        {
            Text = format(slider.Value),
            MinWidth = 80,
            VerticalAlignment = VerticalAlignment.Center,
        };
        slider.PropertyChanged += (_, e) =>
        {
            if (e.Property != RangeBase.ValueProperty)
                return;
            text.Text = format(slider.Value);
            set(slider.Value);
        };
        var row = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(text, Dock.Right);
        row.Children.Add(text);
        row.Children.Add(slider);
        return Labeled(name, row);
    }
}
