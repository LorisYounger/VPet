//跨平台: 原文复制自 VPet-Simulator.Windows/Function/UnifiedPluginHost.cs; 只换了本地化/MessageBoxX/MenuItem 的命名空间
using LinePutScript;
using LinePutScript.Localization;
using VPet_Simulator.Core.MutiPlatform.Display.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using VPet_Simulator.Core;
using VPet_Simulator.Core.MutiPlatform;
using VPet_Simulator.Core.MutiPlatform.Display;
using VPet_Simulator.Unified.Interface;
using VPet_Simulator.Windows.Interface;
using static VPet_Simulator.Core.MutiPlatform.GraphHelper;

namespace VPet_Simulator.MutiPlatform;

/// <summary>
/// 统一 MOD 契约在 Windows 侧的宿主实现
/// </summary>
/// 每个插件实例、每个窗口一个. 与旧的 MainPlugin 体系并列存在: 旧插件仍然经
/// CoreMOD 那条老路加载, 语义一点没变; 按统一契约写的插件走这里, 好处是同一个 dll
/// 在跨平台版上也能跑.
///
/// 有一条要格外注意: GameSavesData 在读档时会被**整个替换**(MainWindow.cs:971),
/// 所以所有跟存档有关的东西都必须每次调用时重新取, 不能在构造时缓存.
internal sealed partial class UnifiedPluginHost : IPetHost
{
    /// <summary>
    /// 最近一次在用的窗口
    /// </summary>
    /// 包装器里 Item.Use / Photo.Unlock 这些方法要求传 IMainWindow, 而包装器是按
    /// 对象缓存的、不属于任何一个窗口. 认不出归属时用它兜底.
    internal static MainWindow? CurrentWindow;

    /// <summary>
    /// 这件物品是哪个窗口的
    /// </summary>
    /// 多开时每个窗口有自己的背包和照片墙, 用错窗口会把效果结算到别的桌宠身上.
    /// 物品对象只会待在一个窗口的列表里, 所以按归属找得回来.
    internal static MainWindow? WindowOf(Item item)
    {
        foreach (var w in App.MainWindows)
            if (w.Items.Contains(item))
                return w;
        return CurrentWindow;
    }

    /// <summary>
    /// 这张照片是哪个窗口的
    /// </summary>
    internal static MainWindow? WindowOf(Photo photo)
    {
        foreach (var w in App.MainWindows)
            if (w.Photos.Contains(photo))
                return w;
        return CurrentWindow;
    }

    private readonly MainWindow mw;
    private readonly CoreMOD mod;
    private readonly List<IDisposable> closables = new();
    private readonly UnifiedWrap.SaveView saveView;
    private readonly UnifiedWrap.StatisticsView statisticsView;
    private readonly UnifiedItemRegistry itemRegistry;
    private readonly UnifiedPetView petView;

    /// <summary>
    /// 插件本体
    /// </summary>
    public UnifiedPlugin Plugin { get; private set; } = null!;

    /// <summary>
    /// LoadPlugin 是否已经跑过
    /// </summary>
    internal bool Loaded { get; private set; }

    private UnifiedPluginHost(MainWindow mw, CoreMOD mod)
    {
        this.mw = mw;
        this.mod = mod;
        CurrentWindow = mw;
        saveView = new UnifiedWrap.SaveView(() => mw.GameSavesData.GameSave);
        statisticsView = new UnifiedWrap.StatisticsView(() => mw.GameSavesData.Statistics);
        itemRegistry = new UnifiedItemRegistry(this, mw);
        petView = new UnifiedPetView(mw);
        steam = new SteamCapability(mw);
    }

    /// <summary>
    /// 建立宿主并实例化插件
    /// </summary>
    /// 调用时机在 CoreMOD 构造期, 也就是**存档加载之前** —— 插件构造函数里注册的
    /// 物品创建器正好赶得上存档反序列化.
    internal static UnifiedPluginHost? Create(MainWindow mw, CoreMOD mod, Type pluginType)
    {
        var host = new UnifiedPluginHost(mw, mod);
        var plugin = Activator.CreateInstance(pluginType, host) as UnifiedPlugin;
        if (plugin == null)
            return null;
        host.Plugin = plugin;
        mw.UnifiedHosts.Add(host);
        return host;
    }

    // ---- 生命周期 ----

    internal void OnLoadPlugin()
    {
        Loaded = true;
        petView.Attach();
        // 存档已经读完了, 这时才把挂起的物品行补上
        itemRegistry.FlushPending();
        Plugin.LoadPlugin();
    }

