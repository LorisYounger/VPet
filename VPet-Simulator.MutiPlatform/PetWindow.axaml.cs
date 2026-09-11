using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using LinePutScript.Localization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VPet_Simulator.Core;
using VPet_Simulator.Core.MutiPlatform;
using VPet_Simulator.Windows.Interface;
using VPet_Simulator.Core.MutiPlatform.Display;
using Shell = VPet_Simulator.Core.MutiPlatform.Display.Shell;
using VPet_Simulator.Core.MutiPlatform.Graph;
using VPet_Simulator.Unified.Services;
using LinePutScript;

namespace VPet_Simulator.MutiPlatform;

/// <summary>
/// 桌宠窗口
/// </summary>
/// 职责很窄: 建立一个无边框透明置顶窗口, 加载 MOD 目录里的宠物动画,
/// 把 PetMain 挂上去跑起来, 并处理拖动.
public partial class PetWindow : Window
{
    private GameCore? core;
    private PetMain? pet;
    private AvaloniaController? controller;
    private AppSettings settings = new AppSettings();

    /// <summary>
    /// 桌宠身体的基准尺寸, 缩放倍率乘在它上面
    /// </summary>
    private const double BodySize = 500;

    public PetWindow() : this(string.Empty)
    {
    }

    /// <summary>
    /// 开一只桌宠
    /// </summary>
    /// <param name="prefix">多开前缀, 空表示默认那只</param>
    public PetWindow(string prefix)
    {
        prefixSave = prefix;
        InitializeComponent();
        Opened += OnOpened;
        Closing += OnClosing;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        //对话框默认挂到主窗口上, 这样它们会跟着桌宠居中而不是飞到屏幕角落.
        //多开时只认第一只, 免得后开的那只把属主抢走
        Shell.DialogService.DefaultOwner ??= this;
        images.OnError = Log;
        if (!string.IsNullOrEmpty(prefixSave))
            Title = "VPet - " + MultiPetStore.DisplayName(prefixSave);
        Log($"窗口已打开. 桌宠={(string.IsNullOrEmpty(prefixSave) ? "默认" : MultiPetStore.DisplayName(prefixSave))} 安装目录={AppPaths.InstallDirectory} 数据目录={AppPaths.DataRoot} MOD目录={AppPaths.ModRoot}");
        settings = AppSettings.Load(prefixSave, out var settingWarning);
        if (settingWarning != null)
            Log($"设置: {settingWarning}");
        ApplyWindowSettings();
        controller = new AvaloniaController(this, settings);
        controller.MoveToDefaultPosition();

        ShowStatus("正在加载桌宠动画...");
        try
        {
            await LoadPetAsync();
        }
        catch (Exception ex)
        {
            ShowStatus($"加载失败:\n{ex.Message}");
        }
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        // 先让插件收尾再存档: 它们可能在 EndGame 里还要改数据
        RaiseUnifiedEndGame();
        SaveNow();
        SaveSettings();
        pet?.Dispose();
        autoSaveTimer?.Dispose();
        images.Dispose();
        core?.Graph?.Dispose();
    }

    /// <summary>
    /// 应用主题配色
    /// </summary>
    /// 找不到设置里指定的那套就退回第一套, 一套都没有就用 App.axaml 里的默认值 ——
    /// 主题缺了只该是不好看, 不该起不来
    private void ApplyTheme()
    {
        //字体先应用: 主题里没有字体这一项, 它是单独一个设置
        var app = Application.Current;
        if (app != null)
        {
            var used = Avalonia.Threading.Dispatcher.UIThread.Invoke(
                () => FontLoader.Apply(settings.Font, app.Resources));
            if (!used && !string.IsNullOrWhiteSpace(settings.Font))
                Log($"字体: 系统里没装「{settings.Font}」, 用默认字体");
        }
        if (resources.Themes.Count == 0)
            return;
        var theme = resources.Themes.Find(x => x.XName == settings.Theme) ?? resources.Themes[0];
        var count = Avalonia.Threading.Dispatcher.UIThread.Invoke(() => ThemeLoader.Apply(theme));
        var missing = ThemeLoader.MissingKeys(theme);
        Log($"主题: {theme.XName} 写入 {count} 个配色"
            + (missing.Count > 0 ? $", 缺 {missing.Count} 个键: {string.Join(",", missing)}" : ""));
        //主题自带的图片包覆盖在 MOD 图片之上, 与 Windows 版同序
        foreach (var image in theme.Images.Sources)
            resources.Images[image.Key] = image.Value;
    }

