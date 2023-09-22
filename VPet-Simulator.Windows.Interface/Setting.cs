using LinePutScript;
using LinePutScript.Dictionary;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using VPet_Simulator.Core;

namespace VPet_Simulator.Windows.Interface
{
    /// <summary>
    /// 游戏设置
    /// </summary>
    public class Setting : LPS_D
    {
        /// <summary>
        /// 游戏设置
        /// </summary>
        public Setting(string lps) : base(lps)
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
            presslength = this["gameconfig"].GetInt("presslength", 500);
            intercycle = this["gameconfig"].GetInt("intercycle", 200);
            allowmove = !this["gameconfig"].GetBool("allowmove");
            smartmove = this["gameconfig"].GetBool("smartmove");
            enablefunction = !this["gameconfig"].GetBool("nofunction");
            Statistics_OLD = new Statistics(this["statistics"].ToList());
            autobuy = this["gameconfig"].GetBool("autobuy");
            autogift = this["gameconfig"].GetBool("autogift");
        }
        public override string ToString()
        {//留作备份,未来版本删了
            this["statistics"] = new Line("statistics", "", "", Statistics_OLD.ToSubs().ToArray());
            return base.ToString();
        }
        /// <summary>
        /// 统计数据信息(旧)
        /// </summary>
        public Statistics Statistics_OLD;

        //public Size WindowsSize
        //{
        //    get
        //    {
        //        var line = FindLine("windowssize");
        //        if (line == null)
        //            return new Size(1366, 799);
        //        var strs = line.GetInfos();
        //        if (int.TryParse(strs[0], out int x))
        //            x = 1366;
        //        if (int.TryParse(strs[0], out int y))
        //            y = 799;
        //        return new Size(x, y);
        //    }
        //    set
        //    {
        //        FindorAddLine("windowssize").info = $"{value.Width},{value.Height}";
        //    }
        //}
        private double zoomlevel = 0;
        /// <summary>
        /// 缩放倍率
        /// </summary>
        public double ZoomLevel
        {
            get
            {
                return zoomlevel;
            }
            set
            {
                FindorAddLine("zoomlevel").InfoToDouble = value;
                zoomlevel = value;
            }
        }
        /// <summary>
        /// 播放声音大小
        /// </summary>
        public double VoiceVolume
        {
            get => GetFloat("voicevolume", 0.5);
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
        ///// <summary> //经过测试,储存到内存好处多多,不储存也要占用很多内存,干脆存了吧
        ///// 是将图片储存到内存
        ///// </summary>
        //public bool StoreInMemory
        //{
        //    get => !this["set"].GetBool("storemem");
        //    set => this["set"].SetBool("storemem", value);
        //}
        /// <summary>
        /// 非计算模式下默认模式
        /// </summary>
        public GameSave.ModeType CalFunState
        {
            get => (GameSave.ModeType)this[(gint)"calfunstate"];
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
            get => Math.Max(GetInt("bakupsave", 30), 1);
            set => SetInt("bakupsave", value);
        }
        /// <summary>
        /// 是否置于顶层
        /// </summary>
        public bool TopMost
        {
            get => !GetBool("topmost");
            set => SetBool("topmost", !value);
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
        /// 数据收集是否被禁止(当日)
        /// </summary>
        public bool DiagnosisDayEnable = true;
        /// <summary>
        /// 语言
        /// </summary>
        public string Language
        {
            get => GetString("language", "null");
            set => this[(gstr)"language"] = value;
        }
        public string Font
        {
            get => GetString("font", "OPPOSans R");
            set => this[(gstr)"font"] = value;
        }
        public string Theme
        {
            get
            {
                var line = FindLine("theme");
                if (line == null)
                    return "default";
                return line.Info;
            }
            set
            {
                FindorAddLine("theme").Info = value;
            }
        }
        /// <summary>
        /// 当前宠物的储存数据
        /// </summary>
        public ILine PetData => this["petdata"];

        private int presslength;
        private int intercycle;
        /// <summary>
        /// 按多久视为长按 单位毫秒
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
        /// <summary>
        /// 计算间隔 (秒)
        /// </summary>
        public double LogicInterval
        {
            get => this["gameconfig"].GetDouble("logicinterval", 15);
            set => this["gameconfig"].SetDouble("logicinterval", value);
        }

        /// <summary>
        /// 计算间隔
        /// </summary>
        public double PetHelpLeft
        {
            get => this["pethelp"].GetFloat("left", 0);
            set => this["pethelp"].SetFloat("left", value);
        }
        /// <summary>
        /// 计算间隔
        /// </summary>
        public double PetHelpTop
        {
            get => this["pethelp"].GetFloat("top", 0);
            set => this["pethelp"].SetFloat("top", value);
        }

        bool allowmove;
        /// <summary>
        /// 允许移动事件
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
        bool smartmove;
        /// <summary>
        /// 智能移动
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
        bool enablefunction;
        /// <summary>
        /// 启用计算等数据功能
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
            get => !this["gameconfig"].GetBool("startbootsteam");
            set => this["gameconfig"].SetBool("startbootsteam", !value);
        }
        /// <summary>
        /// 桌宠选择内容
        /// </summary>
        public string PetGraph
        {
            get => this["gameconfig"].GetString("petgraph", "默认虚拟桌宠");
            set => this["gameconfig"].SetString("petgraph", value);
        }

        /// <summary>
        /// 是否记录游戏退出位置 (默认:是)
        /// </summary>
        public bool StartRecordLast
        {
            get => !this["gameconfig"].GetBool("startboot");
            set => this["gameconfig"].SetBool("startboot", !value);
        }
        /// <summary>
        /// 记录上次退出位置
        /// </summary>
        public Point StartRecordLastPoint
        {
            get
            {
                var line = FindLine("startrecordlast");
                if (line == null)
                    return new Point(100, 100);
                return new Point(line.GetDouble("x", 0), line.GetDouble("y", 0));
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
                if (line == null)
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
            get => this["gameconfig"].GetDouble("musiccatch", 0.3);
            set => this["gameconfig"].SetDouble("musiccatch", value);
        }
        /// <summary>
        /// 当实时播放音量达到该值时运行特殊音乐动作
        /// </summary>
        public double MusicMax
        {
            get => this["gameconfig"].GetDouble("musicmax", 0.70);
            set => this["gameconfig"].SetDouble("musicmax", value);
        }
        /// <summary>
        /// 桌宠图形渲染的分辨率,越高图形越清晰
        /// </summary>
        public int Resolution
        {
            get => this["gameconfig"].GetInt("resolution", 500);
            set => this["gameconfig"].SetInt("resolution", value);
        }

        bool autobuy;
        /// <summary>
        /// 允许桌宠自动购买食品
        /// </summary>
        public bool AutoBuy
        {
            get => autobuy;
            set
            {
                autobuy = value;
                this["gameconfig"].SetBool("autobuy", value);
            }
        }
        bool autogift;
        /// <summary>
        /// 允许桌宠自动购买礼物
        /// </summary>
        public bool AutoGift
        {
            get => autogift;
            set
            {
                autogift = value;
                this["gameconfig"].SetBool("autogift", value);
            }
        }
        /// <summary>
        /// 在任务切换器(Alt+Tab)中隐藏窗口
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
                if (line == null)
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
                if (line == null)
                    return default(System.Drawing.Rectangle);
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
    }
}
