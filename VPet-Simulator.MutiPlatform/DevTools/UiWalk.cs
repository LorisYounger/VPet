using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VPet_Simulator.Core.MutiPlatform;
using VPet_Simulator.Core.MutiPlatform.Display;

namespace VPet_Simulator.MutiPlatform;

/// <summary>
/// 界面走查: 按脚本往真实窗口里注入指针事件, 截图并记录状态
/// </summary>
/// 与 Windows 版的开发控制台 (winConsole) 同性质的调试工具, 只在 --ui-walk 参数下启用.
///
/// 注入的是 Avalonia 的 RawPointerEventArgs, 走的是与真实鼠标完全相同的命中测试、
/// 指针捕获、冒泡和 PointerExited 路径, 但不碰系统光标 —— 验证机器上有人在干活,
/// 不能抢他的键鼠. 弹出层 (菜单) 是独立的 TopLevel, 事件要注入到它自己那一层.
///
/// 脚本一行一条命令, 坐标一律用桌宠的 500×500 布局单位 (与 XAML 里的数字一致):
///   wait 毫秒
///   place x y                                  把桌宠窗口挪到屏幕坐标 (物理像素)
///   rightclick x y | click x y | move x y      注入到桌宠窗口
///   menu 标题                                   点工具栏顶层菜单
///   hover 标题 | item 标题                       悬停 / 点击当前打开的弹出层里的菜单项
///   shot 名字                                   截桌宠窗口和所有弹出层
///   dump 名字                                   记录工具栏/菜单/气泡状态
///   menutree 名字                               把工具栏和托盘的菜单树写成文本 (菜单对齐门禁用)
///   open 类名 | shotwindows 名字 | closewindows  开一个功能窗口 / 截所有功能窗口 / 全关掉
///   winclick 类名 x y                            往某个功能窗口注入一次左键 (窗口逻辑像素)
///   select 类名 控件名 序号                       给某个功能窗口里的下拉框/列表选第几项 (弹出层截不到, 直接设)
///   setprop 类名 控件名 属性 值                   直接改某个功能窗口里控件的一个属性 (例如把等级遮罩藏掉好截图)
///   additem 序号                                往背包里放一份 Foods[序号] (演示背包用, 走查不写存档)
///   quit
internal static class UiWalk
{
    // Pointer 是内部类型, 同样只能反射着建 (与 Inject 里的说明相同)
    private static readonly IInputDevice Mouse = (IInputDevice)Activator.CreateInstance(typeof(MouseDevice),
        Activator.CreateInstance(typeof(MouseDevice).Assembly.GetType("Avalonia.Input.Pointer")!,
            NextPointerId(), PointerType.Mouse, true))!;

    private static int NextPointerId()
        => (int)typeof(MouseDevice).Assembly.GetType("Avalonia.Input.Pointer")!.GetMethod("GetNextFreeId")!.Invoke(null, null)!;

