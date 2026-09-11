using LinePutScript;
using LinePutScript.Dictionary;
using System;
using System.IO;

namespace VPet_Simulator.Core.MutiPlatform;

/// <summary>
/// 游戏设置
/// </summary>
/// 对应 Windows 版 VPet-Simulator.Windows/Function/Setting.cs 里与桌宠本体相关的
/// 那部分. 只搬跨平台版真正用得上的项, 商店/联机/自动购买之类还没移植的功能不搬 ——
/// 存进去也没人读, 反而让人以为能用.
///
/// 键名和取值方式(包括几个反着存的布尔)与 Windows 版逐字一致, 这样两边的
/// Setting.lps 可以互换: 用户从 Windows 换到 Linux 时设置不会丢.
public class AppSettings : LPS_D
{
    private const string FileName = "Setting.lps";

    /// <summary>
    /// 设置文件路径 (默认那只桌宠)
    /// </summary>
    public static string SettingPath => Path.Combine(AppPaths.DataRoot, FileName);

    /// <summary>
    /// 这份设置属于哪只桌宠
    /// </summary>
    /// 多开时每只有自己的一套设置和存档, 空前缀是默认那只
    public string Prefix { get; private set; } = string.Empty;

    public AppSettings() : this("")
    {
    }

    public AppSettings(string lps) : base(lps)
    {
        var line = FindLine("zoomlevel");
        if (line == null)
            zoomlevel = 0.5;
        else
        {
            zoomlevel = line.InfoToDouble;
            if (zoomlevel < 0.1 || zoomlevel > 8)
            {
                zoomlevel = 0.5;
            }
        }
        presslength = this["gameconfig"].GetInt("presslength", 300);
        intercycle = this["gameconfig"].GetInt("intercycle", 200);
        allowmove = !this["gameconfig"].GetBool("allowmove");
        smartmove = this["gameconfig"].GetBool("smartmove");
        enablefunction = !this["gameconfig"].GetBool("nofunction");
    }

    /// <summary>
    /// 读取设置, 没有或读不了就用默认值
    /// </summary>
    /// <param name="warning">读取过程中的问题描述</param>
    public static AppSettings Load(out string? warning) => Load(string.Empty, out warning);

    /// <summary>
    /// 读取某只桌宠的设置
    /// </summary>
    /// <param name="prefix">多开前缀, 空表示默认那只</param>
    /// <param name="warning">读取过程中的问题描述</param>
    public static AppSettings Load(string prefix, out string? warning)
    {
        warning = null;
        var path = MultiPetStore.SettingPath(AppPaths.DataRoot, prefix);
        if (!File.Exists(path))
            return new AppSettings { Prefix = prefix };
        try
        {
            return new AppSettings(File.ReadAllText(path)) { Prefix = prefix };
        }
        catch (Exception ex)
        {
            // 设置读不了不该拦住桌宠启动, 用默认值继续跑就是了
            warning = $"{Path.GetFileName(path)} 读取失败, 已改用默认设置: {ex.Message}";
            return new AppSettings { Prefix = prefix };
        }
    }

    /// <summary>
    /// 保存设置
    /// </summary>
    /// 与存档一样先写临时文件再替换, 避免写到一半被中断留下半截文件
    public void Save()
    {
        AppPaths.EnsureDirectory(AppPaths.DataRoot);
        var path = MultiPetStore.SettingPath(AppPaths.DataRoot, Prefix);
        var temp = path + ".tmp";
        File.WriteAllText(temp, ToString());
        File.Move(temp, path, true);
    }

    private double zoomlevel;
    /// <summary>
    /// 缩放倍率
    /// </summary>
    public double ZoomLevel
    {
        get => zoomlevel;
        set
        {
            FindorAddLine("zoomlevel").InfoToDouble = value;
            zoomlevel = value;
        }
    }

    /// <summary>
    /// 是否置顶
    /// </summary>
    /// Windows 版是反着存的: 存 topmost=true 表示"不置顶". 保持一致
    public bool TopMost
    {
        get => !GetBool("topmost");
        set => SetBool("topmost", !value);
    }

