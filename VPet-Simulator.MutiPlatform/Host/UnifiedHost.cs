using Avalonia.Threading;
using LinePutScript;
using LinePutScript.Localization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VPet_Simulator.Core.MutiPlatform;
using VPet_Simulator.Unified.Interface;
using VPet_Simulator.Unified.Services;
using VPet_Simulator.Windows.Interface;

namespace VPet_Simulator.MutiPlatform;

/// <summary>
/// 统一 MOD 契约在跨平台侧的宿主实现
/// </summary>
/// 与 Windows 侧的 UnifiedPluginHost 是同一份契约的两个实现. 同一个插件 dll 在两
/// 边看到的门面是一样的, 所以它编译一次两边都能跑。
///
/// 与 Windows 侧的差别集中在两点:
///   - 这边没有"真对象 + 包装器"两层. 背包直接存契约里那份数据, 少一层就少一处
///     会走样的地方
///   - 有些面这边还没有东西可给(照片墙、日程表), 明确返回空而不是抛异常 ——
///     MOD 拿到空表会自己跳过, 拿到异常则会整个崩掉
internal sealed class UnifiedHost : IPetHost
{
    private readonly PetWindow window;
    private readonly ModLoader mod;
    private readonly List<IDisposable> closables = new List<IDisposable>();
    private readonly UnifiedItemRegistry itemRegistry;
    private readonly UnifiedPetView petView;
    private readonly HostSaveView saveView;
    private readonly HostStatisticsView statisticsView;

    /// <summary>
    /// 插件本体
    /// </summary>
    public UnifiedPlugin Plugin { get; private set; } = null!;

    /// <summary>
    /// LoadPlugin 是否已经跑过
    /// </summary>
    internal bool Loaded { get; private set; }

    private UnifiedHost(PetWindow window, ModLoader mod)
    {
        this.window = window;
        this.mod = mod;
        itemRegistry = new UnifiedItemRegistry(window);
        petView = new UnifiedPetView(window);
        saveView = new HostSaveView(() => window.HostGameSave.GameSave);
        statisticsView = new HostStatisticsView(() => window.HostGameSave.Statistics!);
    }

    /// <summary>
    /// 建立宿主并实例化插件
    /// </summary>
    /// 调用时机在读档之前 —— 插件构造函数里注册的物品创建器正好赶得上
    internal static UnifiedHost? Create(PetWindow window, ModLoader mod, Type pluginType, List<string> warnings)
    {
        var host = new UnifiedHost(window, mod);
        try
        {
            if (Activator.CreateInstance(pluginType, host) is not UnifiedPlugin plugin)
            {
                warnings.Add($"{pluginType.FullName} 不是统一契约插件");
                return null;
            }
            host.Plugin = plugin;
            return host;
        }
        catch (Exception ex)
        {
            warnings.Add($"{pluginType.FullName} 构造失败: {ex.Message}");
            return null;
        }
    }

    // ---- 生命周期 ----