    /// <summary>
    /// 把设置应用到窗口上
    /// </summary>
    private void ApplyWindowSettings()
    {
        Width = BodySize * settings.ZoomLevel;
        Height = BodySize * settings.ZoomLevel;
        Topmost = settings.TopMost;
    }

    /// <summary>
    /// 保存设置
    /// </summary>
    private void SaveSettings()
    {
        try
        {
            settings.Save();
        }
        catch (Exception ex)
        {
            Log($"设置保存失败: {ex.Message}");
        }
    }

    /// <summary>
    /// MOD 提供的食物和图片
    /// </summary>
    private readonly ModResources resources = new ModResources();

    /// <summary>
    /// 已解码的图片
    /// </summary>
    /// 食物、物品、照片、主题共用一份缓存: 它们本来就可能指向同一个文件
    private readonly ImageCache images = new ImageCache();

    /// <summary>
    /// 往"投喂"菜单里塞食物
    /// </summary>
    /// Windows 版点"投喂"打开的是一整个商店窗口(带价格、收藏、背包、统计),
    /// 那套界面依赖 VPet-Simulator.Windows.Interface 一整层还没跨平台化的东西.
    /// 这里退而求其次: 按食物类型分组直接列在菜单里, 点了就买下并喂掉.
    private void LoadFeedMenu(ToolBar toolbar)
    {
        // 商店窗口是完整的那一套(搜索/排序/分类/背包/赊账), 菜单里那份是快捷方式
        toolbar.AddMenuButton(ToolBar.MenuType.Feed, LocalizeCore.Translate("打开商店"), OpenShop);

        // 分组顺序与 Windows 版"投喂"菜单里的顺序一致
        var groups = new (FoodItem.FoodType Type, string Name)[]
        {
            (FoodItem.FoodType.Meal, "正餐"),
            (FoodItem.FoodType.Snack, "零食"),
            (FoodItem.FoodType.Drink, "饮料"),
            (FoodItem.FoodType.Functional, "功能性"),
            (FoodItem.FoodType.Drug, "药品"),
            (FoodItem.FoodType.Gift, "礼品"),
            (FoodItem.FoodType.Food, "食物"),
        };
        foreach (var (type, name) in groups)
        {
            foreach (var food in resources.Foods.Where(x => x.Type == type)
                .OrderBy(x => x.Price))
            {
                var text = $"{food.NameTrans}  ${food.Price:f1}";
                toolbar.AddMenuButton(ToolBar.MenuType.Feed, LocalizeCore.Translate(name), text,
                    () => TakeFood(food));
            }
        }
    }
    private void LoadPanelMenu(ToolBar toolbar)
    {
        toolbar.AddMenuButton(ToolBar.MenuType.Interact, LocalizeCore.Translate("工作面板"), OpenWorkMenu);
        toolbar.AddMenuButton(ToolBar.MenuType.Interact, LocalizeCore.Translate("桌宠状态"), OpenCharacterPanel);
        toolbar.AddMenuButton(ToolBar.MenuType.Interact, LocalizeCore.Translate("照片图库"), OpenGallery);
        toolbar.AddMenuButton(ToolBar.MenuType.Interact, LocalizeCore.Translate("多人联机"), OpenMultiplayer);
        toolbar.AddMenuButton(ToolBar.MenuType.Setting, LocalizeCore.Translate("设置"), OpenSetting);
    }

