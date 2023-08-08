using ChatGPT.API.Framework;
using LinePutScript;
using LinePutScript.Localization.WPF;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;
using static VPet_Simulator.Core.GraphInfo;
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
        public int verison { get; } = 50;
        /// <summary>
        /// 版本号
        /// </summary>
        public string Verison => $"{verison / 100}.{verison % 100}";

        public List<LowText> LowFoodText { get; set; } = new List<LowText>();

        public List<LowText> LowDrinkText { get; set; } = new List<LowText>();
        /// <summary>
        /// 存档 Hash检查 是否通过
        /// </summary>
        public bool HashCheck { get; set; } = true;
        public void SetZoomLevel(double zl)
        {
            Set.ZoomLevel = zl;
            //this.Height = 500 * zl;
            this.Width = 500 * zl;
            if (petHelper != null)
            {
                petHelper.Width = 50 * zl;
                petHelper.Height = 50 * zl;
                petHelper.ReloadLocation();
            }
        }
        //private DateTime timecount = DateTime.Now;
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
                    //Set.Statistics[(gint)"stat_time"] = (int)(DateTime.Now - timecount).TotalMinutes;
                    //timecount = DateTime.Now;
                }
                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"\Setting.lps", Set.ToString());

                if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + @"\UserData"))
                    Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + @"\UserData");

                if (Core != null && Core.Save != null)
                {
                    var ds = new List<string>(Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + @"\UserData"));
                    while (ds.Count > Set.BackupSaveMaxNum)
                    {
                        File.Delete(ds[0]);
                        ds.RemoveAt(0);
                    }
                    if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + $"\\UserData\\Save_{st}.lps"))
                        File.Delete(AppDomain.CurrentDomain.BaseDirectory + $"\\UserData\\Save_{st}.lps");

                    if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"\Save.lps"))
                        File.Move(AppDomain.CurrentDomain.BaseDirectory + @"\Save.lps", AppDomain.CurrentDomain.BaseDirectory + $"\\UserData\\Save_{st}.lps");
                    var l = Core.Save.ToLine();
                    if (HashCheck)
                    {
                        l[(gi64)"hash"] = new Line(l.ToString()).GetLongHashCode();
                    }
                    else
                        l[(gint)"hash"] = -1;
                    File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"\Save.lps", l.ToString());
                }
                if (CGPTClient != null)
                    File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"\ChatGPTSetting.json", CGPTClient.Save());
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
                new winReport(this, "由于插件引起的自定按钮加载错误".Translate() + '\n' + e.ToString()).Show();
            }
        }
        /// <summary>
        /// 加载帮助器
        /// </summary>
        public void LoadPetHelper()
        {
            petHelper = new PetHelper(this);
            petHelper.Show();
        }

        public static void RunDIY(string content)
        {
            if (content.Contains("://") || content.Contains(@":\"))
            {
                try
                {
                    Process.Start(content);
                }
                catch (Exception e)
                {
                    MessageBox.Show("快捷键运行失败:无法运行指定内容".Translate() + '\n' + e.Message);
                }
            }
            else
            {
                System.Windows.Forms.SendKeys.SendWait(content);
            }
        }

        public void ShowSetting(int page = -1)
        {
            if (page >= 0 && page <= 6)
                winSetting.MainTab.SelectedIndex = page;
            winSetting.Show();
        }
        public void ShowBetterBuy(Food.FoodType type)
        {
            winBetterBuy.Show(type);
        }
        int lowstrengthAskCountFood = 20;
        int lowstrengthAskCountDrink = 20;
        private void lowStrength()
        {
            if (Core.Save.Mode == GameSave.ModeType.Happy || Core.Save.Mode == GameSave.ModeType.Nomal)
            {
                if (Core.Save.StrengthFood < 75 && Function.Rnd.Next(lowstrengthAskCountFood--) == 0)
                {
                    lowstrengthAskCountFood = Set.InteractionCycle;
                    var like = Core.Save.Likability < 40 ? 0 : (Core.Save.Likability < 70 ? 1 : (Core.Save.Likability < 100 ? 2 : 3));
                    var txt = LowFoodText.FindAll(x => x.Mode == LowText.ModeType.H && (int)x.Like <= like);
                    if (txt.Count != 0)
                        if (Core.Save.StrengthFood > 60)
                        {
                            txt = txt.FindAll(x => x.Strength == LowText.StrengthType.L);
                            Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateText);
                        }
                        else if (Core.Save.StrengthFood > 40)
                        {
                            txt = txt.FindAll(x => x.Strength == LowText.StrengthType.M);
                            Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateText);
                        }
                        else
                        {
                            txt = txt.FindAll(x => x.Strength == LowText.StrengthType.S);
                            Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateText);
                        }
                    Main.DisplayStopForce(() => Main.Display(GraphType.Switch_Hunger, AnimatType.Single, Main.DisplayToNomal));
                    return;
                }
                if (Core.Save.StrengthDrink < 75 && Function.Rnd.Next(lowstrengthAskCountDrink--) == 0)
                {
                    lowstrengthAskCountDrink = Set.InteractionCycle;
                    var like = Core.Save.Likability < 40 ? 0 : (Core.Save.Likability < 70 ? 1 : (Core.Save.Likability < 100 ? 2 : 3));
                    var txt = LowDrinkText.FindAll(x => x.Mode == LowText.ModeType.H && (int)x.Like <= like);
                    if (txt.Count != 0)
                        if (Core.Save.StrengthDrink > 60)
                        {
                            txt = txt.FindAll(x => x.Strength == LowText.StrengthType.L);
                            Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateText);
                        }
                        else if (Core.Save.StrengthDrink > 40)
                        {
                            txt = txt.FindAll(x => x.Strength == LowText.StrengthType.M);
                            Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateText);
                        }
                        else
                        {
                            txt = txt.FindAll(x => x.Strength == LowText.StrengthType.S);
                            Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateText);
                        }
                    Main.DisplayStopForce(() => Main.Display(GraphType.Switch_Thirsty, AnimatType.Single, Main.DisplayToNomal));
                    return;
                }
            }
            else
            {
                if (Core.Save.StrengthFood < 60 && Function.Rnd.Next(lowstrengthAskCountFood--) == 0)
                {
                    lowstrengthAskCountFood = Set.InteractionCycle;
                    var like = Core.Save.Likability < 40 ? 0 : (Core.Save.Likability < 70 ? 1 : (Core.Save.Likability < 100 ? 2 : 3));
                    var txt = LowFoodText.FindAll(x => x.Mode == LowText.ModeType.L && (int)x.Like < like);
                    if (Core.Save.StrengthFood > 40)
                    {
                        txt = txt.FindAll(x => x.Strength == LowText.StrengthType.L);
                        Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateText);
                    }
                    else if (Core.Save.StrengthFood > 20)
                    {
                        txt = txt.FindAll(x => x.Strength == LowText.StrengthType.M);
                        Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateText);
                    }
                    else
                    {
                        txt = txt.FindAll(x => x.Strength == LowText.StrengthType.S);
                        Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateText);
                    }
                    Main.DisplayStopForce(() => Main.Display(GraphType.Switch_Hunger, AnimatType.Single, Main.DisplayToNomal));
                    return;
                }
                if (Core.Save.StrengthDrink < 60 && Function.Rnd.Next(lowstrengthAskCountDrink--) == 0)
                {
                    lowstrengthAskCountDrink = Set.InteractionCycle;
                    var like = Core.Save.Likability < 40 ? 0 : (Core.Save.Likability < 70 ? 1 : (Core.Save.Likability < 100 ? 2 : 3));
                    var txt = LowDrinkText.FindAll(x => x.Mode == LowText.ModeType.L && (int)x.Like < like);
                    if (Core.Save.StrengthDrink > 40)
                    {
                        txt = txt.FindAll(x => x.Strength == LowText.StrengthType.L);
                        Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateText);
                    }
                    else if (Core.Save.StrengthDrink > 20)
                    {
                        txt = txt.FindAll(x => x.Strength == LowText.StrengthType.M);
                        Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateText);
                    }
                    else
                    {
                        txt = txt.FindAll(x => x.Strength == LowText.StrengthType.S);
                        Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateText);
                    }
                    Main.DisplayStopForce(() => Main.Display(GraphType.Switch_Thirsty, AnimatType.Single, Main.DisplayToNomal));
                    return;
                }
            }


        }

        public void RunAction(string action)
        {
            switch (action)
            {
                case "DisplayNomal":
                    Main.DisplayNomal();
                    break;
                case "DisplayToNomal":
                    Main.DisplayToNomal();
                    break;
                case "DisplayTouchHead":
                    Main.DisplayTouchHead();
                    break;
                case "DisplayTouchBody":
                    Main.DisplayTouchBody();
                    break;
                case "DisplayIdel":
                    Main.DisplayIdel();
                    break;
                case "DisplayIdel_StateONE":
                    Main.DisplayIdel_StateONE();
                    break;
                case "DisplaySleep":
                    Main.DisplaySleep();
                    break;
                case "DisplayRaised":
                    Main.DisplayRaised();
                    break;
                case "DisplayMove":
                    Main.DisplayMove();
                    break;
            }
        }
        /// <summary>
        /// Steam统计相关变化
        /// </summary>
        private void Statistics_StatisticChanged(Statistics sender, string name, SetObject value)
        {
            if (name.StartsWith("stat_"))
            {
                Steamworks.SteamUserStats.SetStat(name, (int)value);
            }
        }
        /// <summary>
        /// 计算统计数据
        /// </summary>
        private void StatisticsCalHandle()
        {
            var stat = Set.Statistics;
            var save = Core.Save;
            stat["stat_money"] = save.Money;
            stat["stat_level"] = save.Level;
            stat["stat_likability"] = save.Likability;

            stat[(gi64)"stat_total_time"] += (int)Set.LogicInterval;
            switch (Main.State)
            {
                case Main.WorkingState.Work:
                    if (Core.Graph.GraphConfig.Works[Main.StateID].Type == GraphHelper.Work.WorkType.Work)
                        stat[(gi64)"stat_work_time"] += (int)Set.LogicInterval;
                    else
                        stat[(gi64)"stat_study_time"] += (int)Set.LogicInterval;
                    break;
                case Main.WorkingState.Sleep:
                    stat[(gi64)"stat_work_time"] += (int)Set.LogicInterval;
                    break;
            }
            if (save.Mode == GameSave.ModeType.Ill)
            {
                if (save.Money < 10)
                    stat["stat_ill_nomoney"] = 1;
            }
            if (save.Money < save.Level)
            {
                stat["stat_level_g_money"] = 1;
            }
            if (save.Feeling < 1)
            {
                stat["stat_0_feel"] = 1;
                if (save.StrengthDrink < 1)
                    stat["stat_0_f_sd"] = 1;
            }
            if (save.Strength < 1 && save.Feeling < 1 && save.StrengthFood < 1 && save.StrengthDrink < 1)
                stat["stat_0_all"] = 1;
            if (save.StrengthFood < 1)
                stat["stat_0_strengthfood"] = 1;
            if (save.StrengthDrink < 1)
            {
                stat["stat_0_strengthdrink"] = 1;
                if (save.StrengthFood < 1)
                    stat["stat_0_sd_sf"] = 1;
            }
            if (save.Strength > 99 && save.Feeling > 99 && save.StrengthFood > 99 && save.StrengthDrink > 99)
                stat[(gint)"stat_100_all"]++;

            if (IsSteamUser)
            {
                Task.Run(Steamworks.SteamUserStats.StoreStats);
            }
        }
        public void GameLoad(ILine line)
        {
            Core.Save = GameSave.Load(line);
            long hash = line.GetInt64("hash");
            if (line.Remove("hash"))
            {
                HashCheck = line.GetLongHashCode() == hash;
            }
        }
    }
}
