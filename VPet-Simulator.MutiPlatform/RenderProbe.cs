using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VPet_Simulator.Core.MutiPlatform.Display.Shell;

namespace VPet_Simulator.MutiPlatform;

/// <summary>
/// 渲染探针
/// </summary>
/// `--probe <名字>` 会把指定界面渲染成 PNG 然后退出, 不弹窗也不需要桌宠跑起来。
///
/// 存在的理由很实际: 迁移期间没有 Linux/macOS 机器可以实机验证(所有者已确认),
/// 而"界面画出来是什么样"是截图之外没法确认的事. 有了它, 拿到机器之后跑一条
/// 命令就能把两个平台的渲染结果摆在一起比, 而不是靠人肉点一遍。
///
/// 也用来在改样式之后快速看一眼有没有画崩, 不用每次都把桌宠启动一遍。
internal static class RenderProbe
{
    /// <summary>
    /// 能画的界面
    /// </summary>
    /// 新界面做出来之后往这里加一条, 探针就能画它
    private static readonly Dictionary<string, Func<Control>> Views =
        new Dictionary<string, Func<Control>>(StringComparer.OrdinalIgnoreCase)
        {
            ["styles"] = BuildStyleSheet,
            ["dialog"] = BuildDialogSample,
            ["controls"] = BuildControlSample,
            ["shop"] = BuildShopSample,
            ["work"] = BuildWorkSample,
            ["character"] = BuildCharacterSample,
            ["setting"] = BuildSettingSample,
            ["gallery"] = BuildGallerySample,
            ["talk"] = BuildTalkSample,
        };

    /// <summary>
    /// 能画的界面名
    /// </summary>
    internal static IEnumerable<string> Names => Views.Keys;

