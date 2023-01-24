using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VPet_Simulator.Core;

namespace VPet_Simulator.Windows
{
    public partial class MainWindow
    {
        public readonly string ModPath = Environment.CurrentDirectory + @"\mod";
        public readonly bool IsSteamUser;
        public Setting Set;
        public List<PetLoader> Pets = new List<PetLoader>();
        public List<CoreMOD> CoreMODs = new List<CoreMOD>();
        public GameCore Core = new GameCore();
        public winGameSetting winSetting;
        /// <summary>
        /// 版本号
        /// </summary>
        public readonly int verison = 10;
        /// <summary>
        /// 版本号
        /// </summary>
        public string Verison => $"{verison / 100}.{verison % 100}";

        public void SetZoomLevel(double zl)
        {
            Set.ZoomLevel = zl;
            this.Height = 500 * zl;
            this.Width = 500 * zl;
        }
        /// <summary>
        /// 保存设置
        /// </summary>
        public void Save()
        {
            //游戏存档
            if (Set != null)
                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"\Setting.lps", Set.ToString());
            if (Core != null && Core.Save != null)
                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"\Save.lps", Core.Save.ToLine().ToString());

        }
    }
}
