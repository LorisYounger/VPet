using ChatGPT.API.Framework;
using LinePutScript;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;
using ToolBar = VPet_Simulator.Core.ToolBar;

namespace VPet_Simulator.Windows
{
    public partial class MainWindow : IMainWindow
    {
        public readonly string ModPath = Environment.CurrentDirectory + @"\mod";
        public bool IsSteamUser { get; }
        public Setting Set { get; set; }
        public List<PetLoader> Pets { get; set; } = new List<PetLoader>();
        public List<CoreMOD> CoreMODs = new List<CoreMOD>();
        public GameCore Core { get; set; } = new GameCore();
        public Main Main { get; set; }
        public UIElement TalkBox;
        public winGameSetting winSetting { get; set; }
        public winBetterBuy winBetterBuy { get; set; }
        public ChatGPTClient CGPTClient;
        public ImageResources ImageSources { get; set; } = new ImageResources();
        /// <summary>
        /// 所有三方插件
        /// </summary>
        public List<MainPlugin> Plugins { get; } = new List<MainPlugin>();
        public List<Food> Foods { get; } = new List<Food>();
        /// <summary>
        /// 版本号
        /// </summary>
        public int verison { get; } = 21;
        /// <summary>
        /// 版本号
        /// </summary>
        public string Verison => $"{verison / 100}.{verison % 100}";

        public List<LowText> LowFoodText { get; set; } = new List<LowText>();

        public List<LowText> LowDrinkText { get; set; } = new List<LowText>();

        public void SetZoomLevel(double zl)
        {
            Set.ZoomLevel = zl;
            //this.Height = 500 * zl;
            this.Width = 500 * zl;
        }
        /// <summary>
        /// 保存设置
        /// </summary>
        public void Save()
        {
            foreach (MainPlugin mp in Plugins)
                mp.Save();
            //游戏存档
            if (Set != null)
            {
                var st = Set.Statistics[(gint)"savetimes"]++;
                if (Main != null)
                {
                    Set.VoiceVolume = Main.PlayVoiceVolume;
                    List<string> list = new List<string>();
                    Foods.FindAll(x => x.Star).ForEach(x => list.Add(x.Name));
                    Set["betterbuy"]["star"].info = string.Join(",", list);
                }

                if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"\Save.lps"))
                {
                    File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"\Setting.lps", Set.ToString());
                    var ds = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + @"\UserData");
                    if (ds.Length > 20)
                        File.Delete(ds[0]);