    /// <summary>
    /// 跑一遍脚本
    /// </summary>
    internal static async Task RunAsync(MainWindow window, Main pet, string scriptPath, string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        var log = new StringBuilder();
        // 出错也要留下痕迹, 否则只能看到一个空目录
        File.WriteAllText(Path.Combine(outputDirectory, "walk.log"), "走查已进入" + Environment.NewLine);
        void Note(string text)
        {
            log.AppendLine($"[{DateTime.Now:HH:mm:ss.fff}] {text}");
            MainWindow.Log("走查: " + text);
            // 每条都落盘: 联机走查要在进程还开着的时候读到房间号
            try { File.WriteAllText(Path.Combine(outputDirectory, "walk.log"), log.ToString()); } catch { }
        }

        var toolbar = pet.ToolBar;
        var menu = toolbar.FindControl<Menu>("ToolBarMenu")!;
        double zoom = window.Width / 500;
        Point ToClient(double x, double y) => new Point(x * zoom, y * zoom);

        Note($"开始 缩放={zoom:f2} 窗口={window.Width}x{window.Height}");
        try
        {
            foreach (var raw in File.ReadAllLines(scriptPath))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith('#'))
                    continue;
                var parts = line.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                var command = parts[0].ToLowerInvariant();
                var argument = parts.Length > 1 ? parts[1].Trim() : string.Empty;
                Note("> " + line);
                switch (command)
                {
                    case "wait":
                        await Task.Delay(int.Parse(argument));
                        break;
                    case "rightclick":
                        Click(window, ParsePoint(argument, ToClient), right: true);
                        break;
                    case "click":
                        Click(window, ParsePoint(argument, ToClient), right: false);
                        break;
                    case "move":
                        Inject(window, RawPointerEventType.Move, ParsePoint(argument, ToClient), RawInputModifiers.None);
                        break;
                    case "place":
                    {
                        // 把窗口挪开: 桌宠默认贴在工作区右下角, 走查时那里可能正好有人在用鼠标
                        var xy = argument.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        window.Position = new PixelPoint(int.Parse(xy[0]), int.Parse(xy[1]));
                        break;
                    }
                    case "menu":
                    {
                        var item = menu.Items.OfType<MenuItem>().FirstOrDefault(x => Header(x) == argument);
                        if (item == null) { Note($"  找不到顶层菜单 {argument}"); break; }
                        var center = Center(item, window);
                        Note($"  顶层菜单 {argument} 中心={center}");
                        Click(window, center, right: false);
                        break;
                    }
                    case "hover":
                    case "item":
                    {
                        var found = FindOpenMenuItem(menu, argument);
                        if (found == null) { Note($"  打开的弹出层里找不到 {argument}"); break; }
                        var (item, root) = found.Value;
                        var center = Center(item, root);
                        Note($"  弹出层项 {argument} 中心={center} 层={root.GetType().Name}");
                        // 真鼠标从桌宠窗口挪进弹出层时, 系统会先给桌宠窗口一个离开消息
                        if (root != window)
                            Inject(window, RawPointerEventType.LeaveWindow, new Point(-1, -1), RawInputModifiers.None);
                        if (command == "hover")
                            Inject(root, RawPointerEventType.Move, center, RawInputModifiers.None);
                        else
                            Click(root, center, right: false);
                        break;
                    }
                    case "shot":
                        await Dispatcher.UIThread.InvokeAsync(() => Shoot(window, menu, outputDirectory, argument, Note));
                        break;
                    case "dump":
                        Note("  " + Dump(pet, toolbar, menu));
                        break;
                    case "menutree":
                    {
                        var path = Path.Combine(outputDirectory, argument + ".txt");
                        File.WriteAllText(path, MenuTree(menu), new UTF8Encoding(false));
                        Note($"  菜单树 {Path.GetFileName(path)}");
                        break;
                    }
                    case "open":
                    {
                        // 按类名开一个功能窗口 (非模态, 好截图); 构造参数用固定的演示值
                        var opened = OpenWindow(window, argument);
                        Note(opened == null ? $"  不认识的窗口 {argument}" : $"  已打开 {opened.GetType().Name}");
                        break;
                    }
                    case "shotwindows":
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            int index = 0;
                            foreach (var w in OtherWindows(window))
                            {
                                var size = new PixelSize(Math.Max(1, (int)w.Bounds.Width), Math.Max(1, (int)w.Bounds.Height));
                                using var bitmap = new RenderTargetBitmap(size);
                                bitmap.Render(w);
                                var path = Path.Combine(outputDirectory, $"{argument}-{index++}-{w.GetType().Name}.png");
                                bitmap.Save(path);
                                Note($"  截图 {Path.GetFileName(path)} {size.Width}x{size.Height}");
                            }
                        });
                        break;
                    }
                    case "closewindows":
                        foreach (var w in OtherWindows(window).ToList())
                            w.Close();
                        break;
                    case "select":
                    {
                        var parts3 = argument.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        var target = OtherWindows(window).FirstOrDefault(x => x.GetType().Name == parts3[0]);
                        if (target == null) { Note($"  没开着的窗口 {parts3[0]}"); break; }
                        var control = target.FindControl<SelectingItemsControl>(parts3[1]);
                        if (control == null) { Note($"  窗口里没有 {parts3[1]}"); break; }
                        control.SelectedIndex = int.Parse(parts3[2]);
                        Note($"  {parts3[1]} 选中 {control.SelectedIndex}");
                        break;
                    }
                    case "winplace":
                    {
                        // 把某个功能窗口挪到屏幕坐标 (物理像素), 好用系统截屏对照真实渲染
                        var parts3 = argument.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        var target = OtherWindows(window).FirstOrDefault(x => x.GetType().Name == parts3[0]);
                        if (target == null) { Note($"  没开着的窗口 {parts3[0]}"); break; }
                        target.Position = new PixelPoint(int.Parse(parts3[1]), int.Parse(parts3[2]));
                        break;
                    }
                    case "getprop":
                    {
                        // 读某个功能窗口里控件的一个属性 (字符串里的控制字符转义显示)
                        var parts3 = argument.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
                        var target = OtherWindows(window).FirstOrDefault(x => x.GetType().Name == parts3[0]);
                        if (target == null) { Note($"  没开着的窗口 {parts3[0]}"); break; }
                        var control = parts3[1] == "this" ? target : target.FindControl<Control>(parts3[1]);
                        var value = control?.GetType().GetProperty(parts3[2])?.GetValue(control);
                        Note($"  {parts3[1]}.{parts3[2]} = {(value is string str ? str.Replace("\r", "<CR>").Replace("\n", "<LF>") : value)}");
                        break;
                    }
                    case "setprop":
                    {
                        var parts3 = argument.Split(' ', 4, StringSplitOptions.RemoveEmptyEntries);
                        var target = OtherWindows(window).FirstOrDefault(x => x.GetType().Name == parts3[0]);
                        if (target == null) { Note($"  没开着的窗口 {parts3[0]}"); break; }
                        var control = target.FindControl<Control>(parts3[1]);
                        if (control == null) { Note($"  窗口里没有 {parts3[1]}"); break; }
                        var property = control.GetType().GetProperty(parts3[2]);
                        if (property == null) { Note($"  {parts3[1]} 没有属性 {parts3[2]}"); break; }
                        property.SetValue(control, Convert.ChangeType(parts3[3], Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType,
                            System.Globalization.CultureInfo.InvariantCulture));
                        Note($"  {parts3[1]}.{parts3[2]} = {parts3[3]}");
                        break;
                    }
                    case "winclick":
                    {
                        var parts3 = argument.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                        var target = OtherWindows(window).FirstOrDefault(x => x.GetType().Name == parts3[0]);
                        if (target == null) { Note($"  没开着的窗口 {parts3[0]}"); break; }
                        Click(target, ParsePoint(parts3[1], (x, y) => new Point(x, y)), right: false);
                        break;
                    }
                    case "dumpwin":
                    {
                        // 把某个功能窗口的可视树前几层记下来 (名字/可见/尺寸), 查模板问题用
                        var target = OtherWindows(window).FirstOrDefault(x => x.GetType().Name == argument);
                        if (target == null) { Note($"  没开着的窗口 {argument}"); break; }
                        void Walk(Visual v, int depth)
                        {
                            if (depth > 20) return;
                            Note($"  {new string(' ', depth * 2)}{v.GetType().Name}#{v.Name} 可见={v.IsVisible} 界={v.Bounds}");
                            foreach (var c in v.GetVisualChildren())
                                Walk(c, depth + 1);
                        }
                        Walk(target, 0);
                        // 可视树里挂在弹出层/轮播里的那些用 GetVisualDescendants 再兜一遍 (只记 Name 非空或 Panel 类)
                        foreach (var v in target.GetVisualDescendants().OfType<Panel>())
                            Note($"  * {v.GetType().Name}#{v.Name} 界={v.Bounds} 子={v.Children.Count}");
                        break;
                    }
                    case "gallerydetail":
                    {
                        // 直接开某张照片的详情 (不管锁没锁), 看动图
                        var gallery = OtherWindows(window).OfType<winGallery>().FirstOrDefault();
                        var photo = window.Photos.FirstOrDefault(x => x.Name == argument);
                        if (gallery == null || photo == null) { Note($"  没开图库或没有照片 {argument}"); break; }
                        if (!photo.IsUnlock) photo.Unlock(window);
                        gallery.DisplayDetail(photo);
                        break;
                    }
                    case "say":
                        // 让桌宠说一句, 看消息栏
                        window.Main.Say(argument);
                        break;
                    case "additem":
                        window.ItemsAdd(window.Foods[int.Parse(argument)].Clone());
                        Note($"  背包 {window.Items.Count} 件");
                        break;
                    case "quit":
                        Note("结束");
                        File.WriteAllText(Path.Combine(outputDirectory, "walk.log"), log.ToString());
                        // 走查刻意不走存档 (Environment.Exit 不触发 Closed), 走查机是 Windows, 这里不用顾 macOS
                        Environment.Exit(0);
                        break;
                    case "exit":
                        // 走正常退出 (关闭动画 → Closed → 存档 → 生命周期 Shutdown), 验证退出路径用
                        window.Close();
                        return;
                    default:
                        Note($"  不认识的命令 {command}");
                        break;
                }
                // 让 UI 线程把注入的事件消化掉
                await Task.Delay(150);
            }
        }
        catch (Exception ex)
        {
            Note("出错: " + ex);
        }
        Note("脚本走完");
        File.WriteAllText(Path.Combine(outputDirectory, "walk.log"), log.ToString());
    }

    private static IEnumerable<Window> OtherWindows(Window main)
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            foreach (var w in desktop.Windows)
                if (w != main && w.IsVisible)
                    yield return w;
    }

    /// <summary>
    /// 走查时能开的窗口, 移植一个加一个
    /// </summary>
    private static Window? OpenWindow(MainWindow host, string name)
    {
        // "open winBetterBuy Drink" 这种带一个参数的写法
        var parts = name.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var parameter = parts.Length > 1 ? parts[1] : "";
        switch (parts[0])
        {
            case "winBetterBuy":
                host.winBetterBuy!.Show(Enum.TryParse<Windows.Interface.Food.FoodType>(parameter, out var type) ? type : Windows.Interface.Food.FoodType.Meal);
                return host.winBetterBuy;
            case "winInventory":
                host.winInventory = new winInventory(host);
                host.winInventory.Show();
                return host.winInventory;
            case "winGameSetting":
                host.ShowSetting(int.TryParse(parameter, out var page) ? page : -1);
                return host.winSetting;
            case "winCharacterPanel":
                host.Core.Controller!.ShowPanel();
                return OtherWindows(host).OfType<winCharacterPanel>().LastOrDefault();
            case "winMoveArea":
            {
                var wma = new winMoveArea(host);
                wma.Show(host);
                return wma;
            }
            case "winGallery":
                host.ShowGallery();
                return host.winGallery;
            case "winConsole":
                host.ShowConsole();
                return OtherWindows(host).OfType<winConsole>().LastOrDefault();
            case "winReport":
                host.ShowReport();
                return OtherWindows(host).OfType<winReport>().LastOrDefault();
            case "winWorkMenu":
                host.ShowWorkMenu(Enum.TryParse<GraphHelper.Work.WorkType>(parameter, out var worktype) ? worktype : GraphHelper.Work.WorkType.Work);
                return host.winWorkMenu;
            case "winMutiPlayer":
                // 真的连 Steam: 没参数就创建访客表, 给了十六进制房间号就加入
                if (host.winMutiPlayer == null)
                {
                    host.winMutiPlayer = ulong.TryParse(parameter, System.Globalization.NumberStyles.HexNumber, null, out var lobbyid)
                        ? new winMutiPlayer(host, lobbyid) : new winMutiPlayer(host);
                    host.winMutiPlayer.Show();
                }
                return host.winMutiPlayer;
            case "MPFriends":
            {
                // 一个账号测不了别人的桌宠: 把自己当访客塞进访客表, 好把好友桌宠窗口/访客条/给好友买的商店都画出来
                var wmp = host.winMutiPlayer;
                if (wmp == null || wmp.lb.Equals(default(Steamworks.Data.Lobby))) return null;
                var me = wmp.lb.Members.First(x => x.Id == Steamworks.SteamClient.SteamId);
                var mpf = new MPFriends(wmp, host, wmp.lb, me);
                wmp.MPFriends.Add(mpf);
                mpf.Show();
                var mpuc = new MPUserControl(wmp, mpf);
                wmp.MUUCList.Children.Add(mpuc);
                wmp.MPUserControls.Add(mpuc);
                return mpf;
            }
            case "winMPBetterBuy":
            {
                var mpf = OtherWindows(host).OfType<MPFriends>().FirstOrDefault();
                if (mpf == null) return null;
                mpf.ShowBetterBuy(Enum.TryParse<Windows.Interface.Food.FoodType>(parameter, out var mptype) ? mptype : Windows.Interface.Food.FoodType.Meal);
                return mpf.winMPBetterBuy;
            }
        }
        Window? w = parts[0] switch
        {
            "winInputBox" => new winInputBox(host, "输入框标题", "请输入一段文字", "默认值"),
            _ => null,
        };
        w?.Show(host);
        return w;
    }

    private static Point ParsePoint(string argument, Func<double, double, Point> convert)
    {
        var xy = argument.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return convert(double.Parse(xy[0]), double.Parse(xy[1]));
    }

    private static string Header(MenuItem item) => item.Header?.ToString() ?? string.Empty;

    /// <summary>
    /// 控件中心在其 TopLevel 里的坐标
    /// </summary>
    private static Point Center(Control control, TopLevel root)
    {
        var bounds = control.Bounds;
        return control.TranslatePoint(new Point(bounds.Width / 2, bounds.Height / 2), root) ?? new Point();
    }

    /// <summary>
    /// 在所有打开的弹出层里找一个菜单项
    /// </summary>
    /// 弹出层是 MenuItem 模板里的 Popup, 它的内容挂在独立的 PopupRoot 上
    private static (MenuItem Item, TopLevel Root)? FindOpenMenuItem(Menu menu, string header)
    {
        foreach (var top in menu.Items.OfType<MenuItem>())
        {
            var hit = Search(top);
            if (hit != null)
                return hit;
        }
        return null;

        (MenuItem, TopLevel)? Search(MenuItem parent)
        {
            if (!parent.IsSubMenuOpen)
                return null;
            foreach (var child in parent.Items.OfType<MenuItem>())
            {
                if (Header(child) == header && TopLevel.GetTopLevel(child) is { } root)
                    return (child, root);
                var deeper = Search(child);
                if (deeper != null)
                    return deeper;
            }
            return null;
        }
    }

    private static void Click(TopLevel root, Point point, bool right)
    {
        var down = right ? RawPointerEventType.RightButtonDown : RawPointerEventType.LeftButtonDown;
        var up = right ? RawPointerEventType.RightButtonUp : RawPointerEventType.LeftButtonUp;
        var held = right ? RawInputModifiers.RightMouseButton : RawInputModifiers.LeftMouseButton;
        Inject(root, RawPointerEventType.Move, point, RawInputModifiers.None);
        Inject(root, down, point, held);
        Inject(root, up, point, RawInputModifiers.None);
    }

    /// <summary>
    /// 往一层 TopLevel 注入一个原始指针事件
    /// </summary>
    /// Avalonia 12 把 ITopLevelImpl.Input 和 IInputRoot 标成了内部成员 (Headless 包也是
    /// 靠 InternalsVisibleTo 用它们的), 这里用反射拿. 只有这个调试开关用, 版本升级时
    /// 若断了, 跑一次走查就能发现.
    private static void Inject(TopLevel root, RawPointerEventType type, Point point, RawInputModifiers modifiers)
    {
        var impl = root.PlatformImpl ?? throw new InvalidOperationException("这一层没有平台实现: " + root.GetType().Name);
        var implType = typeof(TopLevel).Assembly.GetType("Avalonia.Platform.ITopLevelImpl")!;
        var input = implType.GetProperty("Input")!.GetValue(impl) as Delegate
            ?? throw new InvalidOperationException("这一层没有输入入口: " + root.GetType().Name);
        // 12 里 TopLevel 自己不再是 IInputRoot, 输入根是它的一个内部属性
        var inputRoot = typeof(TopLevel).GetProperty("InputRoot",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(root)!;
        var ctor = typeof(RawPointerEventArgs).GetConstructors()
            .First(x => x.GetParameters()[4].ParameterType == typeof(Point));
        var args = ctor.Invoke(new object[] { Mouse, (ulong)Environment.TickCount64, inputRoot, type, point, modifiers });
        Dispatcher.UIThread.Invoke(() => input.DynamicInvoke(args));
    }

    /// <summary>
    /// 截桌宠窗口和所有打开的弹出层
    /// </summary>
    private static void Shoot(Window window, Menu menu, string directory, string name, Action<string> note)
    {
        Save(window, Path.Combine(directory, name + ".png"));
        int index = 0;
        foreach (var root in OpenPopupRoots(menu))
            Save(root, Path.Combine(directory, $"{name}-popup{index++}.png"));

        void Save(TopLevel root, string path)
        {
            var size = new PixelSize(Math.Max(1, (int)root.Bounds.Width), Math.Max(1, (int)root.Bounds.Height));
            using var bitmap = new RenderTargetBitmap(size);
            bitmap.Render(root);
            bitmap.Save(path);
            note($"  截图 {Path.GetFileName(path)} {size.Width}x{size.Height}");
        }
    }

    private static IEnumerable<TopLevel> OpenPopupRoots(Menu menu)
    {
        var seen = new HashSet<TopLevel>();
        foreach (var top in menu.Items.OfType<MenuItem>())
            foreach (var root in Walk(top))
                if (seen.Add(root))
                    yield return root;

        static IEnumerable<TopLevel> Walk(MenuItem item)
        {
            if (!item.IsSubMenuOpen)
                yield break;
            foreach (var child in item.Items.OfType<MenuItem>())
            {
                if (TopLevel.GetTopLevel(child) is { } root && root is not Window)
                    yield return root;
                foreach (var deeper in Walk(child))
                    yield return deeper;
            }
        }
    }

    /// <summary>
    /// 工具栏与托盘的菜单树, 供菜单对齐门禁与 Windows 版的清单 diff
    /// </summary>
    /// 一行一项, 两空格缩进; 不可见的标 [hidden], 面板那项标 [hover-panel]
    private static string MenuTree(Menu menu)
    {
        var sb = new StringBuilder();
        foreach (var item in menu.Items.OfType<MenuItem>())
            Write(item, 0);
        sb.AppendLine("tray:");
        if (Application.Current is { } app)
            foreach (var tray in TrayIcon.GetIcons(app) ?? new TrayIcons())
                foreach (var entry in tray.Menu?.Items ?? Enumerable.Empty<NativeMenuItemBase>())
                    if (entry is NativeMenuItem native)
                        sb.AppendLine("  " + native.Header + (native.ToggleType != MenuItemToggleType.None ? " [check]" : "")
                            + (native.IsVisible ? "" : " [hidden]"));
        return sb.ToString();

        void Write(MenuItem item, int depth)
        {
            var line = new string(' ', depth * 2) + Header(item);
            if (item.Name == "MenuPanel")
                line += " [hover-panel]";
            if (!item.IsVisible)
                line += " [hidden]";
            sb.AppendLine(line);
            foreach (var child in item.Items.OfType<MenuItem>())
                Write(child, depth + 1);
        }
    }

    /// <summary>
    /// 一行状态: 工具栏可见/菜单开着/哪些子菜单开着/气泡在说什么/当前动画
    /// </summary>
    private static string Dump(Main pet, ToolBar toolbar, Menu menu)
    {
        return Dispatcher.UIThread.Invoke(() =>
        {
            var open = menu.Items.OfType<MenuItem>().Where(x => x.IsSubMenuOpen).Select(Header).ToList();
            var sub = menu.Items.OfType<MenuItem>().SelectMany(x => x.Items.OfType<MenuItem>())
                .Where(x => x.IsSubMenuOpen).Select(Header).ToList();
            var bubble = pet.MsgBar is MessageBar bar && bar.IsVisible ? bar.TText.Text : null;
            return $"工具栏可见={toolbar.IsVisible} 收起计时={toolbar.CloseTimer.Enabled}"
                + $" 顶层打开=[{string.Join(",", open)}] 二级打开=[{string.Join(",", sub)}]"
                + $" 动画={pet.DisplayType?.Name} 状态={pet.State} 气泡={(bubble == null ? "无" : '"' + bubble + '"')}";
        });
    }
}