    internal void OnEndGame()
    {
        try
        {
            Plugin.EndGame();
        }
        finally
        {
            petView.Detach();
            // 逆序释放: 后登记的通常依赖先登记的
            for (int i = closables.Count - 1; i >= 0; i--)
            {
                try { closables[i].Dispose(); }
                catch (Exception ex) { Log($"释放失败: {ex.Message}"); }
            }
            closables.Clear();
        }
    }

    // ---- 宿主能力 ----

    public string HostName => "Windows";
    public int HostVersion => mw.version;
    public bool IsUIThread => mw.Dispatcher.CheckAccess();
    public void RunOnUI(Action action) => mw.Dispatcher.Invoke(action);
    public T RunOnUI<T>(Func<T> func) => mw.Dispatcher.Invoke(func);
    public string Translate(string text) => LocalizeCore.Translate(text);
    public string Translate(string text, params object[] args) => LocalizeCore.Translate(text, args);
    // Console 已经被重定向进 ActivityLogs (MainWindow.cs:1419)
    public void Log(string message) => Console.WriteLine($"[{Plugin?.PluginName ?? mod.Name}] {message}");
    public void ShowMessage(string text, string title = "")
        => RunOnUI(() => NoticeBox.Show(text, string.IsNullOrEmpty(title) ? mod.Name : title));
    public void ShowInputBox(string title, string text, string defaultText, Action<string> end, bool allowMultiLine = false)
        => RunOnUI(() => mw.ShowInputBox(title, text, defaultText, end, allowMultiLine));

    /// <summary>
    /// 取宿主专属能力
    /// </summary>
    /// 用了它这个 MOD 就只能在 Windows 上跑了 —— 这是有意留的逃生舱, 不是推荐用法.
    /// Main 在插件构造期还是 null(要到 MainWindow.cs:1800 才建), 所以取的时候要判空.
    public T? GetService<T>() where T : class
        //Steam 能力是跨平台的(契约里的 ISteamServices), 放在这里 MOD 不用管平台
        => new object?[] { steam, mw, mw.Main, mw.Core, mw.Set, mw.GameSavesData, mw.Dispatcher }
            .OfType<T>().FirstOrDefault();

    /// <summary>
    /// Steam 能力
    /// </summary>
    /// 每个宿主一个, 不共享 —— 它拿着 mw
    private readonly SteamCapability steam;

    // ---- 插件与 MOD ----

    public IPluginInfo Info => new UnifiedWrap.PluginInfoView(mod, IsModEnabled);
    public IEnumerable<IPluginInfo> Mods => mw.ModInfo.Select(x => (IPluginInfo)new UnifiedWrap.PluginInfoView(x, IsModEnabled));
    public IEnumerable<IPluginInfo> OnMods => mw.OnModInfo.Select(x => (IPluginInfo)new UnifiedWrap.PluginInfoView(x, IsModEnabled));
    public IReadOnlyList<string> ModPaths => mw.MODPath.Select(x => x.FullName).ToList();
    public string DataDirectory => AppPaths.DataRoot;
    public string GetModStorage(string modName) => ExtensionValue.GetMODStorage(modName);
    public string PrefixSave => mw.PrefixSave;

    private bool IsModEnabled(string name) => mw.OnModInfo.Any(x => x.Name == name);

    // ---- 设置与存档 ----

    public ILine Setting(string lineName) => mw.Set[lineName];
    public ILPS SaveData => mw.GameSavesData.Data;
    public IPetSave Save => saveView;
    public IPetStatistics Statistics => statisticsView;
    public bool HashCheck => mw.HashCheck;
    public void HashCheckOff() => mw.HashCheckOff();
    public void SaveGame() => mw.Save();
    public IDictionary<string, object> DynamicResources => mw.DynamicResources;

    // ---- 数据表 ----

    public IReadOnlyList<IFoodInfo> Foods => mw.Foods.Select(UnifiedWrap.Of).ToList();
    public IReadOnlyList<IPhotoInfo> Photos => mw.Photos.Select(UnifiedWrap.Of).ToList();

    public IReadOnlyList<ITextInfo> Texts(PetTextKind kind) => kind switch
    {
        PetTextKind.LowFood => mw.LowFoodText.Select(x => (ITextInfo)new UnifiedWrap.TextView(x, kind)).ToList(),
        PetTextKind.LowDrink => mw.LowDrinkText.Select(x => (ITextInfo)new UnifiedWrap.TextView(x, kind)).ToList(),
        PetTextKind.Click => mw.ClickTexts.Select(x => (ITextInfo)new UnifiedWrap.TextView(x, kind)).ToList(),
        PetTextKind.Select => mw.SelectTexts.Select(x => (ITextInfo)new UnifiedWrap.TextView(x, kind)).ToList(),
        _ => new List<ITextInfo>(),
    };

