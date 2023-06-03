using LinePutScript;
using LinePutScript.Dictionary;
using System;
using System.Windows;

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
            enablefunction = !this["gameconfig"].GetBool("enablefunction");
            Statistics = this["statistics"];
        }
        /// <summary>
        /// 统计数据信息
        /// </summary>
        public ILine Statistics;

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
        /// 是否启用数据收集 //TODO:判断游戏是否是原版的
        /// </summary>
        public bool Diagnosis
        {
            get => !this["diagnosis"].GetBool("disable");
            set => this["diagnosis"].SetBool("disable", !value);
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
            get => Math.Max(GetInt("autosave", 20), 0);
            set => SetInt("autosave", value);
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
        /// 数据收集是否被禁止(当日)
        /// </summary>
        public bool DiagnosisDayEnable = true;


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
        /// 计算间隔
        /// </summary>
        public double LogicInterval
        {
            get => this["gameconfig"].GetDouble("logicinterval", 15);
            set => this["gameconfig"].SetDouble("logicinterval", value);
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
                this["gameconfig"].SetBool("function", !value);
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
                    return new Point();
                return new Point(line.GetDouble("x", 0), line.GetDouble("y", 0));
            }
            set
            {
                var line = FindorAddLine("startrecordlast");
                line.SetDouble("x", value.X);
                line.SetDouble("y", value.Y);
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
                line.SetDouble("x", value.X);
                line.SetDouble("y", value.Y);
            }
        }
    }
}