    internal void OnLoadPlugin()
    {
        Loaded = true;
        petView.Attach();
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

    public string HostName => "MutiPlatform";
    public int HostVersion => PetWindow.HostVersion;
    public bool IsUIThread => Dispatcher.UIThread.CheckAccess();
    public void RunOnUI(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
            action();
        else
            Dispatcher.UIThread.Invoke(action);
    }
    public T RunOnUI<T>(Func<T> func)
        => Dispatcher.UIThread.CheckAccess() ? func() : Dispatcher.UIThread.Invoke(func);
    public string Translate(string text) => LocalizeCore.Translate(text);
    public string Translate(string text, params object[] args) => LocalizeCore.Translate(text, args);
    public void Log(string message) => PetWindow.Log($"[{Plugin?.PluginName ?? mod.Name}] {message}");
    public void ShowMessage(string text, string title = "")
        => window.ShowHostMessage(string.IsNullOrEmpty(title) ? mod.Name : title, text);
    public void ShowInputBox(string title, string text, string defaultText, Action<string> end, bool allowMultiLine = false)
        => window.ShowHostInputBox(title, text, defaultText, end, allowMultiLine);

    /// <summary>
    /// 取宿主专属能力
    /// </summary>
    /// 用了它这个 MOD 就只能在跨平台版上跑了 —— 有意留的逃生舱, 不是推荐用法
    public T? GetService<T>() where T : class
        //Steam 能力是跨平台的(契约里的 ISteamServices), 放在这里 MOD 不用管平台
        => new object?[] { window.HostSteam, window, window.HostPet, window.HostCore,
            window.HostSettings, window.HostGameSave }
            .OfType<T>().FirstOrDefault();

    // ---- 插件与 MOD ----

    public IPluginInfo Info => new HostPluginInfo(mod, IsModEnabled);
    public IEnumerable<IPluginInfo> Mods => window.HostMods.Select(x => (IPluginInfo)new HostPluginInfo(x, IsModEnabled));
    public IEnumerable<IPluginInfo> OnMods => Mods.Where(x => x.IsEnabled);
    public IReadOnlyList<string> ModPaths => AppPaths.ModRoots.ToList();
    public string DataDirectory => AppPaths.DataDirectory;
    public string GetModStorage(string modName) => AppPaths.GetModStorage(modName);
    public string PrefixSave => window.HostPrefixSave;

    private bool IsModEnabled(string name) => ModSwitchStore.IsOn(window.HostSettings, name);

    // ---- 设置与存档 ----

    public ILine Setting(string lineName) => window.HostSettings.FindorAddLine(lineName);
    public ILPS SaveData => window.HostGameSave.Data;
    public IPetSave Save => saveView;
    public IPetStatistics Statistics => statisticsView;
    public bool HashCheck => window.HostGameSave.HashCheck;
    public void HashCheckOff() => window.HostGameSave.HashCheckOff();
    public void SaveGame() => window.SaveNowFromHost();
    public IDictionary<string, object> DynamicResources => window.HostDynamicResources;

    // ---- 数据表 ----

    public IReadOnlyList<IFoodInfo> Foods => window.HostResources.Foods.Cast<IFoodInfo>().ToList();

    public IReadOnlyList<IPhotoInfo> Photos
        => window.HostPhotos.Photos.Select(x => (IPhotoInfo)new HostPhotoView(window, x)).ToList();

    public IReadOnlyList<ITextInfo> Texts(PetTextKind kind) => kind switch
    {
        PetTextKind.LowFood => window.HostResources.LowFoodTexts.Select(x => (ITextInfo)new HostTextView(x, kind)).ToList(),
        PetTextKind.LowDrink => window.HostResources.LowDrinkTexts.Select(x => (ITextInfo)new HostTextView(x, kind)).ToList(),
        PetTextKind.Click => window.HostResources.ClickTexts.Select(x => (ITextInfo)new HostTextView(x, kind)).ToList(),
        PetTextKind.Select => window.HostResources.SelectTexts.Select(x => (ITextInfo)new HostTextView(x, kind)).ToList(),
        _ => Array.Empty<ITextInfo>(),
    };

    public void AddText(PetTextKind kind, ILine line)
    {
        switch (kind)
        {
            case PetTextKind.LowFood:
                Add(window.HostResources.LowFoodTexts, LinePutScript.Converter.LPSConvert.DeserializeObject<LowText>(line));
                break;
            case PetTextKind.LowDrink:
                Add(window.HostResources.LowDrinkTexts, LinePutScript.Converter.LPSConvert.DeserializeObject<LowText>(line));
                break;
            case PetTextKind.Click:
                Add(window.HostResources.ClickTexts, LinePutScript.Converter.LPSConvert.DeserializeObject<ClickText>(line));
                break;
            case PetTextKind.Select:
                Add(window.HostResources.SelectTexts, LinePutScript.Converter.LPSConvert.DeserializeObject<SelectText>(line));
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
        var text = window.HostPickClickText();
        return text == null ? null : new HostTextView(text, PetTextKind.Click);
    }

    public string? FindImagePath(string name, string? superior = null)
        => window.HostResources.FindImagePath(name, superior);

    public string? FindFilePath(string name) => window.HostResources.FindFilePath(name);

    // ---- 物品与桌宠 ----

    public IItemRegistry Items => itemRegistry;
    public IPetView Pet => petView;

    // ---- 界面挂点 ----

    public void AddMenuButton(PetMenuType menu, string displayName, Action click)
        => RunOnUI(() => window.HostAddMenuButton(menu, null, displayName, click));

    public void AddMenuButton(PetMenuType menu, string groupName, string displayName, Action click)
        => RunOnUI(() => window.HostAddMenuButton(menu, groupName, displayName, click));

    public void RegisterClosable(IDisposable closable) => closables.Add(closable);

    /// <summary>
    /// 登记语音播放器
    /// </summary>
    /// 跨平台侧真的会拿它来放音: Avalonia 没有内置音频后端, 语音只能由 MOD 提供
    public void RegisterVoicePlayer(IVoicePlayer player)
    {
        DynamicResources["UnifiedVoicePlayer"] = player;
        window.HostSetVoicePlayer(player);
        RegisterClosable(player);
    }

    // ---- 杂项 ----

    public void SetZoomLevel(double level) => RunOnUI(() => window.HostSetZoomLevel(level));
    public void Close() => RunOnUI(window.Close);
    public void Restart() => RunOnUI(window.HostRestart);

    public event Action? NewDay
    {
        add { if (value != null) window.HostNewDay += value; }
        remove { if (value != null) window.HostNewDay -= value; }
    }
}

/// <summary>
/// MOD 元信息的包装
/// </summary>
internal sealed class HostPluginInfo : IPluginInfo
{
    private readonly ModLoader mod;
    private readonly Func<string, bool> isEnabled;

    internal HostPluginInfo(ModLoader mod, Func<string, bool> isEnabled)
    {
        this.mod = mod;
        this.isEnabled = isEnabled;
    }

    public string Name => mod.Name;
    public string Author => mod.Author;
    public long AuthorID => mod.AuthorID;
    public ulong ItemID => mod.ItemID;
    public string Intro => mod.Intro;
    public int GameVer => mod.GameVer;
    public int Ver => mod.Ver;
    public string Path => mod.Path.FullName;
    public IReadOnlyCollection<string> Tag => mod.Tag.ToList();
    public bool IsEnabled => isEnabled(mod.Name);
}

/// <summary>
/// 照片的包装
/// </summary>
/// 跨平台的 Photo 与 Windows 的是同一份共享源码, 但解锁要经过宿主(要写存档),
/// 所以这一层还是要的
internal sealed class HostPhotoView : IPhotoInfo
{
    private readonly PetWindow window;
    private readonly Photo target;

    internal HostPhotoView(PetWindow window, Photo target)
    {
        this.window = window;
        this.target = target;
    }

    public string Name => target.Name;
    public string TranslateName => target.TranslateName;
    public string Description => target.Description;
    public string Tags => string.Join(",", target.Tags);
    public PetPhotoType Type => (PetPhotoType)(int)target.Type;
    public bool IsUnlock => target.IsUnlock;
    public bool IsStar { get => target.IsStar; set => target.IsStar = value; }
    public void Unlock() => window.HostPhotos.Unlock(window.HostGameSave.Data, target);
}

/// <summary>
/// 说话文本的包装
/// </summary>
internal sealed class HostTextView : ITextInfo
{
    private readonly PetText target;

    internal HostTextView(PetText target, PetTextKind kind)
    {
        this.target = target;
        Kind = kind;
    }

    public PetTextKind Kind { get; }
    public string Text => target.Text;
    public string TranslateText => target.TranslateText;
    public string Tag => target.Tag;
    public ILine ToLine()
        => LinePutScript.Converter.LPSConvert.SerializeObjectToLine<Line>(target, Kind.ToString().ToLowerInvariant() + "text");
}