    /// <summary>
    /// 画一张出来
    /// </summary>
    /// <param name="name">界面名</param>
    /// <param name="outputPath">PNG 路径</param>
    /// <returns>画成功了返回 true</returns>
    internal static bool Render(string name, string outputPath)
    {
        if (!Views.TryGetValue(name, out var factory))
        {
            Console.WriteLine($"没有叫 {name} 的界面. 可用的: {string.Join(", ", Views.Keys)}");
            return false;
        }

        return Dispatcher.UIThread.Invoke(() =>
        {
            var size = new Size(520, 640);
            // 必须画一个真的窗口, 不能拿脱离可视树的控件直接画:
            // Button / TextBox 这些是模板控件, 模板要等它接进某个 TopLevel 才展开,
            // 应用级样式也是那时候才匹配上 —— 直接画只会得到一片空白.
            // 这也让探针顺带覆盖了窗口本身的样式.
            var window = new Window
            {
                Width = size.Width,
                Height = size.Height,
                WindowDecorations = WindowDecorations.None,
                ShowInTaskbar = false,
                // 挪到屏幕外: 探针不该在玩家眼前闪一下
                Position = new PixelPoint(-32000, -32000),
                Content = factory(),
            };
            window.Show();
            try
            {
                window.Measure(size);
                window.Arrange(new Rect(size));
                window.UpdateLayout();

                // 96 DPI: 与桌面默认一致, 两个平台画出来的像素尺寸才对得上
                using var bitmap = new RenderTargetBitmap(
                    new PixelSize((int)size.Width, (int)size.Height), new Vector(96, 96));
                bitmap.Render(window);

                var directory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);
                bitmap.Save(outputPath);
            }
            finally
            {
                window.Close();
            }
            Console.WriteLine($"已渲染 {name} -> {Path.GetFullPath(outputPath)}");
            return true;
        });
    }

    /// <summary>
    /// 把通用样式都摆一遍
    /// </summary>
    /// 改了 VPetStyles.axaml 之后看这张就知道有没有画崩
    private static Control BuildStyleSheet()
    {
        var panel = new StackPanel { Spacing = 12, Margin = new Thickness(16) };

        var title = new Border { Padding = new Thickness(12, 8) };
        title.Classes.Add("vpet-title");
        title.Child = new TextBlock { Text = "标题栏 Title Bar" };
        panel.Children.Add(title);

        panel.Children.Add(VPetWindow.BodyText("正文: 桌宠今天也很有精神。The quick brown fox jumps over the lazy dog."));

        var hint = new TextBlock { Text = "说明文字: 这一行是弱化的提示", TextWrapping = Avalonia.Media.TextWrapping.Wrap };
        hint.Classes.Add("vpet-hint");
        panel.Children.Add(hint);

        var card = new Border();
        card.Classes.Add("vpet-card");
        card.Child = VPetWindow.BodyText("卡片里的内容");
        panel.Children.Add(card);

        panel.Children.Add(VPetWindow.ButtonRow(
            VPetWindow.SecondaryButton("次要按钮", () => { }),
            VPetWindow.PrimaryButton("主按钮", () => { })));

        panel.Children.Add(new ProgressBar { IsIndeterminate = false, Value = 60, Maximum = 100, MinWidth = 280 });
        panel.Children.Add(new TextBox { Text = "输入框", MinWidth = 280 });

        var root = new Border { Background = Avalonia.Media.Brushes.White };
        root.Child = panel;
        return root;
    }

    /// <summary>
    /// 图库的格子长什么样
    /// </summary>
    /// 重点是看"没解锁"那种格子够不够清楚 —— 图库里多半是没解锁的
    private static Control BuildGallerySample()
    {
        var panel = new WrapPanel { Margin = new Thickness(16) };
        var samples = new (string Name, bool Unlocked, string Detail)[]
        {
            ("初次见面", true, "日常 回忆"),
            ("生日快乐", true, "节日 生日"),
            ("毕业照", false, "等级要求: 60"),
            ("中秋赏月", false, "解锁节日: 中秋"),
        };
        foreach (var (name, unlocked, detail) in samples)
        {
            var cell = new Border { Width = 170, Height = 210, Margin = new Thickness(4) };
            cell.Classes.Add("vpet-card");
            var dock = new DockPanel { LastChildFill = true };
            var top = new StackPanel { Spacing = 4 };
            DockPanel.SetDock(top, Dock.Top);
            dock.Children.Add(top);
            top.Children.Add(new Border
            {
                Height = 110,
                Background = unlocked
                    ? new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(180, 220, 250))
                    : new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(40, 0, 0, 0)),
                Child = new TextBlock
                {
                    Text = unlocked ? "(照片)" : "未解锁",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                },
            });
            top.Children.Add(new TextBlock { Text = name, HorizontalAlignment = HorizontalAlignment.Center });
            var hint = new TextBlock { Text = detail, FontSize = 12, TextWrapping = Avalonia.Media.TextWrapping.Wrap };
            hint.Classes.Add("vpet-hint");
            top.Children.Add(hint);
            if (unlocked)
                dock.Children.Add(new CheckBox
                {
                    Content = "喜欢",
                    VerticalAlignment = VerticalAlignment.Bottom,
                });
            cell.Child = dock;
            panel.Children.Add(cell);
        }
        var root = new Border { Background = Avalonia.Media.Brushes.White };
        root.Child = panel;
        return root;
    }

    /// <summary>
    /// 选项式聊天框长什么样
    /// </summary>
    /// 底下垫一块工具栏那种深蓝, 看得出它是浮在工具栏顶部的
    private static Control BuildTalkSample()
    {
        var texts = new List<VPet_Simulator.Core.MutiPlatform.SelectText>
        {
            new() { Choose = "今天过得怎么样?", Text = "还不错哦, 谢谢主人关心" },
            new() { Choose = "我们出去玩吧", Text = "好耶! 去哪里?", Feeling = 5 },
            new() { Choose = "你饿了吗", Text = "有一点点饿了", StrengthFood = -1 },
        };
        var selector = new VPet_Simulator.Core.MutiPlatform.TalkSelector(() => texts, _ => true);
        var talk = new Display.TalkSelect(selector, _ => { });
        var root = new Grid { Background = Avalonia.Media.Brushes.White };
        root.Children.Add(new Border
        {
            Height = 60,
            VerticalAlignment = VerticalAlignment.Bottom,
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FF039be5")),
        });
        root.Children.Add(talk);
        return root;
    }

    /// <summary>
    /// 设置页长什么样
    /// </summary>
    private static Control BuildSettingSample()
    {
        var panel = new StackPanel { Spacing = 12, Margin = new Thickness(16) };

        foreach (var (name, on) in new[] { ("置顶", true), ("允许乱跑", true), ("智能移动", false) })
        {
            var dock = new DockPanel { LastChildFill = true };
            var toggle = new ToggleSwitch { IsChecked = on };
            DockPanel.SetDock(toggle, Dock.Right);
            dock.Children.Add(toggle);
            dock.Children.Add(new TextBlock { Text = name, VerticalAlignment = VerticalAlignment.Center });
            panel.Children.Add(dock);
        }

        foreach (var (name, value, text) in new[] { ("缩放", 1.0, "100%"), ("自动存档间隔", 5.0, "5 分钟") })
        {
            var dock = new DockPanel { LastChildFill = true };
            var label = new TextBlock { Text = name, MinWidth = 120, VerticalAlignment = VerticalAlignment.Center };
            DockPanel.SetDock(label, Dock.Left);
            dock.Children.Add(label);
            var right = new TextBlock { Text = text, MinWidth = 80, VerticalAlignment = VerticalAlignment.Center };
            DockPanel.SetDock(right, Dock.Right);
            dock.Children.Add(right);
            dock.Children.Add(new Slider { Minimum = 0, Maximum = 10, Value = value, MinWidth = 220 });
            panel.Children.Add(dock);
        }

        // 多开那一页的卡片
        foreach (var (name, opened) in new[] { ("默认", true), ("小黑", false) })
        {
            var card = new Border();
            card.Classes.Add("vpet-card");
            var dock = new DockPanel { LastChildFill = true };
            var button = opened ? VPetWindow.SecondaryButton("已开着", () => { }) : VPetWindow.PrimaryButton("打开", () => { });
            button.IsEnabled = !opened;
            DockPanel.SetDock(button, Dock.Right);
            dock.Children.Add(button);
            dock.Children.Add(new TextBlock { Text = name, VerticalAlignment = VerticalAlignment.Center });
            card.Child = dock;
            panel.Children.Add(card);
        }

        var root = new Border { Background = Avalonia.Media.Brushes.White };
        root.Child = panel;
        return root;
    }

    /// <summary>
    /// 角色面板长什么样
    /// </summary>
    private static Control BuildCharacterSample()
    {
        var panel = new StackPanel { Spacing = 12, Margin = new Thickness(16) };
        panel.Children.Add(new TextBlock { Text = "萝莉丝  Lv.37", FontSize = 20, HorizontalAlignment = HorizontalAlignment.Center });
        panel.Children.Add(new TextBlock { Text = "金钱: $12345.67", HorizontalAlignment = HorizontalAlignment.Center });

        var rings = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 16,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        foreach (var (name, value) in new[] { ("体力", 82d), ("饱腹", 64d), ("口渴", 47d), ("心情", 91d) })
        {
            var box = new StackPanel { Spacing = 4 };
            box.Children.Add(new RingProgress
            {
                Width = 72,
                Height = 72,
                Value = value,
                Maximum = 100,
                Thickness = 9,
                Foreground = Avalonia.Media.Brushes.DodgerBlue,
                Background = Avalonia.Media.Brushes.LightBlue,
            });
            var label = new TextBlock { Text = $"{name} {value:f0}", HorizontalAlignment = HorizontalAlignment.Center };
            label.Classes.Add("vpet-hint");
            box.Children.Add(label);
            rings.Children.Add(box);
        }
        panel.Children.Add(rings);

        foreach (var (title, rows) in new (string, (string, string)[])[]
        {
            ("陪伴", new[] { ("累计时长", "213.5 小时"), ("平均每天", "4.2 小时"), ("关系", "挚友") }),
            ("消费", new[] { ("购买次数", "482"), ("总花费", "$9931.20"), ("自动购买占比", "31.5%") }),
        })
        {
            var card = new Border();
            card.Classes.Add("vpet-card");
            var box = new StackPanel { Spacing = 4 };
            box.Children.Add(new TextBlock { Text = title, FontSize = 16 });
            foreach (var (name, value) in rows)
            {
                var row = new DockPanel { LastChildFill = true };
                var left = new TextBlock { Text = name, MinWidth = 90 };
                DockPanel.SetDock(left, Dock.Left);
                row.Children.Add(left);
                row.Children.Add(new TextBlock { Text = value });
                box.Children.Add(row);
            }
            card.Child = box;
            panel.Children.Add(card);
        }

        var root = new Border { Background = Avalonia.Media.Brushes.White };
        root.Child = panel;
        return root;
    }

    /// <summary>
    /// 工作面板的格子长什么样
    /// </summary>
    private static Control BuildWorkSample()
    {
        var panel = new WrapPanel { Margin = new Thickness(16) };
        var samples = new (string Name, string Detail, int NeedLevel)[]
        {
            ("打工", "时长 30 分钟\n收益 2.5/分钟\n饱腹 -0.4\n口渴 -0.5", 0),
            ("写代码", "时长 60 分钟\n收益 8.0/分钟\n饱腹 -0.8\n心情 -0.3", 0),
            ("当律师", "时长 90 分钟\n收益 20.0/分钟\n饱腹 -1.2\n心情 -0.8", 40),
        };
        foreach (var (name, detail, need) in samples)
        {
            var cell = new Border { Width = 190, Height = 180, Margin = new Thickness(4) };
            cell.Classes.Add("vpet-card");
            var dock = new DockPanel { LastChildFill = true };
            var top = new StackPanel { Spacing = 4 };
            DockPanel.SetDock(top, Dock.Top);
            dock.Children.Add(top);
            top.Children.Add(new TextBlock
            {
                Text = name,
                HorizontalAlignment = HorizontalAlignment.Center,
                FontSize = 15,
            });
            var hint = new TextBlock { Text = detail, FontSize = 12, TextWrapping = Avalonia.Media.TextWrapping.Wrap };
            hint.Classes.Add("vpet-hint");
            top.Children.Add(hint);
            if (need > 0)
                top.Children.Add(new TextBlock
                {
                    Text = $"需要等级 {need}",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = Avalonia.Media.Brushes.OrangeRed,
                    FontSize = 12,
                });
            var start = VPetWindow.PrimaryButton("开始", () => { });
            start.IsEnabled = need == 0;
            start.VerticalAlignment = VerticalAlignment.Bottom;
            dock.Children.Add(start);
            cell.Child = dock;
            panel.Children.Add(cell);
        }
        var root = new Border { Background = Avalonia.Media.Brushes.White };
        root.Child = panel;
        return root;
    }

    /// <summary>
    /// 商店的格子长什么样
    /// </summary>
    /// 画的是真的格子(Windows.ShopCell), 不是照着样子另写的一份 —— 另写一份的话
    /// 探针就只是在检查探针自己, 改了真格子也看不出来。数据是假的, 摆法是真的。
    private static Control BuildShopSample()
    {
        var panel = new WrapPanel { Margin = new Thickness(16) };
        var samples = new (string Name, double Price, (string, double)[] Values, bool Over)[]
        {
            ("可乐", 9.0, new[] { ("经验", 4d), ("口渴", 50d), ("心情", 50d) }, false),
            ("汉堡", 25.0, new[] { ("经验", 12d), ("饱腹", 60d), ("体力", 10d) }, false),
            ("感冒药", 48.0, new[] { ("健康", 30d), ("心情", -5d) }, false),
            ("神秘礼物", 1.0, new[] { ("好感", 99d), ("心情", 99d) }, true),
        };
        foreach (var (name, price, values, over) in samples)
        {
            panel.Children.Add(Windows.ShopCell.Build(
                name,
                $"${price:f1}",
                over ? "超模" : null,
                Windows.ShopCell.Describe(values),
                null,
                "购买",
                () => { }));
        }
        // 背包那一页的格子: 没有价格, 按钮是"使用"
        panel.Children.Add(Windows.ShopCell.Build(
            "每日礼包 ×3", null, null, "物品系统附赠的每日礼包, 打开后会获得3个随机物品",
            null, "使用", () => { }));
        var root = new Border { Background = Avalonia.Media.Brushes.White };
        root.Child = panel;
        return root;
    }

    /// <summary>
    /// 自己写的那几个控件长什么样
    /// </summary>
    /// 环形进度、翻页条、分页流式面板 —— 它们是自己画的, 更需要看一眼
    private static Control BuildControlSample()
    {
        var panel = new StackPanel { Spacing = 16, Margin = new Thickness(16) };

        var rings = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16 };
        foreach (var (value, name) in new[] { (25d, "体力"), (60d, "饱腹"), (95d, "心情"), (100d, "满") })
        {
            var box = new StackPanel { Spacing = 4 };
            box.Children.Add(new RingProgress
            {
                Width = 72,
                Height = 72,
                Value = value,
                Maximum = 100,
                Thickness = 9,
                Foreground = Avalonia.Media.Brushes.DodgerBlue,
                Background = Avalonia.Media.Brushes.LightBlue,
            });
            var label = new TextBlock { Text = $"{name} {value:f0}%", HorizontalAlignment = HorizontalAlignment.Center };
            label.Classes.Add("vpet-hint");
            box.Children.Add(label);
            rings.Children.Add(box);
        }
        panel.Children.Add(rings);

        var paged = new PagedWrapPanel<int>(x =>
        {
            var cell = new Border { Width = 84, Height = 56, Margin = new Thickness(4) };
            cell.Classes.Add("vpet-card");
            cell.Child = new TextBlock
            {
                Text = "格子 " + x,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            return cell;
        })
        {
            PageSize = 8,
            Height = 260,
        };
        paged.SetSource(Enumerable.Range(1, 30).ToList());
        panel.Children.Add(paged);

        var root = new Border { Background = Avalonia.Media.Brushes.White };
        root.Child = panel;
        return root;
    }

    /// <summary>
    /// 对话框长什么样
    /// </summary>
    private static Control BuildDialogSample()
    {
        var panel = new StackPanel { Spacing = 8, Margin = new Thickness(16) };
        panel.Children.Add(VPetWindow.BodyText(
            "MOD「示例模组」带有代码插件。\n代码插件能做的事和游戏本身一样多, 请只放行你信任来源的 MOD。\n\n文件: Demo.dll\n指纹: 1A2B3C4D5E6F7788\n\n要加载它吗?"));
        panel.Children.Add(VPetWindow.ButtonRow(
            VPetWindow.SecondaryButton("否", () => { }),
            VPetWindow.PrimaryButton("是", () => { })));

        var body = new Border { Padding = new Thickness(4) };
        body.Child = panel;

        var title = new Border { Padding = new Thickness(12, 8) };
        title.Classes.Add("vpet-title");
        title.Child = new TextBlock { Text = "是否加载代码插件" };

        var dock = new DockPanel();
        DockPanel.SetDock(title, Dock.Top);
        dock.Children.Add(title);
        dock.Children.Add(body);

        var root = new Border
        {
            Background = Avalonia.Media.Brushes.White,
            Child = new Border
            {
                Margin = new Thickness(24),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Child = dock,
            },
        };
        return root;
    }
}
