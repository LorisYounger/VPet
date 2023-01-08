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
        public ResolutionType Resolution
        {
            get
            {
                var line = FindLine("windowssize");
                if (line == null)
                    return ResolutionType.q1280x720;
                return (ResolutionType)line.InfoToInt;
            }
            set
            {
                FindorAddLine("windowssize").InfoToInt = (int)value;
            }
        }
        public int ZoomLevel
        {
            get
            {
                var line = FindLine("zoomlevel");
                if (line == null)
                    return 75;
                int zl = line.InfoToInt;
                if (zl < 25 || zl > 100)
                {
                    return 75;
                }
                return zl;
            }
            set
            {
                FindorAddLine("zoomlevel").InfoToInt = value;
            }
        }
        /// <summary>
        /// 分辨率类型,仅支持以下分辨率 q:1.77 l:1.6 s:1.33
        /// </summary>
        public enum ResolutionType
        {
            q1280x720,// = 128000720,
            l1280x800,// = 128000800,
            s1280x960,// = 128000960,
            q1440x810,// = 144000810,
            l1440x900,// = 144000900,
            s1440x1080,// = 144001080,
            q1600x900,// = 160000900,
            l1600x1000,// = 160001000,
            s1600x1200,// = 160001200,
            q1680x945,// = 160000945,
            l1680x1050,// = 160001050,
            s1680x1260,// = 160001260,
            q1920x1080,// = 192001080,
            l1920x1200,// = 192001200,
            s1920x1440,// = 192001440,
            q2048x1152,// = 204801152,
            l2048x1260,// = 204801260,
            s2048x1536,// = 204801536,
            q2560x1440,// = 256001440,
            l2560x1600,// = 256001600,
            s2560x1920,// = 256001920,
            q3840x2160,// = 384002160,
            l3840x2400,// = 384002400,
            s3840x2880,// = 384002880,
        }
        /// <summary>
        /// 分辨率表,快速查询分辨率
        /// </summary>
        public static readonly int[] ResolutionList = {
            128000720,
            128000800,
            128000960,
            144000810,
            144000900,
            144001080,
            160000900,
            160001000,
            160001200,
            160000945,
            160001050,
            160001260,
            192001080,
            192001200,
            192001440,
            204801152,
            204801260,
            204801536,
            256001440,
            256001600,
            256001920,
            384002160,
            384002400,
            384002880
        };
        //public static readonly Dictionary<int, int> ResolutionList = new Dictionary<int, int>()
        //{
        //    {1,  128000720 },
        //        128000800,
        //        128000960,
        //        144000810,
        //        144000900,
        //        144001080,
        //        160000900,
        //        160001000,
        //        160001200,
        //        160000945,
        //        160001050,
        //        160001260,
        //        192001080,
        //        192001200,
        //        192001440,
        //        204801152,
        //        204801260,
        //        204801536,
        //        256001440,
        //        256001600,
        //        256001920,
        //        384002160,
        //        384002400,
        //        384002880,
        //};
        public bool IsFullScreen
        {
            get => FindLine("fullscreen") != null;
            set
            {
                if (value)
                    FindorAddLine("fullscreen");
                else
                    RemoveAll("fullscreen");
            }
        }
        /// <summary>
        /// 是否启用数据收集 //TODO:判断游戏是否是原版的
        /// </summary>
        public bool Diagnosis
        {
            get => !this["diagnosis"].GetBool("disable");
            set => this["diagnosis"].SetBool("disable", !value);
        }
        /// <summary>
        /// 数据收集频率
        /// </summary>
        public int DiagnosisInterval
        {
            get => Math.Max(this["diagnosis"].GetInt("interval", 14), 7);
            set => this["diagnosis"].SetInt("interval", value);
        }
        /// <summary>
        /// 自动保存频率
        /// </summary>
        public int AutoSaveInterval
        {
            get => Math.Max(GetInt("autosave", 7), 0);
            set => SetInt("autosave", value);
        }
        /// <summary>
        /// 桌面图标是否自动对齐
        /// </summary>
        public bool ShortcutAlignment
        {
            get => !GetBool("shortcut_alignment");
            set => SetBool("shortcut_alignment", !value);
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

        /// <summary>
        /// 按多久视为长按 单位毫秒
        /// </summary>
        public int PressLength
        {
            get => this["windows"].GetInt("presslength", 500);
            set => this["windows"].SetInt("presslength", value);
        }
    }
}
