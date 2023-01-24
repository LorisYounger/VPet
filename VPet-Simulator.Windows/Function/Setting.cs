using LinePutScript;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VPet_Simulator.Windows
{
    public class Setting : LpsDocument
    {
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
        }

        public void Save()
        {
            File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"\Setting.lps", ToString());
        }

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
        public bool IsBanMod(string ModName)
        {
            var line = FindLine("banmod");
            if (line == null)
                return false;
            return line.Find(ModName.ToLower()) != null;
        }
        public bool IsPassMOD(string ModName)
        {
            var line = FindLine("passmod");
            if (line == null)
                return false;
            return line.Find(ModName.ToLower()) != null;
        }
        public void BanMod(string ModName)
        {
            if (string.IsNullOrWhiteSpace(ModName))
                return;
            FindorAddLine("banmod").AddorReplaceSub(new Sub(ModName.ToLower()));
        }
        public void BanModRemove(string ModName)
        {
            FindorAddLine("banmod").Remove(ModName.ToLower());
        }
        public void PassMod(string ModName)
        {
            FindorAddLine("passmod").AddorReplaceSub(new Sub(ModName.ToLower()));
        }
        public void PassModRemove(string ModName)
        {
            FindorAddLine("passmod").Remove(ModName.ToLower());
        }

        private int presslength;
        private int intercycle;
        /// <summary>
        /// 按多久视为长按 单位毫秒
        /// </summary>
        public int PressLength
        {
            get => presslength;
            set => this["gameconfig"].SetInt("presslength", value);
        }
        /// <summary>
        /// 互动周期
        /// </summary>
        public int InteractionCycle
        {
            get => intercycle;
            set => this["gameconfig"].SetInt("intercycle", value);
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
            get => this["gameconfig"].GetInt("smartmoveinterval", 20*60);
            set => this["gameconfig"].SetInt("smartmoveinterval", value);
        }
    }
}
