using Avalonia;
using LinePutScript;
using LinePutScript.Dictionary;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;

namespace VPet.Solution.MultiPlatform.Models.SettingEditor;

/// <summary>
/// 游戏设置
/// </summary>
public class Setting : LPS_D, ISetting
{
    public static Setting Default { get; } = new("Setting#VPET:|\n");

    /// <summary>
    /// 游戏设置
    /// </summary>
    public Setting(string lps)
        : base(lps)
    {
        var line = FindLine("zoomlevel");
        if (line is null)
            _zoomlevel = 0.5;
        else
        {
            _zoomlevel = line.InfoToDouble;
            if (_zoomlevel < 0.1 || _zoomlevel > 8)
            {
                _zoomlevel = 0.5;
            }
        }
        _presslength = this["gameconfig"].GetInt("presslength", 300);
        _intercycle = this["gameconfig"].GetInt("intercycle", 200);
        _allowmove = this["gameconfig"].GetBool("allowmove") is false;
        _smartmove = this["gameconfig"].GetBool("smartmove");
        _enablefunction = this["gameconfig"].GetBool("nofunction") is false;
        _autobuy = this["gameconfig"].GetBool("autobuy");
        _autogift = this["gameconfig"].GetBool("autogift");
        _autochangewindow = this["gameconfig"].GetBool("autochangewindow") is false;
    }

    private double _zoomlevel;

    /// <summary>
    /// 缩放倍率
    /// </summary>
    public double ZoomLevel
    {
        get => _zoomlevel;
        set
        {
            FindorAddLine("zoomlevel").InfoToDouble = value;
            _zoomlevel = value;
        }
    }

    /// <summary>
    /// 播放声音大小
    /// </summary>
    public double VoiceVolume
    {
        get => (double)GetFloat("voicevolume", 0.5);
        set => SetFloat("voicevolume", value);
    }

    /// <summary>
    /// 是否为更大的屏幕
    /// </summary>
    public bool IsBiggerScreen
    {
        get => GetBool("bigscreen");
        set => SetBool("bigscreen", value);
    }

    /// <summary>
    /// 是否启用数据收集
    /// </summary>
    public bool Diagnosis
    {
        get => this["diagnosis"].GetBool("enable");
        set => this["diagnosis"].SetBool("enable", value);
    }

    /// <summary>
    /// 非计算模式下默认模式
    /// </summary>
    public IGameSave.ModeType CalFunState
    {
        get => (IGameSave.ModeType)this[(gint)"calfunstate"];
        set => this[(gint)"calfunstate"] = (int)value;
    }

    /// <summary>
    /// 数据收集频率
    /// </summary>
    public int DiagnosisInterval
    {
        get => Math.Max(this["diagnosis"].GetInt("interval", 500), 20000);
        set => this["diagnosis"].SetInt("interval", value);
    }

    /// <summary>
    /// 自动保存频率 (min)
    /// </summary>
    public int AutoSaveInterval
    {
        get => Math.Max(GetInt("autosave", 10), -1);
        set => SetInt("autosave", value);
    }

    /// <summary>
    /// 备份保存最大数量
    /// </summary>
    public int BackupSaveMaxNum
    {
        get => Math.Max(GetInt("bakupsave", 50), 1);
        set => SetInt("bakupsave", value);
    }

    /// <summary>
    /// 是否置于顶层
    /// </summary>
    public bool TopMost
    {
        get => GetBool("topmost") is false;
        set => SetBool("topmost", value is false);
    }

    /// <summary>
    /// 是否显示宠物帮助窗口
    /// </summary>
    public bool PetHelper
    {
        get => GetBool("pethelper");
        set => SetBool("pethelper", value);
    }

    /// <summary>
    /// 是否鼠标穿透
    /// </summary>
    public bool HitThrough
    {
        get => GetBool("hitthrough");
        set => SetBool("hitthrough", value);
    }