                    File.Move(AppDomain.CurrentDomain.BaseDirectory + @"\Save.lps", AppDomain.CurrentDomain.BaseDirectory + $"\\UserData\\Save_{st}.lps");
                    if (Core != null && Core.Save != null)
                        File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"\Save.lps", Core.Save.ToLine().ToString());
                    if (CGPTClient != null)
                        File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"\ChatGPTSetting.json", CGPTClient.Save());
                }
            }
        }
        /// <summary>
        /// 重载DIY按钮区域
        /// </summary>
        public void LoadDIY()
        {
            Main.ToolBar.MenuDIY.Items.Clear();
            foreach (Sub sub in Set["diy"])
                Main.ToolBar.AddMenuButton(ToolBar.MenuType.DIY, sub.Name, () =>
                {
                    Main.ToolBar.Visibility = Visibility.Collapsed;
                    RunDIY(sub.Info);
                });
            try
            {
                //加载游戏创意工坊插件
                foreach (MainPlugin mp in Plugins)
                    mp.LoadDIY();
            }
            catch (Exception e)
            {
                new winReport(this, "由于插件引起的自定按钮加载错误\n" + e.ToString()).Show();
            }
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
        int lowstrengthAskCountFood = 1;
        int lowstrengthAskCountDrink = 1;
        private void lowStrength()
        {
            if (Core.Save.Mode == GameSave.ModeType.Happy || Core.Save.Mode == GameSave.ModeType.Nomal)
            {
                if (Core.Save.StrengthFood < 75 && Function.Rnd.Next(lowstrengthAskCountFood--) == 0)
                {
                    lowstrengthAskCountFood = 20;
                    var like = Core.Save.Likability < 40 ? 0 : (Core.Save.Likability < 70 ? 1 : (Core.Save.Likability < 100 ? 2 : 3));
                    var txt = LowFoodText.FindAll(x => x.Mode == LowText.ModeType.H && (int)x.Like < like);
                    if (Core.Save.StrengthFood > 60)
                    {
                        txt = txt.FindAll(x => x.Strength == LowText.StrengthType.L);
                        Main.Say(txt[Function.Rnd.Next(txt.Count)].Text, GraphCore.Helper.SayType.None);
                    }
                    else if (Core.Save.StrengthFood > 40)
                    {
                        txt = txt.FindAll(x => x.Strength == LowText.StrengthType.M);
                        Main.Say(txt[Function.Rnd.Next(txt.Count)].Text, GraphCore.Helper.SayType.None);
                    }
                    else
                    {
                        txt = txt.FindAll(x => x.Strength == LowText.StrengthType.S);
                        Main.Say(txt[Function.Rnd.Next(txt.Count)].Text, GraphCore.Helper.SayType.None);
                    }
                    Task.Run(() => Main.Display(GraphCore.GraphType.Switch_Hunger, Main.DisplayToNomal));
                    return;
                }
                if (Core.Save.StrengthDrink < 75 && Function.Rnd.Next(lowstrengthAskCountDrink--) == 0)
                {
                    lowstrengthAskCountDrink = 20;
                    var like = Core.Save.Likability < 40 ? 0 : (Core.Save.Likability < 70 ? 1 : (Core.Save.Likability < 100 ? 2 : 3));
                    var txt = LowDrinkText.FindAll(x => x.Mode == LowText.ModeType.H && (int)x.Like < like);
                    if (Core.Save.StrengthDrink > 60)
                    {
                        txt = txt.FindAll(x => x.Strength == LowText.StrengthType.L);
                        Main.Say(txt[Function.Rnd.Next(txt.Count)].Text, GraphCore.Helper.SayType.None);
                    }
                    else if (Core.Save.StrengthDrink > 40)
                    {
                        txt = txt.FindAll(x => x.Strength == LowText.StrengthType.M);
                        Main.Say(txt[Function.Rnd.Next(txt.Count)].Text, GraphCore.Helper.SayType.None);
                    }
                    else
                    {
                        txt = txt.FindAll(x => x.Strength == LowText.StrengthType.S);
                        Main.Say(txt[Function.Rnd.Next(txt.Count)].Text, GraphCore.Helper.SayType.None);
                    }
                    Task.Run(() => Main.Display(GraphCore.GraphType.Switch_Thirsty, Main.DisplayToNomal));
                    return;
                }
            }
            else
            {
                if (Core.Save.StrengthFood < 60 && Function.Rnd.Next(lowstrengthAskCountFood--) == 0)
                {
                    var like = Core.Save.Likability < 40 ? 0 : (Core.Save.Likability < 70 ? 1 : (Core.Save.Likability < 100 ? 2 : 3));
                    var txt = LowFoodText.FindAll(x => x.Mode == LowText.ModeType.L && (int)x.Like < like);
                    if (Core.Save.StrengthFood > 40)
                    {
                        txt = txt.FindAll(x => x.Strength == LowText.StrengthType.L);
                        Main.Say(txt[Function.Rnd.Next(txt.Count)].Text, GraphCore.Helper.SayType.None);
                    }
                    else if (Core.Save.StrengthFood > 20)
                    {
                        txt = txt.FindAll(x => x.Strength == LowText.StrengthType.M);
                        Main.Say(txt[Function.Rnd.Next(txt.Count)].Text, GraphCore.Helper.SayType.None);
                    }
                    else
                    {
                        txt = txt.FindAll(x => x.Strength == LowText.StrengthType.S);
                        Main.Say(txt[Function.Rnd.Next(txt.Count)].Text, GraphCore.Helper.SayType.None);
                    }
                    Task.Run(() => Main.Display(GraphCore.GraphType.Switch_Hunger, Main.DisplayToNomal));
                    return;
                }
                if (Core.Save.StrengthDrink < 60 && Function.Rnd.Next(lowstrengthAskCountDrink--) == 0)
                {
                    var like = Core.Save.Likability < 40 ? 0 : (Core.Save.Likability < 70 ? 1 : (Core.Save.Likability < 100 ? 2 : 3));
                    var txt = LowDrinkText.FindAll(x => x.Mode == LowText.ModeType.L && (int)x.Like < like);
                    if (Core.Save.StrengthDrink > 40)
                    {
                        txt = txt.FindAll(x => x.Strength == LowText.StrengthType.L);
                        Main.Say(txt[Function.Rnd.Next(txt.Count)].Text, GraphCore.Helper.SayType.None);
                    }
                    else if (Core.Save.StrengthDrink > 20)
                    {
                        txt = txt.FindAll(x => x.Strength == LowText.StrengthType.M);
                        Main.Say(txt[Function.Rnd.Next(txt.Count)].Text, GraphCore.Helper.SayType.None);
                    }
                    else
                    {
                        txt = txt.FindAll(x => x.Strength == LowText.StrengthType.S);
                        Main.Say(txt[Function.Rnd.Next(txt.Count)].Text, GraphCore.Helper.SayType.None);
                    }
                    Task.Run(() => Main.Display(GraphCore.GraphType.Switch_Thirsty, Main.DisplayToNomal));
                    return;
                }
            }


        }   
    }
}