    /// <summary>
    /// 已经开着的功能窗口
    /// </summary>
    /// 每种同时只开一个: 开两个商店会各自显示各自那份金钱, 看着像出了 bug
    private readonly Dictionary<Type, Window> openWindows = new Dictionary<Type, Window>();

    /// <summary>
    /// 打开一个功能窗口, 已经开着就把它拉到前面
    /// </summary>
    private void OpenWindow<T>(Func<T> create) where T : Window
    {
        if (openWindows.TryGetValue(typeof(T), out var exist))
        {
            exist.Activate();
            return;
        }
        var window = create();
        openWindows[typeof(T)] = window;
        window.Closed += (_, _) => openWindows.Remove(typeof(T));
        window.Show(this);
    }

    /// <summary>
    /// 打开商店
    /// </summary>
    private void OpenShop() => OpenWindow(() => new Windows.ShopWindow(this));

    /// <summary>
    /// 打开工作面板
    /// </summary>
    private void OpenWorkMenu() => OpenWindow(() => new Windows.WorkWindow(this));

    /// <summary>
    /// 打开角色面板
    /// </summary>
    private void OpenCharacterPanel() => OpenWindow(() => new Windows.CharacterWindow(this));

    /// <summary>
    /// 打开设置
    /// </summary>
    private void OpenSetting() => OpenWindow(() => new Windows.SettingWindow(this));

    /// <summary>
    /// 打开照片图库
    /// </summary>
    private void OpenGallery() => OpenWindow(() => new Windows.GalleryWindow(this));

    /// <summary>
    /// 打开多人联机
    /// </summary>
    private void OpenMultiplayer() => OpenWindow(() => new Windows.MultiplayerWindow(this));

    /// <summary>
    /// 买下并喂掉一份食物
    /// </summary>
    /// 与 Windows 版 winBetterBuy + MainWindow.TakeItem 的流程一致: 先扣钱, 再按
    /// 吃腻度打折结算数值, 记账, 最后播动画. 吃腻度和统计项名都走共享后端, 所以
    /// 存档在两个平台之间拷来拷去时这些数是接得上的.
    private void TakeFood(FoodItem food)
    {
        if (core?.Save is not IGameSave save || pet == null)
            return;
        if (save.Money < food.Price)
        {
            pet.SayRnd(LocalizeCore.Translate("金钱不足, 买不起{0}", food.NameTrans), true);
            return;
        }
        save.Money -= food.Price;
        pet.LabelDisplayShow(LocalizeCore.Translate("{0} -${1:f1}", food.NameTrans, food.Price));
        FeedNoCharge(food);
    }

    /// <summary>
    /// 把一份食物喂给桌宠, 不扣钱
    /// </summary>
    /// 对应 Windows 版 MainWindow.TakeItem: 结算数值、记吃腻度、记账、播动画.
    /// MOD 通过契约里的 Items.Take 走的也是这条路。
    internal void FeedNoCharge(FoodItem food)
    {
        if (core?.Save is not IGameSave save || pet == null)
            return;
        pet.LastInteractionTime = DateTime.Now;

        //吃腻度: 同一样东西连着吃会越吃越没用
        var now = DateTime.Now;
        var buytime = gameSavesData[FeedingRules.BuyTimeLineName];
        double boredom = FeedingRules.RemainingBoredom(buytime.GetDateTime(food.Name, now), now);
        save.EatFood(food, FeedingRules.Effectiveness(boredom, food.Type == FoodItem.FoodType.Gift));
        boredom += FeedingRules.AddedBoredom(food.Likability, food.Feeling);
        buytime.SetDateTime(food.Name, now.AddHours(boredom));

        RecordPurchase(food);
        HostRaiseFoodTaken(food);

        var image = LoadFoodImage(food);
        if (image == null)
            pet.DisplayToNomal();
        else
            pet.Display(food.GetGraph(), image, pet.DisplayToNomal);
    }

