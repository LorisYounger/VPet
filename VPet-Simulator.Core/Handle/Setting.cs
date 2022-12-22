using LinePutScript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// 游戏设置,例如窗体大小等
    /// </summary>
    public class Setting : LpsDocument
    {
        public Setting(string lps) : base(lps)
        {
        }
        public Setting() : base()
        {
        }

        /// <summary>
        /// 窗体宽度
        /// </summary>
        public double Width
        {
            get => this["windows"].GetDouble("width", 250);
            set => this["windows"].SetDouble("width", value);
        }
        /// <summary>
        /// 窗体高度
        /// </summary>
        public double Heigh
        {
            get => this["windows"].GetDouble("heigh", 250);
            set => this["windows"].SetDouble("heigh", value);
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
