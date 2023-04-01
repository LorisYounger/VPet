using ChatGPT.API.Framework;
using LinePutScript;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;
using ToolBar = VPet_Simulator.Core.ToolBar;

namespace VPet_Simulator.Windows
{
    public partial class MainWindow : IMainWindow
    {
        public readonly string ModPath = Environment.CurrentDirectory + @"\mod";
        public readonly bool IsSteamUser;
        public Setting Set;
        public List<PetLoader> Pets = new List<PetLoader>();
        public List<CoreMOD> CoreMODs = new List<CoreMOD>();
        public GameCore Core = new GameCore();
        public winGameSetting winSetting;
        public ChatGPTClient CGPTClient;
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
            if (CGPTClient != null)
                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"\ChatGPTSetting.json", CGPTClient.Save());
        }
        public void LoadDIY()
        {
            Main.ToolBar.MenuDIY.Items.Clear();
            foreach (Sub sub in Set["diy"])
                Main.ToolBar.AddMenuButton(ToolBar.MenuType.DIY, sub.Name, () => RunDIY(sub.Info));
        }
        public static void RunDIY(string content)
        {
            if (content.Contains("://") || content.Contains(@":\"))
            {
                Process.Start(content);
            }
            else
            {
                System.Windows.Forms.SendKeys.SendWait(content);
            }
        }

        public void RunAction(string action)
        {
            switch (action)
            {
                case "DisplayNomal":
                    Main.DisplayNomal();
                    break;
                case "DisplayTouchHead":
                    Main.DisplayTouchHead();
                    break;
                case "DisplayTouchBody":
                    Main.DisplayTouchBody();
                    break;
                case "DisplayBoring":
                    Main.DisplayBoring();
                    break;
                case "DisplaySquat":
                    Main.DisplaySquat();
                    break;
                case "DisplaySleep":
                    Main.DisplaySleep();
                    break;
                case "DisplayRaised":
                    Main.DisplayRaised();
                    break;
                case "DisplayFalled_Left":
                    Main.DisplayFalled_Left();
                    break;
                case "DisplayFalled_Right":
                    Main.DisplayFalled_Right();
                    break;
                case "DisplayWalk":
                    if (Function.Rnd.Next(2) == 0)
                        Main.DisplayWalk_Left();
                    else
                        Main.DisplayWalk_Right();
                    break;
                case "DisplayWalk_Left":
                    Main.DisplayWalk_Left();
                    break;
                case "DisplayWalk_Right":
                    Main.DisplayWalk_Right();
                    break;
                case "DisplayCrawl":
                    if (Function.Rnd.Next(2) == 0)
                        Main.DisplayCrawl_Left();
                    else
                        Main.DisplayCrawl_Right();
                    break;
                case "DisplayCrawl_Left":
                    Main.DisplayCrawl_Left();
                    break;
                case "DisplayCrawl_Right":
                    Main.DisplayCrawl_Right();
                    break;
                case "DisplayClimb_Left_UP":
                    Main.DisplayClimb_Left_UP();
                    break;
                case "DisplayClimb_Left_DOWN":
                    Main.DisplayClimb_Left_DOWN();
                    break;
                case "DisplayClimb_Right_UP":
                    Main.DisplayClimb_Right_UP();
                    break;
                case "DisplayClimb_Right_DOWN":
                    Main.DisplayClimb_Right_DOWN();
                    break;
                case "DisplayClimb_Top_Right":
                    Main.DisplayClimb_Top_Right();
                    break;
                case "DisplayClimb_Top_Left":
                    Main.DisplayClimb_Top_Left();
                    break;
                case "DisplayFall":
                    if (Function.Rnd.Next(2) == 0)
                        Main.DisplayFall_Left();
                    else
                        Main.DisplayFall_Right();
                    break;
                case "DisplayFall_Left":
                    Main.DisplayFall_Left();
                    break;
                case "DisplayFall_Right":
                    Main.DisplayFall_Right();
                    break;
                case "DisplayIdel_StateONE":
                    Main.DisplayIdel_StateONE();
                    break;
                case "DisplayIdel_StateTWO":
                    Main.DisplayIdel_StateTWO();
                    break;
            }
        }
    }
}
