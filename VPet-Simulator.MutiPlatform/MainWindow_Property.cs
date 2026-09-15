using Avalonia.Controls;
using Avalonia.Threading;
using LinePutScript;
using LinePutScript.Dictionary;
using LinePutScript.Localization;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using VPet_Simulator.Core;
using VPet_Simulator.Core.MutiPlatform;
using VPet_Simulator.Core.MutiPlatform.Display;
using VPet_Simulator.Windows.Interface;

namespace VPet_Simulator.MutiPlatform;

/// <summary>
/// 桌宠主窗口: 属性
/// </summary>
/// 对应 Windows 版 VPet-Simulator.Windows/MainWindow_Property.cs, 成员逐条相同; 末尾多出的是
/// IMainWindow 里那几个 Windows 版写在 MainWindow.cs 里的成员, 集中放在这里好对照.
public partial class MainWindow : IMainWindow
{//主窗口部分数据
    /// <summary>
    /// 版本号
    /// </summary>
    public int version { get; } = 12100;
    /// <summary>
    /// 版本号
    /// </summary>
    public string Version => $"{version / 10000}.{version % 10000 / 100}.{version % 100:00}";
    /// <summary>
    /// Steam用户ID
    /// </summary>
    public ulong SteamID => IsSteamUser ? SteamClient.SteamId.Value : 0;
    /// <summary>
    /// Steam作者ID
    /// </summary>
    public uint SteamAuthorID => IsSteamUser ? SteamClient.SteamId.AccountId : 0;
    /// <summary>
    /// 低食物文本
    /// </summary>
    public List<LowText> LowFoodText { get; set; } = new List<LowText>();
    /// <summary>
    /// 低水文本
    /// </summary>
    public List<LowText> LowDrinkText { get; set; } = new List<LowText>();
    /// <summary>
    /// 选择文本
    /// </summary>
    public List<SelectText> SelectTexts { get; set; } = new List<SelectText>();
    /// <summary>
    /// 点击文本
    /// </summary>
    public List<ClickText> ClickTexts { get; set; } = new List<ClickText>();
    /// <summary>
    /// 食物
    /// </summary>
    public List<Food> Foods { get; } = new List<Food>();
    /// <summary>
    /// 照片
    /// </summary>
    public List<Photo> Photos { get; } = new List<Photo>();
    /// <summary>
    /// 游戏存档
    /// </summary>
    public GameSave_v2 GameSavesData { get; set; } = new GameSave_v2("VPET");
    /// <summary>
    /// MOD目录
    /// </summary>
    /// 本地 MOD 统一放在运行目录下的 mod 文件夹。
    public static readonly string ModPath = AppPaths.ModRoot;
    /// <summary>
    /// 是否为Steam用户
    /// </summary>
    public bool IsSteamUser { get; }
    /// <summary>
    /// 启动参数
    /// </summary>
    public LPS_D Args { get; }
    /// <summary>
    /// 多开前缀
    /// </summary>
    public string PrefixSave { get; } = "";
    private string? prefixsavetrans = null;
    /// <summary>
    /// 多开前缀 (翻译)
    /// </summary>
    public string PrefixSaveTrans
    {
        get
        {
            if (prefixsavetrans == null)
            {
                if (PrefixSave == "")
                    prefixsavetrans = "";
                else
                    prefixsavetrans = '-' + PrefixSave.TrimStart('-').Translate();
            }
            return prefixsavetrans;
        }
    }
    /// <summary>
    /// 游戏设置
    /// </summary>
    internal Setting Set { get; set; } = null!;
    ISetting IMainWindow.Set => Set;
    /// <summary>
    /// 宠物加载器
    /// </summary>
    public List<PetLoader> Pets { get; set; } = new List<PetLoader>();
    /// <summary>
    /// 已加载的 MOD
    /// </summary>
    internal List<CoreMOD> CoreMODs = new List<CoreMOD>();
    /// <summary>
    /// 游戏核心
    /// </summary>
    public GameCore Core { get; set; } = new GameCore();
    /// <summary>
    /// 开着的子窗口
    /// </summary>
    public List<Window> Windows { get; set; } = new List<Window>();
    /// <summary>
    /// 桌宠主体
    /// </summary>
    public Main Main { get; set; } = null!;
    /// <summary>
    /// 聊天框
    /// </summary>
    public Control? TalkBox;
    public winBetterBuy? winBetterBuy { get; set; }
    public winInventory? winInventory { get; set; }