    /// <summary>
    /// 上次清理缓存日期
    /// </summary>
    public DateTime LastCacheDate
    {
        get => GetDateTime("lastcachedate", DateTime.MinValue);
        set => SetDateTime("lastcachedate", value);
    }

    /// <summary>
    /// 数据收集是否被禁止 (当日)
    /// </summary>
    public bool DiagnosisDayEnable { get; set; } = true;

    /// <summary>
    /// 语言
    /// </summary>
    public string Language
    {
        get => GetString("language", "null")!;
        set => this[(gstr)"language"] = value;
    }

    /// <summary>
    /// 字体
    /// </summary>
    public string Font
    {
        get => GetString("font", "OPPOSans R")!;
        set => this[(gstr)"font"] = value;
    }

    /// <summary>
    /// 主题
    /// </summary>
    public string Theme
    {
        get
        {
            var line = FindLine("theme");
            if (line is null)
                return "default";
            return line.Info;
        }
        set { FindorAddLine("theme").Info = value; }
    }

    /// <summary>
    /// 当前宠物的储存数据
    /// </summary>
    public ILine PetData_OLD => this["petdata"];

    /// <summary>
    /// 储存顺序次数++
    /// </summary>
    public int SaveTimesPP
    {
        get
        {
            var list = GetInt("savetimes", 100000) + 1;
            SetInt("savetimes", list);
            return list;
        }
    }

    /// <summary>
    /// 储存顺序次数
    /// </summary>
    public int SaveTimes
    {
        get => GetInt("savetimes", 100000);
        set => SetInt("savetimes", value);
    }

    private int _presslength;
    private int _intercycle;

    /// <summary>
    /// 按多久视为长按, 单位毫秒
    /// </summary>
    public int PressLength
    {
        get => _presslength;
        set
        {
            _presslength = value;
            this["gameconfig"].SetInt("presslength", value);
        }
    }

    /// <summary>
    /// 互动周期
    /// </summary>
    public int InteractionCycle
    {
        get => _intercycle;
        set
        {
            _intercycle = value;
            this["gameconfig"].SetInt("intercycle", value);
        }
    }

    /// <summary>
    /// 计算间隔 (秒)
    /// </summary>
    public double LogicInterval
    {
        get => this["gameconfig"].GetDouble("logicinterval", 15);
        set => this["gameconfig"].SetDouble("logicinterval", value);
    }

    /// <summary>
    /// 宠物助手左侧位置
    /// </summary>
    public double PetHelpLeft
    {
        get => (double)this["pethelp"].GetFloat("left", 0);
        set => this["pethelp"].SetFloat("left", value);
    }

    /// <summary>
    /// 宠物助手顶部位置
    /// </summary>
    public double PetHelpTop
    {
        get => (double)this["pethelp"].GetFloat("top", 0);
        set => this["pethelp"].SetFloat("top", value);
    }

    private bool _allowmove;

    /// <summary>
    /// 允许移动事件
    /// </summary>
    public bool AllowMove
    {
        get => _allowmove;
        set
        {
            _allowmove = value;
            this["gameconfig"].SetBool("allowmove", value is false);
        }
    }

    private bool _smartmove;

    /// <summary>
    /// 智能移动
    /// </summary>
    public bool SmartMove
    {
        get => _smartmove;
        set
        {
            _smartmove = value;
            this["gameconfig"].SetBool("smartmove", value);
        }
    }

    private bool _enablefunction;

    /// <summary>
    /// 启用计算等数据功能
    /// </summary>
    public bool EnableFunction
    {
        get => _enablefunction;
        set
        {
            _enablefunction = value;
            this["gameconfig"].SetBool("nofunction", value is false);
        }
    }

    private bool _autochangewindow;