    /// <summary>
    /// 记一笔投喂的账
    /// </summary>
    /// 统计项名与 Windows 版逐字一致, 年度报告和成就才对得上号
    private void RecordPurchase(FoodItem food)
    {
        var statistics = gameSavesData.Statistics;
        if (statistics == null)
            return;
        statistics[(gint)FeedingRules.BuyTimesStat]++;
        statistics[(gint)FeedingRules.BuyCountStat(food.Name)]++;
        statistics[(gdbe)FeedingRules.TotalSpendStat] += food.Price;
        var spend = FeedingRules.SpendStat((FeedingRules.FoodKind)(int)food.Type);
        if (spend != null)
            statistics[(gdbe)spend] += food.Price;
        if (food.Type == FoodItem.FoodType.Drug)
            statistics[(gdbe)FeedingRules.DrugExpStat] += food.Exp;
        else if (food.Type == FoodItem.FoodType.Gift)
            statistics[(gdbe)FeedingRules.GiftLikeStat] += food.Likability;
    }

    /// <summary>
    /// 取得食物图片, 取不到返回 null
    /// </summary>
    private Avalonia.Media.Imaging.Bitmap? LoadFoodImage(FoodItem food) => images.Get(food.ImagePath);

    /// <summary>
    /// 往"系统"菜单里塞设置项
    /// </summary>
    /// 跨平台版暂时不做独立的设置窗口: 需要调的项就这么几个, 直接做成菜单里的
    /// 勾选项比开一个窗口更顺手, 也少一整套窗口样式要维护.
    private void LoadSettingMenu(ToolBar toolbar)
    {
        foreach (var level in new[] { 0.5, 0.75, 1.0, 1.5 })
        {
            var text = LocalizeCore.Translate("缩放") + $" {level:p0}";
            toolbar.AddMenuButton(ToolBar.MenuType.Setting, text, () => SetZoomLevel(level));
        }
        toolbar.AddMenuButton(ToolBar.MenuType.Setting, LocalizeCore.Translate("置顶"), () =>
        {
            settings.TopMost = !settings.TopMost;
            Topmost = settings.TopMost;
            pet?.LabelDisplayShow(LocalizeCore.Translate(settings.TopMost ? "已置顶" : "已取消置顶"));
            SaveSettings();
        });
        toolbar.AddMenuButton(ToolBar.MenuType.Setting, LocalizeCore.Translate("允许移动"), () =>
        {
            settings.AllowMove = !settings.AllowMove;
            ApplyMoveMode();
            pet?.LabelDisplayShow(LocalizeCore.Translate(settings.AllowMove ? "已允许移动" : "已禁止移动"));
            SaveSettings();
        });
        toolbar.AddMenuButton(ToolBar.MenuType.Setting, LocalizeCore.Translate("数值计算"), () =>
        {
            settings.EnableFunction = !settings.EnableFunction;
            pet?.LabelDisplayShow(LocalizeCore.Translate(settings.EnableFunction ? "已开启数值计算" : "已关闭数值计算"));
            SaveSettings();
        });
        toolbar.AddMenuButton(ToolBar.MenuType.Setting, LocalizeCore.Translate("退出"), Close);
    }

    /// <summary>
    /// 调整缩放
    /// </summary>
    /// 只改窗口尺寸, 不重新解码动画: 动画是按启动时的倍率解码的, 放大之后会略糊,
    /// 下次启动才会按新倍率重新解码. 为了改个缩放就把六百多个动画全部重载不划算.
    private void SetZoomLevel(double level)
    {
        settings.ZoomLevel = level;
        Width = BodySize * level;
        Height = BodySize * level;
        controller?.MoveToDefaultPosition();
        SaveSettings();
    }