    public winWorkMenu? winWorkMenu { get; set; }
    public winGameSetting? winSetting { get; set; }
    public winGallery? winGallery { get; set; }
    internal winMutiPlayer? winMutiPlayer;
    public PetHelper? petHelper;
    internal MWController MWController { get; set; } = null!;
    /// <summary>
    /// 图片资源
    /// </summary>
    public ImageResources ImageSources { get; set; } = new ImageResources();
    /// <summary>
    /// 文件资源
    /// </summary>
    public Resources FileSources { get; set; } = new Resources();
    /// <summary>
    /// 跨 MOD 共享的资源
    /// </summary>
    public Dictionary<string, object> DynamicResources { get; set; } = new Dictionary<string, object>();
    /// <summary>
    /// 统一契约插件的宿主
    /// </summary>
    internal List<UnifiedPluginHost> UnifiedHosts { get; } = new List<UnifiedPluginHost>();
    /// <summary>
    /// 统一契约插件
    /// </summary>
    public IEnumerable<VPet_Simulator.Unified.Interface.UnifiedPlugin> UnifiedPlugins
        => UnifiedHosts.Select(x => x.Plugin);
    /// <summary>
    /// 字体
    /// </summary>
    public List<IFont> Fonts { get; } = new List<IFont>();
    /// <summary>
    /// 主题
    /// </summary>
    public List<Theme> Themes = new List<Theme>();
    /// <summary>
    /// 当前主题
    /// </summary>
    public Theme? Theme = null;
    /// <summary>
    /// 日程表
    /// </summary>
    public ScheduleTask ScheduleTask { get; set; } = null!;
    /// <summary>
    /// 背包
    /// </summary>
    public List<Item> Items { get; set; } = new List<Item>();
    /// <summary>
    /// 日程表套餐
    /// </summary>
    public List<ScheduleTask.PackageFull> SchedulePackage { get; set; } = new List<ScheduleTask.PackageFull>();
    /// <summary>
    /// 活动日志
    /// </summary>
    public ObservableCollection<ActivityLog> ActivityLogs { get; set; } = new ObservableCollection<ActivityLog>();

    // ---- 以下是 IMainWindow 里 Windows 版写在 MainWindow.cs 里的成员 ----