    /// <summary>
    /// 自动切换窗口
    /// </summary>
    public bool AutoChangeWindow
    {
        get => _autochangewindow is false;
        set
        {
            _autochangewindow = value is false;
            this["gameconfig"].SetBool("autochangewindow", value);
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
    /// 消息框外置
    /// </summary>
    public bool MessageBarOutside
    {
        get => this["gameconfig"].GetBool("msgbarout");
        set => this["gameconfig"].SetBool("msgbarout", value);
    }

    /// <summary>
    /// 开机启动
    /// </summary>
    public bool StartUPBoot
    {
        get => this["gameconfig"].GetBool("startboot");
        set => this["gameconfig"].SetBool("startboot", value);
    }

    /// <summary>
    /// 开机启动 Steam
    /// </summary>
    public bool StartUPBootSteam
    {
        get => this["gameconfig"].GetBool("startbootsteam") is false;
        set => this["gameconfig"].SetBool("startbootsteam", value is false);
    }

    /// <summary>
    /// 桌宠选择内容
    /// </summary>
    public string PetGraph
    {
        get => this["gameconfig"].GetString("petgraph", "vup")!;
        set => this["gameconfig"].SetString("petgraph", value);
    }

    /// <summary>
    /// 是否记录游戏退出位置 (默认: 是)
    /// </summary>
    public bool StartRecordLast
    {
        get => this["startrecordlast"].GetBool("enable") is false;
        set => this["startrecordlast"].SetBool("enable", value is false);
    }

    /// <summary>
    /// 记录上次退出位置
    /// </summary>
    public Point StartRecordLastPoint
    {
        get
        {
            var line = FindLine("startrecordlast");
            if (line is null)
                return new Point(100, 100);
            return new Point(line.GetDouble("x", 100), line.GetDouble("y", 100));
        }
        set
        {
            var line = FindorAddLine("startrecordlast");
            line.SetDouble("x", Math.Min(Math.Max(value.X, -65000), 65000));
            line.SetDouble("y", Math.Min(Math.Max(value.Y, -65000), 65000));
        }
    }

    /// <summary>
    /// 设置中桌宠启动的位置
    /// </summary>
    public Point StartRecordPoint
    {
        get
        {
            var line = FindLine("startrecord");
            if (line is null)
                return StartRecordLastPoint;
            return new Point(line.GetDouble("x", 0), line.GetDouble("y", 0));
        }
        set
        {
            var line = FindorAddLine("startrecord");
            line.SetDouble("x", Math.Min(Math.Max(value.X, -65000), 65000));
            line.SetDouble("y", Math.Min(Math.Max(value.Y, -65000), 65000));
        }
    }

    /// <summary>
    /// 当实时播放音量达到该值时运行音乐动作
    /// </summary>
    public double MusicCatch
    {
        get => Math.Max(this["gameconfig"].GetDouble("musiccatch", 0.3), 0.02);
        set => this["gameconfig"].SetDouble("musiccatch", value);
    }

    /// <summary>
    /// 当实时播放音量达到该值时运行特殊音乐动作
    /// </summary>
    public double MusicMax
    {
        get => Math.Max(this["gameconfig"].GetDouble("musicmax", 0.70), 0.02);
        set => this["gameconfig"].SetDouble("musicmax", value);
    }

    /// <summary>
    /// 桌宠图形渲染的分辨率, 越高图形越清晰
    /// </summary>
    public int Resolution
    {
        get => this["gameconfig"].GetInt("resolution", 500);
        set => this["gameconfig"].SetInt("resolution", value);
    }

    private bool _autobuy;

    /// <summary>
    /// 允许桌宠自动购买食品
    /// </summary>
    public bool AutoBuy
    {
        get => _autobuy;
        set
        {
            _autobuy = value;
            this["gameconfig"].SetBool("autobuy", value);
        }
    }

    private bool _autogift;

    /// <summary>
    /// 允许桌宠自动购买礼物
    /// </summary>
    public bool AutoGift
    {
        get => _autogift;
        set
        {
            _autogift = value;
            this["gameconfig"].SetBool("autogift", value);
        }
    }

    /// <summary>
    /// 在任务切换器 (Alt+Tab) 中隐藏窗口
    /// </summary>
    public bool HideFromTaskControl
    {
        get => this["gameconfig"].GetBool("hide_from_task_control");
        set => this["gameconfig"].SetBool("hide_from_task_control", value);
    }

    public bool MoveAreaDefault
    {
        get
        {
            var line = FindLine("movearea");
            if (line is null)
                return true;
            return line.GetBool("set");
        }
        set
        {
            var line = FindorAddLine("movearea");
            line.SetBool("set", value);
        }
    }

    public System.Drawing.Rectangle MoveArea
    {
        get
        {
            var line = FindLine("movearea");
            if (line is null)
                return default;
            return new System.Drawing.Rectangle(
                line.GetInt("x", 0),
                line.GetInt("y", 0),
                line.GetInt("w", 114),
                line.GetInt("h", 514)
            );
        }
        set
        {
            var line = FindorAddLine("movearea");
            line.SetInt("x", value.X);
            line.SetInt("y", value.Y);
            line.SetInt("w", value.Width);
            line.SetInt("h", value.Height);
        }
    }

    /// <summary>
    /// 多人模式下禁止触摸
    /// </summary>
    public bool MPNOTouch
    {
        get => this["mutiplay"].GetBool("notouch");
        set => this["mutiplay"].SetBool("notouch", value);
    }

    public bool DeBug
    {
        get => this[(gbol)"debug"];
        set => this[(gbol)"debug"] = value;
    }

    public double Opacity
    {
        get => Math.Min(Math.Max(this["gameconfig"].GetDouble("opacity", 0.6), 0.05), 1);
        set => this["gameconfig"].SetDouble("opacity", value);
    }

    public bool OpacityMain
    {
        get => this["gameconfig"].GetBool("opacitymain");
        set => this["gameconfig"].SetBool("opacitymain", value);
    }

    public bool OpacityHitThrough
    {
        get => this["gameconfig"].GetBool("opacityhitthrough") is false;
        set => this["gameconfig"].SetBool("opacityhitthrough", value is false);
    }

    public int GameScreenIndex
    {
        get => this["gameconfig"].GetInt("gamescreenindex", 0);
        set => this["gameconfig"].SetInt("gamescreenindex", value);
    }

    public long SteamID
    {
        get => this[(gi64)"steamid"];
        set => this[(gi64)"steamid"] = value;
    }

    /// <summary>
    /// 读写自定义游戏设置 (给 mod 准备的接口)
    /// </summary>
    /// <param name="lineName">游戏设置行名</param>
    /// <returns>如果找到相同名称的第一个 Line, 则为该 Line; 否则为新建的相同名称 Line</returns>
    ILine ISetting.this[string lineName]
    {
        get
        {
            if (lineName == "onmod")
                return new Line("onmod", "true");
            return FindorAddLine(lineName);
        }
        set
        {
            if (value.Name == "onmod")
                return;
            AddorReplaceLine(value);
        }
    }

    public void SetZoomLevel(double level) => ZoomLevel = level;

    public void SetVoiceVolume(double volume) => VoiceVolume = volume;

    public void SetAutoSaveInterval(int interval) => AutoSaveInterval = interval;

    public void SetTopMost(bool topMost) => TopMost = topMost;

    public void SetLanguage(string language) => Language = language;

    public void SetLogicInterval(double interval) => LogicInterval = interval;

    public void SetAllowMove(bool allowMove) => AllowMove = allowMove;

    public void SetSmartMove(bool smartMove) => SmartMove = smartMove;

    public void SetEnableFunction(bool enableFunction) => EnableFunction = enableFunction;

    public void SetSmartMoveInterval(int interval) => SmartMoveInterval = interval;
}