    private int presslength;
    /// <summary>
    /// 长按时间
    /// </summary>
    public int PressLength
    {
        get => presslength;
        set
        {
            presslength = value;
            this["gameconfig"].SetInt("presslength", value);
        }
    }

    private int intercycle;
    /// <summary>
    /// 互动周期
    /// </summary>
    public int InteractionCycle
    {
        get => intercycle;
        set
        {
            intercycle = value;
            this["gameconfig"].SetInt("intercycle", value);
        }
    }

    private bool allowmove;
    /// <summary>
    /// 是否允许移动
    /// </summary>
    public bool AllowMove
    {
        get => allowmove;
        set
        {
            allowmove = value;
            this["gameconfig"].SetBool("allowmove", !value);
        }
    }

    private bool smartmove;
    /// <summary>
    /// 是否智能移动
    /// </summary>
    public bool SmartMove
    {
        get => smartmove;
        set
        {
            smartmove = value;
            this["gameconfig"].SetBool("smartmove", value);
        }
    }

    private bool enablefunction;
    /// <summary>
    /// 是否启用数值计算
    /// </summary>
    public bool EnableFunction
    {
        get => enablefunction;
        set
        {
            enablefunction = value;
            this["gameconfig"].SetBool("nofunction", !value);
        }
    }

    /// <summary>
    /// 智能移动周期 (秒)
    /// </summary>
    public int SmartMoveInterval
    {
        get => this["gameconfig"].GetInt("smartmoveinterval", 20 * 60);
        set => this["gameconfig"].SetInt("smartmoveinterval", value);
    }

    /// <summary>
    /// 存档编号
    /// </summary>
    /// Windows 版有个 SaveTimesPP 属性, 取一次就加一, 这边把递增放在 SaveStore.Save
    /// 里显式做, 免得"读一下属性就有副作用"这种事散落在各处.
    public int SaveTimes
    {
        get => GetInt("savetimes", Unified.Services.SaveCatalog.FirstSaveTimes);
        set => SetInt("savetimes", value);
    }

    /// <summary>
    /// 存档目录里最多留几份
    /// </summary>
    public int BackupSaveMaxNum
    {
        get => Math.Max(GetInt("bakupsave", 50), 1);
        set => SetInt("bakupsave", value);
    }

    /// <summary>
    /// 主题名
    /// </summary>
    /// 键名与 Windows 版一致(theme 行), 缺省 default —— 设置文件可以两边互拷
    public string Theme
    {
        get => FindLine("theme")?.Info ?? "default";
        set => FindorAddLine("theme").Info = value;
    }

    /// <summary>
    /// 字体名
    /// </summary>
    public string Font
    {
        get => GetString("font", "OPPOSans R")!;
        set => this[(gstr)"font"] = value;
    }
    /// <summary>
    /// 自动保存间隔 (分钟)
    /// </summary>
    public int AutoSaveInterval
    {
        get => Math.Max(GetInt("autosave", 5), 1);
        set => SetInt("autosave", value);
    }

    /// <summary>
    /// 桌宠所在的屏幕序号
    /// </summary>
    public int GameScreenIndex
    {
        get => this["gameconfig"].GetInt("gamescreenindex", 0);
        set => this["gameconfig"].SetInt("gamescreenindex", value);
    }

    /// <summary>
    /// 是否允许桌宠跟着活动窗口换屏幕
    /// </summary>
    /// Windows 版是反着存的: 存 autochangewindow=true 表示"不允许". 保持一致
    public bool AutoChangeWindow
    {
        get => !this["gameconfig"].GetBool("autochangewindow");
        set => this["gameconfig"].SetBool("autochangewindow", !value);
    }

    /// <summary>
    /// 语言
    /// </summary>
    /// "null" 表示还没选过, 启动时按系统语言自动挑一个, 与 Windows 版 Setting.Language 一致
    public string Language
    {
        get => GetString("language", "null")!;
        set => this[(gstr)"language"] = value;
    }
}