    /// <summary>
    /// 调度器
    /// </summary>
    public Dispatcher Dispatcher => Dispatcher.UIThread;
    /// <summary>
    /// 主窗口的承载网格 (x:Name 生成的字段, 接口要的是属性)
    /// </summary>
    Grid IMainWindow.MGHost => MGHost;
    /// <summary>
    /// 桌宠所在的网格
    /// </summary>
    Grid IMainWindow.PetGrid => MGrid;
    /// <summary>
    /// 上次点击时间
    /// </summary>
    public long lastclicktime { get; set; }
    private Image? hashcheckimg;
    /// <summary>
    /// 存档 Hash检查 是否通过
    /// </summary>
    /// 对应 Windows 版 MainWindow.cs 的同名属性: 没作弊的玩家在面板上挂一个按游戏时长分级的徽章
    public bool HashCheck
    {
        get => GameSavesData.HashCheck;
        set
        {
            if (!value)
            {
                GameSavesData.HashCheckOff();
            }
            Main?.Dispatcher.Invoke(() =>
            {
                if (GameSavesData.HashCheck)
                {
                    if (hashcheckimg == null)
                    {
                        hashcheckimg = new Image();
                        int hours = GameSavesData.Statistics![(gint)"stat_total_time"] / 3600;

                        if (hours < 10)
                            hashcheckimg.Source = ImageResources.NewSafeBitmapImage("avares://VPet-Simulator.MutiPlatform/Res/hash.png");
                        else if (hours < 50)
                            hashcheckimg.Source = ImageResources.NewSafeBitmapImage("avares://VPet-Simulator.MutiPlatform/Res/hash0.png");
                        else if (hours < 100)
                            hashcheckimg.Source = ImageResources.NewSafeBitmapImage("avares://VPet-Simulator.MutiPlatform/Res/hash1.png");
                        else if (hours < 200)
                            hashcheckimg.Source = ImageResources.NewSafeBitmapImage("avares://VPet-Simulator.MutiPlatform/Res/hash2.png");
                        else if (hours < 500)
                            hashcheckimg.Source = ImageResources.NewSafeBitmapImage("avares://VPet-Simulator.MutiPlatform/Res/hash3.png");
                        else if (hours < 1000)
                            hashcheckimg.Source = ImageResources.NewSafeBitmapImage("avares://VPet-Simulator.MutiPlatform/Res/hash4.png");
                        else if (hours < 2000)
                            hashcheckimg.Source = ImageResources.NewSafeBitmapImage("avares://VPet-Simulator.MutiPlatform/Res/hash5.png");
                        else if (hours < 5000)
                            hashcheckimg.Source = ImageResources.NewSafeBitmapImage("avares://VPet-Simulator.MutiPlatform/Res/hash6.png");
                        else if (hours < 10000)
                            hashcheckimg.Source = ImageResources.NewSafeBitmapImage("avares://VPet-Simulator.MutiPlatform/Res/hash7.png");
                        else
                            hashcheckimg.Source = ImageResources.NewSafeBitmapImage("avares://VPet-Simulator.MutiPlatform/Res/hash8.png");

                        hashcheckimg.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right;
                        ToolTip.SetTip(hashcheckimg, "是没有修改过存档/使用超模MOD的玩家专属标志".Translate() + ' ' + ((int)(Math.Sqrt(hours))).ToString("X"));
                        hashcheckimg.Width = 64;
                        hashcheckimg.Height = 64;
                        Grid.SetColumn(hashcheckimg, 4);
                        Grid.SetRowSpan(hashcheckimg, 2);
                        if (Main.ToolBar != null)
                            Main.ToolBar.gdPanel.Children.Add(hashcheckimg);
                    }
                }
                else
                {
                    if (hashcheckimg != null)
                    {
                        if (Main.ToolBar != null)
                            Main.ToolBar.gdPanel.Children.Remove(hashcheckimg);
                        hashcheckimg = null;
                    }
                }
            });

        }
    }
    /// <summary>
    /// 关闭确认
    /// </summary>
    public bool CloseConfirm { get; set; } = true;
    /// <summary>
    /// MOD 路径
    /// </summary>
    public List<DirectoryInfo> MODPath { get; set; } = new List<DirectoryInfo>();
    /// <summary>
    /// 全部 MOD 信息
    /// </summary>
    public IEnumerable<IModInfo> ModInfo => CoreMODs;
    /// <summary>
    /// 启用的 MOD
    /// </summary>
    public IEnumerable<IModInfo> OnModInfo => CoreMODs.FindAll(x => x.IsOnMOD(this));
    /// <summary>
    /// 使用物品事件
    /// </summary>
    public event Action<Food>? Event_TakeItem;
    /// <summary>
    /// 使用物品事件 (带数量和来源)
    /// </summary>
    public event Action<Food, int, string>? Event_TakeItemHandle;
    /// <summary>
    /// 鼠标穿透
    /// </summary>
    public bool MouseHitThrough
    {
        get => HitThrough;
        set
        {
            if (value != HitThrough)
                SetTransparentHitThrough();
        }
    }
}