    public void AddText(PetTextKind kind, ILine line)
    {
        //反序列化失败会给回 null, 直接塞进去要等以后遍历时才炸, 那时已经查不出是谁塞的了
        switch (kind)
        {
            case PetTextKind.LowFood:
                Add(mw.LowFoodText, LinePutScript.Converter.LPSConvert.DeserializeObject<LowText>(line));
                break;
            case PetTextKind.LowDrink:
                Add(mw.LowDrinkText, LinePutScript.Converter.LPSConvert.DeserializeObject<LowText>(line));
                break;
            case PetTextKind.Click:
                Add(mw.ClickTexts, LinePutScript.Converter.LPSConvert.DeserializeObject<ClickText>(line));
                break;
            case PetTextKind.Select:
                Add(mw.SelectTexts, LinePutScript.Converter.LPSConvert.DeserializeObject<SelectText>(line));
                break;
        }

        void Add<T>(List<T> list, T? text) where T : class
        {
            if (text == null)
                Log($"这一行读不成 {typeof(T).Name}, 已跳过: {line}");
            else
                list.Add(text);
        }
    }

    public ITextInfo? GetClickText()
    {
        var text = mw.GetClickText();
        return text == null ? null : new UnifiedWrap.TextView(text, PetTextKind.Click);
    }

    public string? FindImagePath(string name, string? superior = null)
        => mw.ImageSources.FindSource(name) ?? (superior == null ? null : mw.ImageSources.FindSource(superior));
    public string? FindFilePath(string name) => mw.FileSources.FindSource(name);

    // ---- 物品与桌宠 ----

    public IItemRegistry Items => itemRegistry;
    public IPetView Pet => petView;

    // ---- 界面挂点 ----

    public void AddMenuButton(PetMenuType menu, string displayName, Action click)
        => RunOnUI(() => mw.Main.ToolBar.AddMenuButton((ToolBar.MenuType)(int)menu, displayName, click));

    /// <summary>
    /// 往一级菜单下的分组里加按钮
    /// </summary>
    /// Windows 版的 ToolBar 只有两级(一级菜单 + 直接子项), 分组要自己建一层
    /// MenuItem. 那几个一级菜单在 ToolBar.xaml 里是 x:FieldModifier="public", 可以直接拿.
    public void AddMenuButton(PetMenuType menu, string groupName, string displayName, Action click)
        => RunOnUI(() =>
        {
            var parent = menu switch
            {
                PetMenuType.Feed => mw.Main.ToolBar.MenuFeed,
                PetMenuType.Interact => mw.Main.ToolBar.MenuInteract,
                PetMenuType.DIY => mw.Main.ToolBar.MenuDIY,
                _ => mw.Main.ToolBar.MenuSetting,
            };
            Avalonia.Controls.MenuItem? group = null;
            foreach (var item in parent.Items)
            {
                if (item is Avalonia.Controls.MenuItem mi && (mi.Header as string) == groupName)
                {
                    group = mi;
                    break;
                }
            }
            if (group == null)
            {
                group = new Avalonia.Controls.MenuItem
                {
                    Header = groupName,
                };
                parent.Items.Add(group);
            }
            var leaf = new Avalonia.Controls.MenuItem
            {
                Header = displayName,
            };
            leaf.Click += delegate { click?.Invoke(); };
            group.Items.Add(leaf);
            if (menu == PetMenuType.DIY)
                mw.Main.ToolBar.LoadDIY();
        });

    public void RegisterClosable(IDisposable closable) => closables.Add(closable);

    /// <summary>
    /// 登记语音播放器
    /// </summary>
    /// Windows 版自己有 MediaElement, 而且消息栏的口型同步读的是它的 Clock,
    /// MOD 提供的播放器喂不进去. 所以这里只记下来供 MOD 之间互相发现, 宿主播放
    /// 仍然走原来那套 —— 跨平台侧才是真正需要它的地方.
    public void RegisterVoicePlayer(IVoicePlayer player)
    {
        DynamicResources["UnifiedVoicePlayer"] = player;
        RegisterClosable(player);
        //跨平台: 这里就是真正需要它的地方 —— 桌宠自己没有音频后端, 交给 Main 播放和口型同步
        mw.SetVoicePlayer(player);
    }

    // ---- 杂项 ----

    public void SetZoomLevel(double level) => RunOnUI(() => mw.SetZoomLevel(level));
    public void Close() => RunOnUI(() => mw.Close());
    public void Restart() => RunOnUI(() => mw.Restart());

    public event Action? NewDay
    {
        add { if (value != null) mw.Event_NewDay += new Action(value); }
        remove { if (value != null) mw.Event_NewDay -= new Action(value); }
    }
}