    /// <summary>
    /// 把移动相关设置应用到桌宠上
    /// </summary>
    private void ApplyMoveMode()
    {
        // 智能移动周期在设置里是秒, SetMoveMode 要的是毫秒
        pet?.SetMoveMode(settings.AllowMove, settings.SmartMove, settings.SmartMoveInterval * 1000);
    }

    /// <summary>
    /// 自动存档计时器
    /// </summary>
    /// 桌宠是长时间挂着的程序, 只在退出时存档的话一旦异常退出就白玩了
    private System.Timers.Timer? autoSaveTimer;

    /// <summary>
    /// 完整的存档容器
    /// </summary>
    /// 与 Windows 版是同一个类型: 除了桌宠数值, 里面还有统计、背包、图库解锁、
    /// 日程表、吃腻度这些放在 Data 里的行. 必须整个读进来再整个写回去, 只挑
    /// vpet 行读写会把其余的全丢掉.
    private GameSave_v2 gameSavesData = new GameSave_v2("");

    /// <summary>
    /// 多开前缀
    /// </summary>
    /// 与 Windows 版 PrefixSave 同义: 决定读写哪一套存档和设置
    private string prefixSave = string.Empty;

    /// <summary>
    /// 立刻存档
    /// </summary>
    private void SaveNow()
    {
        if (core?.Save == null)
            return;
        try
        {
            RaiseUnifiedSave();
            HostItems.Save(gameSavesData.Data);
            SaveStore.Save(gameSavesData, settings, prefixSave);
            // 编号是记在设置里的, 存档写完必须把设置也存了, 否则下次启动编号倒退,
            // 新存档会把这一份覆盖掉
            SaveSettings();
        }
        catch (Exception ex)
        {
            Log($"存档失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 加载宠物动画并启动桌宠
    /// </summary>
    private async Task LoadPetAsync()
    {
        // 用户数据目录优先于安装目录: 安装目录在 Linux(/usr) 和 macOS(.app 包内)
        // 通常是只读的, 只能放随程序分发的内容, 用户自己装的 MOD 在数据目录
        var roots = new[] { AppPaths.ModRoot, Path.Combine(AppPaths.InstallDirectory, "mod") };
        var pets = new List<PetLoader>();

        // 扫描 MOD 目录会读大量小文件, 放后台线程
        var mods = await Task.Run(() => ModLoader.LoadAll(roots, pets, resources));
        Log($"扫描到 {mods.Count} 个 MOD, 宠物 {pets.Count} 只,"
            + $" 食物 {resources.Foods.Count} 种, 图片 {resources.Images.Count} 张");
        foreach (var mod in mods)
        {
            if (mod.Tag.Count > 0 || mod.Warnings.Count > 0)
                Log($"  MOD [{mod.Name}] 内容={string.Join("/", mod.Tag)}"
                    + (mod.Warnings.Count > 0 ? " 问题=" + string.Join("; ", mod.Warnings) : ""));
        }

        HostMods.Clear();
        HostMods.AddRange(mods);
        ApplyTheme();
        // 插件要在读档之前实例化: 它们在构造函数里注册的物品创建器正好赶得上
        // 存档里的自定义物品
        LoadUnifiedPlugins();

        // 加载语言, 与 Windows 版 MainWindow.xaml.cs 同序: 各 MOD 先 AddCulture, 全部扫完再 LoadCulture.
        // 之前只做了前一半, 语言包 MOD 读进来了却从没生效.
        LocalizeCore.StoreTranslation = true;
        if (settings.Language == "null")
        {
            LocalizeCore.LoadDefaultCulture();
            if (LocalizeCore.CurrentCulture == "null")
                LocalizeCore.CurrentCulture = "en";
            settings.Language = LocalizeCore.CurrentCulture;
        }
        else
            LocalizeCore.LoadCulture(settings.Language);
        Log($"语言={LocalizeCore.CurrentCulture} 可用={string.Join(",", LocalizeCore.AvailableCultures)}");

        if (pets.Count == 0)
        {
            ShowStatus($"没有找到宠物动画.\n请把 mod 目录放到:\n{AppPaths.ModRoot}");
            return;
        }

        var loader = pets[0];
        //去除其他语言内容
        var tag = loader.Config.Data.GetString("tag", "all")!.Split(',');
        resources.LowDrinkTexts.RemoveAll(x => !x.FindTag(tag));
        resources.LowFoodTexts.RemoveAll(x => !x.FindTag(tag));
        resources.ClickTexts.RemoveAll(x => !x.FindTag(tag));
        resources.SelectTexts.RemoveAll(x => !x.FindTag(tag));
        //加载数据合理化: 说话文本. 阈值与 Windows 版 MainWindow 逐项一致,
        //之前跨平台这边漏了这一段, 超模的 MOD 文本一句话就能把数值顶满
        if (!settings["gameconfig"].GetBool("noAutoCal"))
        {
            foreach (var selet in resources.SelectTexts)
            {
                selet.Exp = Math.Max(Math.Min(selet.Exp, 1000), -1000);
                selet.Feeling = Math.Max(Math.Min(selet.Feeling, 100), -100);
                selet.Health = Math.Max(Math.Min(selet.Health, 100), -100);
                selet.Likability = Math.Max(Math.Min(selet.Likability, 50), -50);
                selet.Money = Math.Max(Math.Min(selet.Money, 1000), -1000);
                selet.Strength = Math.Max(Math.Min(selet.Strength, 1000), -1000);
                selet.StrengthDrink = Math.Max(Math.Min(selet.StrengthDrink, 1000), -1000);
                selet.StrengthFood = Math.Max(Math.Min(selet.StrengthFood, 1000), -1000);
            }
            foreach (var selet in resources.ClickTexts)
            {
                selet.Exp = Math.Max(Math.Min(selet.Exp, 1000), -1000);
                selet.Feeling = Math.Max(Math.Min(selet.Feeling, 1000), -1000);
                selet.Health = Math.Max(Math.Min(selet.Health, 100), -100);
                selet.Likability = Math.Max(Math.Min(selet.Likability, 50), -50);
                selet.Money = Math.Max(Math.Min(selet.Money, 1000), -1000);
                selet.Strength = Math.Max(Math.Min(selet.Strength, 1000), -1000);
                selet.StrengthDrink = Math.Max(Math.Min(selet.StrengthDrink, 1000), -1000);
                selet.StrengthFood = Math.Max(Math.Min(selet.StrengthFood, 1000), -1000);
            }
        }
        var started = DateTime.Now;
        var graph = await Task.Run(() => loader.Graph((int)(BodySize * settings.ZoomLevel)));
        Log($"动画扫描完成, 耗时={(DateTime.Now - started).TotalSeconds:0.0}秒 个数={loader.GraphCount}");

        // 有存档就接着上次的状态玩, 没有才新建
        var loaded = SaveStore.LoadLatest(settings, prefixSave, out var saveWarnings);
        foreach (var warning in saveWarnings)
            Log($"存档: {warning}");
        if (loaded == null)
        {
            gameSavesData = new GameSave_v2(loader.PetName);
            // 与 Windows 版一致: 新存档记下主人名和第一次启动的日子
            gameSavesData.GameSave.HostName = Environment.UserName;
            gameSavesData.Data.SetDateTime("birthday", DateTime.Now);
            Log("存档: 未找到, 新建一份");
        }
        else
        {
            gameSavesData = loaded;
            Log($"存档: 已读取 {gameSavesData.GameSave.Name} 等级={gameSavesData.GameSave.Level}"
                + $" 金钱={gameSavesData.GameSave.Money:0} 防作弊={gameSavesData.HashCheck}"
                + $" 其它数据 {gameSavesData.Data.Count} 行");
        }
        var save = gameSavesData.GameSave;
        // 背包: 认不出主人的物品行会被挂起, 等对应的 MOD 装回来再补出来
        HostItems.Load(gameSavesData.Data);
        resources.Photos.OnError = Log;
        resources.Photos.LoadUnlocked(gameSavesData.Data);
        if (HostItems.PendingCount > 0)
            Log($"背包: {HostItems.PendingCount} 件物品来自没装的 MOD, 已原样留在存档里");

        core = new GameCore
        {
            Graph = graph,
            Save = save,
            Controller = controller,
        };

        pet = new PetMain(core);
        // 插件可能在桌宠建好之前就登记了语音播放器
        AttachVoicePlayer();
        // 触摸区域是从宠物配置里读的, 必须在 Core.Graph 就绪之后注册
        pet.Load_2_TouchEvent();
        // 工作列表同理, 也是从宠物配置里读的
        pet.ToolBar.LoadWork();
        // "系统"菜单的内容由宿主决定: Core.MutiPlatform 是个类库, 不该知道
        // 怎么关闭宿主窗口, 也不该管设置存在哪
        LoadTalk(pet);
        LoadTalkSelect(pet);
        LoadFeedMenu(pet.ToolBar);
        LoadPanelMenu(pet.ToolBar);
        LoadSettingMenu(pet.ToolBar);
        ApplyMoveMode();
        PetHost.Content = pet;
        // 存档读好、控件挂好, 插件可以挂事件和加菜单了
        RaiseUnifiedLoadPlugin();
        LoadUnifiedSettingMenu(pet.ToolBar);
        HideStatus();

        // 等动画真正就绪再开始播放, 否则第一帧会是空的.
        // 交给 PetMain.Load_2_WaitGraph: 它会把加载失败的动画从 GraphsList 里摘掉
        // 并把原因记进 ErrorMessage, 而不是像之前那样干等到超时
        await pet.Load_2_WaitGraph(waited => ShowStatus($"正在加载桌宠动画... {waited}"));

        Log($"宠物={loader.PetName} 动画总数={graph.GraphsALL.Count}"
            + $" 就绪={graph.GraphsALL.Count(x => x.IsReady)} 失败={pet.ErrorMessage.Count}");
        foreach (var message in pet.ErrorMessage.Take(3))
            Log($"加载失败: {message}");
        if (graph.GraphsALL.Count == 0)
        {
            ShowStatus($"宠物动画为空:\n{loader.Name}");
            return;
        }
        if (pet.ErrorMessage.Count == graph.GraphsALL.Count)
        {
            ShowStatus($"全部动画加载失败, 例如:\n{pet.ErrorMessage[0]}");
            return;
        }
        HideStatus();

        pet.Start();
        Log($"桌宠已启动, 当前动画={pet.DisplayType}");
        RaiseUnifiedGameLoaded();
        // 逻辑心跳里顺带判一下有没有跨天: 桌宠常常挂一整夜
        pet.TimeHandle += _ => HostCheckNewDay();
        HostCheckNewDay();

        // 每五分钟自动存一次
        autoSaveTimer = new System.Timers.Timer(settings.AutoSaveInterval * 60 * 1000) { AutoReset = true };
        autoSaveTimer.Elapsed += (_, _) => SaveNow();
        autoSaveTimer.Start();
    }

    /// <summary>
    /// 写入运行日志
    /// </summary>
    /// 桌宠是常驻后台的程序, 出问题时用户看不到任何控制台输出, 必须有日志可查.
    /// 与 Windows 版的 Logs*.txt 作用一致.
    internal static void Log(string message)
    {
        try
        {
            var path = Path.Combine(AppPaths.EnsureDirectory(AppPaths.DataRoot), "vpet.log");
            File.AppendAllText(path, $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        }
        catch
        {
            // 日志写不了也不能影响桌宠运行
        }
    }

    private void ShowStatus(string text) => Dispatcher.UIThread.Post(() =>
    {
        StatusText.Text = text;
        StatusText.IsVisible = true;
    });

    private void HideStatus() => Dispatcher.UIThread.Post(() => StatusText.IsVisible = false);
}
