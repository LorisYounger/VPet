using CSCore.CoreAudioAPI;
using LinePutScript;
using LinePutScript.Dictionary;
using LinePutScript.Localization.WPF;
using Panuon.WPF.UI;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;
using static VPet_Simulator.Core.GraphHelper;
using static VPet_Simulator.Core.GraphInfo;
using Timer = System.Timers.Timer;
using ToolBar = VPet_Simulator.Core.ToolBar;

using MessageBox = System.Windows.MessageBox;
using ContextMenu = System.Windows.Forms.ContextMenu;
using MenuItem = System.Windows.Forms.MenuItem;
using Application = System.Windows.Application;
using Line = LinePutScript.Line;
using static VPet_Simulator.Windows.Interface.ExtensionFunction;
using Image = System.Windows.Controls.Image;

namespace VPet_Simulator.Windows
{
    public partial class MainWindow : IMainWindow
    {
        public readonly string ModPath = ExtensionValue.BaseDirectory + @"\mod";
        public bool IsSteamUser { get; }
        public LPS_D Args { get; }
        public string PrefixSave { get; } = "";
        private string prefixsavetrans = null;
        public string PrefixSaveTrans
        {
            get
            {
                if (prefixsavetrans == null)
                {
                    if (PrefixSave == "")
                        prefixsavetrans = "";
                    else
                        prefixsavetrans = '-' + PrefixSave.TrimStart('-').Translate();
                }
                return prefixsavetrans;
            }
        }
        public Setting Set { get; set; }
        public List<PetLoader> Pets { get; set; } = new List<PetLoader>();
        public List<CoreMOD> CoreMODs = new List<CoreMOD>();
        public GameCore Core { get; set; } = new GameCore();
        public List<Window> Windows { get; set; } = new List<Window>();
        public Main Main { get; set; }
        public UIElement TalkBox;
        public winGameSetting winSetting { get; set; }
        public winBetterBuy winBetterBuy { get; set; }
        //public ChatGPTClient CGPTClient;
        public ImageResources ImageSources { get; set; } = new ImageResources();
        /// <summary>
        /// 所有三方插件
        /// </summary>
        public List<MainPlugin> Plugins { get; } = new List<MainPlugin>();
        public List<Food> Foods { get; } = new List<Food>();
        /// <summary>
        /// 版本号
        /// </summary>
        public int version { get; } = 109;
        /// <summary>
        /// 版本号
        /// </summary>
        public string Version => $"{version / 100}.{version % 100}";

        public List<LowText> LowFoodText { get; set; } = new List<LowText>();

        public List<LowText> LowDrinkText { get; set; } = new List<LowText>();

        public List<SelectText> SelectTexts { get; set; } = new List<SelectText>();

        public List<ClickText> ClickTexts { get; set; } = new List<ClickText>();

        public GameSave_v2 GameSavesData { get; set; }
        /// <summary>
        /// 获得自动点击的文本
        /// </summary>
        /// <returns>说话内容</returns>
        public ClickText GetClickText()
        {
            ClickText.DayTime dt;
            var now = DateTime.Now.Hour;
            if (now < 6)
                dt = ClickText.DayTime.Midnight;
            else if (now < 12)
                dt = ClickText.DayTime.Morning;
            else if (now < 18)
                dt = ClickText.DayTime.Afternoon;
            else
                dt = ClickText.DayTime.Night;

            ClickText.ModeType mt;
            switch (Core.Save.Mode)
            {
                case GameSave.ModeType.PoorCondition:
                    mt = ICheckText.ModeType.PoorCondition;
                    break;
                default:
                case GameSave.ModeType.Nomal:
                    mt = ICheckText.ModeType.Nomal;
                    break;
                case GameSave.ModeType.Happy:
                    mt = ICheckText.ModeType.Happy;
                    break;
                case GameSave.ModeType.Ill:
                    mt = ICheckText.ModeType.Ill;
                    break;
            }
            var list = ClickTexts.FindAll(x => x.DaiTime.HasFlag(dt) && x.Mode.HasFlag(mt) && x.CheckState(Main));
            if (list.Count == 0)
                return null;
            return list[Function.Rnd.Next(list.Count)];
        }
        private Image hashcheckimg;

        /// <summary>
        /// 关闭该玩家的HashCheck检查
        /// 如果你的mod属于作弊mod/有作弊内容,请在作弊前调用这个方法
        /// </summary>
        public void HashCheckOff()
        {
            HashCheck = false;
        }
        /// <summary>
        /// 存档 Hash检查 是否通过
        /// </summary>
        public bool HashCheck
        {
            get => GameSavesData.HashCheck;
            set
            {
                if (!value)
                {
                    GameSavesData.HashCheckOff();
                }
                Main?.Dispatcher.Invoke(() =>
                {
                    if (GameSavesData.HashCheck)
                    {
                        if (hashcheckimg == null)
                        {
                            hashcheckimg = new Image();
                            hashcheckimg.Source = ImageResources.NewSafeBitmapImage("pack://application:,,,/Res/hash.png");
                            hashcheckimg.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                            hashcheckimg.ToolTip = "是没有修改过存档/使用超模MOD的玩家专属标志".Translate();
                            Grid.SetColumn(hashcheckimg, 4);
                            Grid.SetRowSpan(hashcheckimg, 2);
                            Main.ToolBar.gdPanel.Children.Add(hashcheckimg);
                        }
                    }
                    else
                    {
                        if (hashcheckimg != null)
                        {
                            Main.ToolBar.gdPanel.Children.Remove(hashcheckimg);
                            hashcheckimg = null;
                        }
                    }
                });

            }
        }
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
                var st = Set.SaveTimesPP;
                if (Main != null)
                {
                    Set.VoiceVolume = Main.PlayVoiceVolume;
                    List<string> list = new List<string>();
                    Foods.FindAll(x => x.Star).ForEach(x => list.Add(x.Name));
                    Set["betterbuy"]["star"].info = string.Join(",", list);
                    //GameSavesData.Statistics[(gint)"stat_time"] = (int)(DateTime.Now - timecount).TotalMinutes;
                    //timecount = DateTime.Now;
                }
                Set.StartRecordLastPoint = new Point(Dispatcher.Invoke(() => Left), Dispatcher.Invoke(() => Top));
                File.WriteAllText(ExtensionValue.BaseDirectory + @$"\Setting{PrefixSave}.lps", Set.ToString());

                if (!Directory.Exists(ExtensionValue.BaseDirectory + @"\Saves"))
                    Directory.CreateDirectory(ExtensionValue.BaseDirectory + @"\Saves");

                if (Core != null && Core.Save != null)
                {
                    var ds = new List<string>(Directory.GetFiles(ExtensionValue.BaseDirectory + @"\Saves", $"Save{PrefixSave}_*.lps")).OrderBy(x =>
                    {
                        if (int.TryParse(x.Split('_').Last().Split('.')[0], out int i))
                            return i;
                        return 0;
                    }).ToList();
                    while (ds.Count > Set.BackupSaveMaxNum)
                    {
                        File.Delete(ds[0]);
                        ds.RemoveAt(0);
                    }
                    if (File.Exists(ExtensionValue.BaseDirectory + $"\\Saves\\Save{PrefixSave}_{st}.lps"))
                        File.Delete(ExtensionValue.BaseDirectory + $"\\Saves\\Save{PrefixSave}_{st}.lps");

                    File.WriteAllText(ExtensionValue.BaseDirectory + $"\\Saves\\Save{PrefixSave}_{st}.lps", GameSavesData.ToLPS().ToString());

                    if (File.Exists(ExtensionValue.BaseDirectory + @"\Save.lps"))
                    {
                        if (File.Exists(ExtensionValue.BaseDirectory + @"\Save.bkp"))
                            File.Delete(ExtensionValue.BaseDirectory + @"\Save.bkp");
                        File.Move(ExtensionValue.BaseDirectory + @"\Save.lps", ExtensionValue.BaseDirectory + @"\Save.bkp");
                    }

                }
            }
        }
        /// <summary>
        /// 重载DIY按钮区域
        /// </summary>
        public void LoadDIY()
        {
            Main.ToolBar.MenuDIY.Items.Clear();

            if (App.MutiSaves.Count > 1)
            {
                var list = App.MutiSaves.ToList();
                foreach (var win in App.MainWindows)
                {
                    list.Remove(win.PrefixSave);
                }
                list.Remove(PrefixSave);
                if (list.Count > 0)
                {
                    var menuItem = new System.Windows.Controls.MenuItem()
                    {
                        Header = "桌宠多开".Translate(),
                        HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center,
                    };
                    foreach (var win in list)
                    {
                        var mo = new System.Windows.Controls.MenuItem()
                        {
                            Header = win.Translate(),
                            HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center,
                        };
                        mo.Click += (s, e) =>
                        {
                            if (App.MainWindows.FirstOrDefault(x => x.PrefixSave.Trim('-') == win) == null)
                            {
                                new MainWindow(win).Show();
                            }
                            menuItem.Items.Remove(s);
                        };
                        menuItem.Items.Add(mo);
                    }
                    Main.ToolBar.MenuDIY.Items.Add(menuItem);
                }
            }

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
                MessageBoxX.Show(e.ToString(), "由于插件引起的自定按钮加载错误".Translate());
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
                try
                {
                    SendKeys.SendWait(content);
                }
                catch (Exception e)
                {
                    MessageBox.Show("快捷键运行失败:无法运行指定内容".Translate() + '\n' + e.Message);
                }
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
            if (Set.AutoBuy && Core.Save.Money >= 100)
            {
                var havemoney = Core.Save.Money * 1.2 - 120;
                List<Food> food = Foods.FindAll(x => x.Price >= 2 && x.Health >= 0 && x.Exp >= 0 && x.Likability >= 0 && x.Price < havemoney //桌宠不吃负面的食物
                 && !x.IsOverLoad() // 不吃超模食物
                );

                if (Core.Save.StrengthFood < 75)
                {
                    if (Core.Save.StrengthFood < 50)
                    {//太饿了,找正餐
                        food = food.FindAll(x => x.Type == Food.FoodType.Meal && x.StrengthFood > 20);
                    }
                    else
                    {//找零食
                        food = food.FindAll(x => x.Type == Food.FoodType.Snack && x.StrengthFood > 10);
                    }
                    if (food.Count == 0)
                        return;
                    var item = food[Function.Rnd.Next(food.Count)];
                    Core.Save.Money -= item.Price * 0.2;
                    TakeItem(item);
                    GameSavesData.Statistics[(gint)"stat_autobuy"]++;
                    Main.Display(item.GetGraph(), item.ImageSource, Main.DisplayToNomal);
                }
                else if (Core.Save.StrengthDrink < 75)
                {
                    food = food.FindAll(x => x.Type == Food.FoodType.Drink && x.StrengthDrink > 10);
                    if (food.Count == 0)
                        return;
                    var item = food[Function.Rnd.Next(food.Count)];
                    Core.Save.Money -= item.Price * 0.2;
                    TakeItem(item);
                    GameSavesData.Statistics[(gint)"stat_autobuy"]++;
                    Main.Display(item.GetGraph(), item.ImageSource, Main.DisplayToNomal);
                }
                else if (Set.AutoGift && Core.Save.Feeling < 50)
                {
                    food = food.FindAll(x => x.Type == Food.FoodType.Gift && x.Feeling > 10);
                    if (food.Count == 0)
                        return;
                    var item = food[Function.Rnd.Next(food.Count)];
                    Core.Save.Money -= item.Price * 0.2;
                    TakeItem(item);
                    GameSavesData.Statistics[(gint)"stat_autogift"]++;
                    Main.Display(item.GetGraph(), item.ImageSource, Main.DisplayToNomal);
                }
            }
            else if (Core.Save.Mode == GameSave.ModeType.Happy || Core.Save.Mode == GameSave.ModeType.Nomal)
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
        /// <summary>
        /// 使用/食用物品 (不包括显示动画)
        /// </summary>
        /// <param name="item">物品</param>
        public void TakeItem(Food item)
        {
            //获取吃腻时间
            Main.LastInteractionTime = DateTime.Now;
            DateTime now = DateTime.Now;
            DateTime eattime = GameSavesData["buytime"].GetDateTime(item.Name, now);
            double eattimes = 0;
            if (eattime > now)
            {
                eattimes = (eattime - now).TotalHours;
            }
            //开始加点
            Core.Save.EatFood(item, Math.Max(0.5, 1 - Math.Pow(eattimes, 2) * 0.01));
            //吃腻了
            eattimes += 2;
            GameSavesData["buytime"].SetDateTime(item.Name, now.AddHours(eattimes));
            //通知
            item.LoadEatTimeSource(this);
            item.NotifyOfPropertyChange("Description");

            Core.Save.Money -= item.Price;
            //统计
            GameSavesData.Statistics[(gint)"stat_buytimes"]++;
            GameSavesData.Statistics[(gint)("buy_" + item.Name)]++;
            GameSavesData.Statistics[(gdbe)"stat_betterbuy"] += item.Price;
            switch (item.Type)
            {
                case Food.FoodType.Food:
                    GameSavesData.Statistics[(gdbe)"stat_bb_food"] += item.Price;
                    break;
                case Food.FoodType.Drink:
                    GameSavesData.Statistics[(gdbe)"stat_bb_drink"] += item.Price;
                    break;
                case Food.FoodType.Drug:
                    GameSavesData.Statistics[(gdbe)"stat_bb_drug"] += item.Price;
                    GameSavesData.Statistics[(gdbe)"stat_bb_drug_exp"] += item.Exp;
                    break;
                case Food.FoodType.Snack:
                    GameSavesData.Statistics[(gdbe)"stat_bb_snack"] += item.Price;
                    break;
                case Food.FoodType.Functional:
                    GameSavesData.Statistics[(gdbe)"stat_bb_functional"] += item.Price;
                    break;
                case Food.FoodType.Meal:
                    GameSavesData.Statistics[(gdbe)"stat_bb_meal"] += item.Price;
                    break;
                case Food.FoodType.Gift:
                    GameSavesData.Statistics[(gdbe)"stat_bb_gift"] += item.Price;
                    GameSavesData.Statistics[(gdbe)"stat_bb_gift_like"] += item.Likability;
                    break;
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
                SteamUserStats.SetStat(name, (int)value);
            }
        }
        /// <summary>
        /// 计算统计数据
        /// </summary>
        private void StatisticsCalHandle()
        {
            var stat = GameSavesData.Statistics;
            var save = Core.Save;
            stat["stat_money"] = save.Money;
            stat["stat_level"] = save.Level;
            stat["stat_likability"] = save.Likability;

            stat[(gi64)"stat_total_time"] += (int)Set.LogicInterval;
            switch (Main.State)
            {
                case Main.WorkingState.Work:
                    if (Main.nowWork.Type == Work.WorkType.Work)
                        stat[(gi64)"stat_work_time"] += (int)Set.LogicInterval;
                    else
                        stat[(gi64)"stat_study_time"] += (int)Set.LogicInterval;
                    break;
                case Main.WorkingState.Sleep:
                    stat[(gi64)"stat_sleep_time"] += (int)Set.LogicInterval;
                    break;
            }
            if (save.Mode == GameSave.ModeType.Ill)
            {
                if (save.Money < 100)
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
                Task.Run(SteamUserStats.StoreStats);
            }
        }
        /// <summary>
        /// 加载游戏存档
        /// </summary>
        public bool SavesLoad(ILPS lps)
        {
            if (lps == null)
                return false;
            if (string.IsNullOrWhiteSpace(lps.ToString()))
                return false;
            GameSave_v2 tmp;
            if (GameSavesData != null)
                tmp = new GameSave_v2(lps, GameSavesData);
            else
            {
                var data = new LPS_D();
                foreach (var item in Set.PetData_OLD)
                {
                    if (item.Name.Contains("_"))
                    {
                        var strs = Sub.Split(item.Name, "_", 1);
                        data[strs[0]][(gstr)strs[1]] = item.Info;
                    }
                    else
                        data.Add(new Line(item.Name, item.Info));
                }
                tmp = new GameSave_v2(lps, Set.Statistics_OLD, olddata: data);
            }
            if (tmp.GameSave == null)
                return false;
            if (tmp.GameSave.Money == 0 && tmp.GameSave.Likability == 0 && tmp.GameSave.Exp == 0
                && tmp.GameSave.StrengthDrink == 0 && tmp.GameSave.StrengthFood == 0)//数据全是0,可能是bug
                return false;
            GameSavesData = tmp;
            Core.Save = tmp.GameSave;
            HashCheck = HashCheck;
            return true;
        }



        private void Handle_Steam(Main obj)
        {
            if (App.MainWindows.Count > 1)
            {
                if (App.MainWindows.FirstOrDefault() != this)
                {
                    return;
                }
                string str = "";
                int lv = 0;
                int workcount = 0;
                int sleepcount = 0;
                int musiccount = 0;
                int allcount = App.MainWindows.Count * 2 / 3;
                foreach (var item in App.MainWindows)
                {
                    str += item.GameSavesData.GameSave.Name + ",";
                    if (item.HashCheck)
                    {
                        lv += item.GameSavesData.GameSave.Level;
                    }
                    else
                        lv = int.MinValue;
                    switch (item.Main.State)
                    {
                        case Main.WorkingState.Work:
                            workcount++;
                            break;
                        case Main.WorkingState.Sleep:
                            sleepcount++;
                            break;
                        case Main.WorkingState.Nomal:
                            if (item.Main.DisplayType.Name == "music")
                                musiccount++;
                            break;
                    }
                }
                SteamFriends.SetRichPresence("usernames", str.Trim(','));
                if (lv > 0)
                {
                    SteamFriends.SetRichPresence("lv", $" (lv{lv}/{App.MainWindows.Count})");
                }
                else
                {
                    SteamFriends.SetRichPresence("lv", " ");
                }
                if (workcount > allcount)
                {
                    SteamFriends.SetRichPresence("steam_display", "#Status_MUTI_Work");
                }
                else if (sleepcount > allcount)
                {
                    SteamFriends.SetRichPresence("steam_display", "#Status_MUTI_Sleep");
                }
                else if (musiccount > allcount)
                {
                    SteamFriends.SetRichPresence("steam_display", "#Status_MUTI_Music");
                }
                else
                {
                    SteamFriends.SetRichPresence("steam_display", "#Status_MUTI_Play");
                }
            }
            else
            {
                if (HashCheck)
                {
                    SteamFriends.SetRichPresence("lv", $" (lv{GameSavesData.GameSave.Level})");
                }
                else
                {
                    SteamFriends.SetRichPresence("lv", " ");
                }
                if (Core.Save.Mode == GameSave.ModeType.Ill)
                {
                    SteamFriends.SetRichPresence("steam_display", "#Status_Ill");
                }
                else
                {
                    SteamFriends.SetRichPresence("mode", (Core.Save.Mode.ToString() + "ly").Translate());
                    switch (obj.State)
                    {
                        case Main.WorkingState.Work:
                            SteamFriends.SetRichPresence("work", obj.nowWork.Name.Translate());
                            SteamFriends.SetRichPresence("steam_display", "#Status_Work");
                            break;
                        case Main.WorkingState.Sleep:
                            SteamFriends.SetRichPresence("steam_display", "#Status_Sleep");
                            break;
                        default:
                            if (obj.DisplayType.Name == "music")
                                SteamFriends.SetRichPresence("steam_display", "#Status_Music");
                            else
                            {
                                switch (obj.DisplayType.Type)
                                {
                                    case GraphType.Move:
                                        SteamFriends.SetRichPresence("idel", "乱爬".Translate());
                                        break;
                                    case GraphType.Idel:
                                    case GraphType.StateONE:
                                    case GraphType.StateTWO:
                                        SteamFriends.SetRichPresence("idel", "发呆".Translate());
                                        break;
                                    default:
                                        SteamFriends.SetRichPresence("idel", "闲逛".Translate());
                                        break;
                                }
                                SteamFriends.SetRichPresence("steam_display", "#Status_IDLE");
                            }
                            break;
                    }
                }
            }
        }
        private bool? AudioPlayingVolumeOK = null;
        /// <summary>
        /// 获得当前系统音乐播放音量
        /// </summary>
        public float AudioPlayingVolume()
        {
            if (AudioPlayingVolumeOK == null)
            {//第一调用检查是否支持
                try
                {
                    float vol = 0;
                    using (var enumerator = new MMDeviceEnumerator())
                    {
                        using (var meter = AudioMeterInformation.FromDevice(enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia)))
                        {
                            vol = meter.GetPeakValue();
                            AudioPlayingVolumeOK = true;
                        }
                    }
                }
                catch
                {
                    AudioPlayingVolumeOK = false;
                }
            }
            else if (AudioPlayingVolumeOK == false)
            {
                return -1;
            }
            try
            {//后续容错可能是偶发性
                using (var enumerator = new MMDeviceEnumerator())
                {
                    using (var meter = AudioMeterInformation.FromDevice(enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia)))
                    {
                        return meter.GetPeakValue();
                    }
                }
            }
            catch
            {
                return -1;
            }
        }
        /// <summary>
        /// 音乐检测器
        /// </summary>
        private void Handle_Music(Main obj)
        {
            if (MusicTimer.Enabled == false && Core.Graph.FindGraphs("music", AnimatType.B_Loop, Core.Save.Mode) != null &&
                Main.IsIdel && AudioPlayingVolume() > Set.MusicCatch)
            {
                catch_MusicVolSum = 0;
                catch_MusicVolCount = 0;
                CurrMusicType = null;
                MusicTimer.Start();
                Task.Run(() =>
                {//等2秒看看识别结果
                    Thread.Sleep(2500);

                    if (CurrMusicType != null && Main.IsIdel)
                    {//识别通过,开始跑跳舞动画
                        //先统计下
                        GameSavesData.Statistics[(gint)"stat_music"]++;
                        Main.Display(Core.Graph.FindGraph("music", AnimatType.A_Start, Core.Save.Mode), Display_Music);
                    }
                    else
                    { //失败或有东西阻塞,停止检测
                        MusicTimer.Stop();
                    }
                });
            }
        }
        private void Display_Music()
        {
            if (CurrMusicType.HasValue)
            {
                if (CurrMusicType.Value)
                {//播放更刺激的
                    var mg = Core.Graph.FindGraph("music", AnimatType.Single, Core.Save.Mode);
                    mg ??= Core.Graph.FindGraph("music", AnimatType.B_Loop, Core.Save.Mode);
                    Main.Display(mg, Display_Music);
                }
                else
                {
                    Main.Display(Core.Graph.FindGraph("music", AnimatType.B_Loop, Core.Save.Mode), Display_Music);
                }
            }
            else
            {
                Main.Display("music", AnimatType.C_End, Main.DisplayToNomal);
            }
        }
        private void MusicTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!(Main.IsIdel || Main.DisplayType.Name == "music"))//不是音乐,被掐断
                return;
            catch_MusicVolSum += AudioPlayingVolume();
            catch_MusicVolCount++;
            if (catch_MusicVolCount >= 20)
            {
                double ans = catch_MusicVolSum / catch_MusicVolCount;
                catch_MusicVolSum /= 4;
                catch_MusicVolCount /= 4;
                if (ans > Set.MusicCatch)
                {
                    var bef = CurrMusicType;
                    CurrMusicType = ans > Set.MusicMax;
                    if (bef != null && bef != CurrMusicType)
                        Display_Music();
                    MusicTimer.Start();
                }
                else
                {
                    CurrMusicType = null;
                    if (Main.DisplayType.Name == "music")
                        Main.Display("music", AnimatType.C_End, Main.DisplayToNomal);
                }
            }
            else
            {
                MusicTimer.Start();
            }
        }

        public Timer MusicTimer;
        private double catch_MusicVolSum;
        private int catch_MusicVolCount;
        /// <summary>
        /// 当前音乐播放状态
        /// </summary>
        public bool? CurrMusicType { get; private set; }

        int LastDiagnosisTime = 0;

        /// <summary>
        /// 上传遥测文件
        /// </summary>
        public void DiagnosisUPLoad()
        {
            if (!IsSteamUser)
                return;//不遥测非Steam用户
            if (!Set.DiagnosisDayEnable)
                return;//不遥测不参加遥测的用户
            if (!Set.Diagnosis)
                return;//不遥测不参加遥测的用户
            if (!HashCheck)
                return;//不遥测数据修改过的用户
            if (LastDiagnosisTime++ < Set.DiagnosisInterval)
                return;//等待间隔
            LastDiagnosisTime = 0;
            string _url = "http://cn.exlb.org:5810/VPET/Report";
            //参数
            StringBuilder sb = new StringBuilder();
            sb.Append("action=data");
            sb.Append($"&steamid={SteamClient.SteamId.Value}");
            sb.Append($"&ver={version}");
            sb.Append("&save=");
            sb.AppendLine(HttpUtility.UrlEncode(Core.Save.ToLine().ToString() + Set.ToString()));
            //游戏设置比存档更重要,桌宠大部分内容存设置里了,所以一起上传
            var request = (HttpWebRequest)WebRequest.Create(_url);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";//ContentType
            byte[] byteData = Encoding.UTF8.GetBytes(sb.ToString());
            int length = byteData.Length;
            request.ContentLength = length;
            using (Stream writer = request.GetRequestStream())
            {
                writer.Write(byteData, 0, length);
                writer.Close();
                writer.Dispose();
            }
            string responseString;
            using (var response = (HttpWebResponse)request.GetResponse())
            {
                responseString = new StreamReader(response.GetResponseStream(), Encoding.UTF8).ReadToEnd();
                response.Dispose();
            }
            if (responseString == "IP times Max")
            {
                Set.DiagnosisDayEnable = false;
            }
#if DEBUG
            else
            {
                throw new Exception("诊断上传失败");
            }
#endif

        }
        /// <summary>
        /// 关闭指示器,默认为true
        /// </summary>
        public bool CloseConfirm { get; private set; } = true;

        public List<ITalkAPI> TalkAPI { get; } = new List<ITalkAPI>();
        /// <summary>
        /// 当前选择的对话框index
        /// </summary>
        public int TalkAPIIndex = -1;
        /// <summary>
        /// 当前对话框
        /// </summary>
        public ITalkAPI TalkBoxCurr
        {
            get
            {
                if (TalkAPIIndex == -1)
                    return null;
                return TalkAPI[TalkAPIIndex];
            }
        }
        /// <summary>
        /// 移除所有聊天对话框
        /// </summary>
        public void RemoveTalkBox()
        {
            if (TalkBox != null)
            {
                Main.ToolBar.MainGrid.Children.Remove(TalkBox);
                TalkBox = null;
            }
            if (TalkAPIIndex == -1)
                return;
            Main.ToolBar.MainGrid.Children.Remove(TalkAPI[TalkAPIIndex].This);
        }
        /// <summary>
        /// 加载自定义对话框
        /// </summary>
        public void LoadTalkDIY()
        {
            RemoveTalkBox();
            if (TalkAPIIndex == -1)
                return;
            Main.ToolBar.MainGrid.Children.Add(TalkAPI[TalkAPIIndex].This);
        }
        /// <summary>
        /// 超模工作检查
        /// </summary>
        public bool WorkCheck(Work work)
        {
            //看看是否超模
            if (HashCheck && work.IsOverLoad())
            {
                double spend = work.Spend();
                double get = work.Get();
                var rel = get / spend;
                if (MessageBoxX.Show("当前工作数据属性超模,是否继续工作?\n超模工作可能会导致游戏发生不可预料的错误\n超模工作不影响大部分成就解锁\n当前数据比率 {0:f2} 推荐=0.5<0.75\n盈利速度:{1:f0} 推荐<{2}"
                    .Translate(rel, get, (work.LevelLimit + 4) * 3), "超模工作提醒".Translate(), MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                {
                    return false;
                }
                HashCheck = false;
            }
            return true;
        }
        /// <summary>
        /// 游戏加载
        /// </summary>
        public void GameInitialization()
        {
            App.MainWindows.Add(this);
            try
            {
                //加载游戏设置
                if (new FileInfo(ExtensionValue.BaseDirectory + @$"\Setting{PrefixSave}.lps").Exists)
                {
                    Set = new Setting(File.ReadAllText(ExtensionValue.BaseDirectory + @$"\Setting{PrefixSave}.lps"));
                }
                else
                    Set = new Setting("Setting#VPET:|\n");

                var visualTree = new FrameworkElementFactory(typeof(Border));
                visualTree.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(BackgroundProperty));
                var childVisualTree = new FrameworkElementFactory(typeof(ContentPresenter));
                childVisualTree.SetValue(ClipToBoundsProperty, true);
                visualTree.AppendChild(childVisualTree);

                Template = new ControlTemplate
                {
                    TargetType = typeof(Window),
                    VisualTree = visualTree,
                };

                InitializeComponent();

                this.Height = 500 * Set.ZoomLevel;
                this.Width = 500 * Set.ZoomLevel;

                double L = 0, T = 0;
                if (Set.StartRecordLast)
                {
                    var point = Set.StartRecordLastPoint;
                    if (point.X != 0 || point.Y != 0)
                    {
                        L = point.X;
                        T = point.Y;
                    }
                }
                else
                {
                    var point = Set.StartRecordPoint;
                    L = point.X; T = point.Y;
                }

                Left = L;
                Top = T;

                // control position inside bounds
                Core.Controller = new MWController(this);
                double dist;
                if ((dist = Core.Controller.GetWindowsDistanceLeft()) < 0) Left -= dist;
                if ((dist = Core.Controller.GetWindowsDistanceRight()) < 0) Left += dist;
                if ((dist = Core.Controller.GetWindowsDistanceUp()) < 0) Top -= dist;
                if ((dist = Core.Controller.GetWindowsDistanceDown()) < 0) Top += dist;

                if (Set.TopMost)
                {
                    Topmost = true;
                }

                //不存在就关掉
                var modpath = new DirectoryInfo(ModPath + @"\0000_core\pet\vup");
                if (!modpath.Exists)
                {
                    MessageBoxX.Show("缺少模组Core,无法启动桌宠\nMissing module Core, can't start up", "启动错误 boot error", Panuon.WPF.UI.MessageBoxIcon.Error);
                    Close();
                    return;
                }

            }
            catch (Exception e)
            {
                string errstr = "游戏发生错误,可能是".Translate() + (string.IsNullOrWhiteSpace(CoreMOD.NowLoading) ?
              "游戏或者MOD".Translate() : $"MOD({CoreMOD.NowLoading})") +
              "导致的\n如有可能请发送 错误信息截图和引发错误之前的操作 给开发者:service@exlb.net\n感谢您对游戏开发的支持\n".Translate()
              + e.ToString();
                MessageBoxX.Show(errstr, "游戏致命性错误".Translate() + ' ' + "启动错误".Translate(), Panuon.WPF.UI.MessageBoxIcon.Error);
                Close();
            }
        }

        /// <summary>
        /// 支持多开的启动方式
        /// </summary>
        /// <param name="prefixsave">存档前缀</param>
        public MainWindow(string prefixsave)
        {
            PrefixSave = prefixsave;
            if (prefixsave != string.Empty && !PrefixSave.StartsWith("-"))
                PrefixSave = '-' + prefixsave;

            IsSteamUser = App.MainWindows[0].IsSteamUser;

            //处理ARGS
            Args = new LPS_D();
            foreach (var str in App.Args)
            {
                Args.Add(new Line(str));
            }
            _dwmEnabled = Win32.Dwmapi.DwmIsCompositionEnabled();
            _hwnd = new WindowInteropHelper(this).EnsureHandle();

            GameInitialization();

            //加载所有MOD
            List<DirectoryInfo> Path = new List<DirectoryInfo>();
            Path.AddRange(new DirectoryInfo(ModPath).EnumerateDirectories());

            var workshop = Set["workshop"];
            foreach (Sub ws in workshop)
            {
                Path.Add(new DirectoryInfo(ws.Name));
            }


            Task.Run(() => GameLoad(Path));
        }
        /// <summary>
        /// 加载游戏
        /// </summary>
        /// <param name="Path">MOD地址</param>
        public async void GameLoad(List<DirectoryInfo> Path)
        {

            Path = Path.Distinct().ToList();
            await Dispatcher.InvokeAsync(new Action(() => LoadingText.Content = "Loading MOD"));
            //加载mod
            foreach (DirectoryInfo di in Path)
            {
                if (!File.Exists(di.FullName + @"\info.lps"))
                    continue;
                await Dispatcher.InvokeAsync(new Action(() => LoadingText.Content = $"Loading MOD: {di.Name}"));
                CoreMODs.Add(new CoreMOD(di, this));
            }

            CoreMOD.NowLoading = null;

            //判断是否需要清空缓存
            if (App.MainWindows.Count == 1 && Set.LastCacheDate < CoreMODs.Max(x => x.CacheDate))
            {//需要清理缓存
                Set.LastCacheDate = DateTime.Now;
                if (Directory.Exists(GraphCore.CachePath))
                {
                    Directory.Delete(GraphCore.CachePath, true);
                    Directory.CreateDirectory(GraphCore.CachePath);
                }
            }


            await Dispatcher.InvokeAsync(new Action(() => LoadingText.Content = "尝试加载游戏MOD".Translate()));

            //当前桌宠动画
            var petloader = Pets.Find(x => x.Name == Set.PetGraph);
            petloader ??= Pets[0];

            await Dispatcher.InvokeAsync(new Action(() => LoadingText.Content = "尝试加载游戏存档".Translate()));
            //加载存档
            if (File.Exists(ExtensionValue.BaseDirectory + @"\Save.lps")) //有老的旧存档,优先旧存档
                try
                {
                    if (!SavesLoad(new LpsDocument(File.ReadAllText(ExtensionValue.BaseDirectory + @"\Save.lps"))))
                    {
                        //如果加载存档失败了,试试加载备份,如果没备份,就新建一个
                        LoadLatestSave(petloader.PetName);
                    }

                }
                catch (Exception ex)
                {
                    MessageBoxX.Show("存档损毁,无法加载该存档\n可能是数据溢出/超模导致的" + '\n' + ex.Message, "存档损毁".Translate());
                    //如果加载存档失败了,试试加载备份,如果没备份,就新建一个
                    LoadLatestSave(petloader.PetName);
                }
            else
                //如果加载存档失败了,试试加载备份,如果没备份,就新建一个
                LoadLatestSave(petloader.PetName);

            //加载数据合理化:食物
            if (!Set["gameconfig"].GetBool("noAutoCal"))
            {
                foreach (Food f in Foods)
                {
                    if (f.IsOverLoad())
                    {
                        f.Price = Math.Max((int)f.RealPrice, 1);
                        f.isoverload = false;
                    }
                }
                //var food = new Food();
                foreach (var selet in SelectTexts)
                {
                    selet.Exp = Math.Max(Math.Min(selet.Exp, 1000), -1000);
                    //food.Exp += selet.Exp;
                    selet.Feeling = Math.Max(Math.Min(selet.Feeling, 1000), -1000);
                    //food.Feeling += selet.Feeling;
                    selet.Health = Math.Max(Math.Min(selet.Feeling, 100), -100);
                    //food.Health += selet.Health;
                    selet.Likability = Math.Max(Math.Min(selet.Likability, 50), -50);
                    //food.Likability += selet.Likability;
                    selet.Money = Math.Max(Math.Min(selet.Money, 1000), -1000);
                    //food.Price -= selet.Money;
                    selet.Strength = Math.Max(Math.Min(selet.Strength, 1000), -1000);
                    //food.Strength += selet.Strength;
                    selet.StrengthDrink = Math.Max(Math.Min(selet.StrengthDrink, 1000), -1000);
                    //food.StrengthDrink += selet.StrengthDrink;
                    selet.StrengthFood = Math.Max(Math.Min(selet.StrengthFood, 1000), -1000);
                    //food.StrengthFood += selet.StrengthFood;
                }
                //if (food.IsOverLoad())
                //{
                //    MessageBox.Show(food.RealPrice.ToString());
                //}
                foreach (var selet in ClickTexts)
                {
                    selet.Exp = Math.Max(Math.Min(selet.Exp, 1000), -1000);
                    //food.Exp += selet.Exp;
                    selet.Feeling = Math.Max(Math.Min(selet.Feeling, 1000), -1000);
                    //food.Feeling += selet.Feeling;
                    selet.Health = Math.Max(Math.Min(selet.Feeling, 100), -100);
                    //food.Health += selet.Health;
                    selet.Likability = Math.Max(Math.Min(selet.Likability, 50), -50);
                    //food.Likability += selet.Likability;
                    selet.Money = Math.Max(Math.Min(selet.Money, 1000), -1000);
                    //food.Price -= selet.Money;
                    selet.Strength = Math.Max(Math.Min(selet.Strength, 1000), -1000);
                    //food.Strength += selet.Strength;
                    selet.StrengthDrink = Math.Max(Math.Min(selet.StrengthDrink, 1000), -1000);
                    //food.StrengthDrink += selet.StrengthDrink;
                    selet.StrengthFood = Math.Max(Math.Min(selet.StrengthFood, 1000), -1000);
                    //food.StrengthFood += selet.StrengthFood;
                }
            }

            //桌宠生日:第一次启动日期
            if (GameSavesData.Data.FindLine("birthday") == null)
            {
                var sf = new FileInfo(ExtensionValue.BaseDirectory + @$"\Setting{PrefixSave}.lps");
                if (sf.Exists)
                {
                    GameSavesData[(gdat)"birthday"] = sf.CreationTime.Date;
                }
                else
                    GameSavesData[(gdat)"birthday"] = DateTime.Now.Date;
            }

            AutoSaveTimer.Elapsed += AutoSaveTimer_Elapsed;

            if (GameSavesData.Statistics[(gdbe)"stat_bb_food"] < 0 || GameSavesData.Statistics[(gdbe)"stat_bb_drink"] < 0 || GameSavesData.Statistics[(gdbe)"stat_bb_drug"] < 0
                || GameSavesData.Statistics[(gdbe)"stat_bb_snack"] < 0 || GameSavesData.Statistics[(gdbe)"stat_bb_functional"] < 0 || GameSavesData.Statistics[(gdbe)"stat_bb_meal"] < 0
                || GameSavesData.Statistics[(gdbe)"stat_bb_gift"] < 0)
            {
                HashCheck = false;
            }

            if (Set.AutoSaveInterval > 0)
            {
                AutoSaveTimer.Interval = Set.AutoSaveInterval * 60000;
                AutoSaveTimer.Start();
            }
            ClickTexts.Add(new ClickText("你知道吗? 鼠标右键可以打开菜单栏"));
            ClickTexts.Add(new ClickText("你知道吗? 你可以在设置里面修改游戏的缩放比例"));
            ClickTexts.Add(new ClickText("想要宠物不乱动? 设置里可以设置智能移动或者关闭移动"));
            ClickTexts.Add(new ClickText("这游戏开发这么慢,都怪画师太咕了"));
            //ClickTexts.Add(new ClickText("有建议/游玩反馈? 来 菜单-系统-反馈中心 反馈吧"));
            ClickTexts.Add(new ClickText("长按脑袋拖动桌宠到你喜欢的任意位置"));

            ////临时聊天内容
            //ClickTexts.Add(new ClickText("主人，sbema秋季促销开始了哦，还有游戏大奖赛，快去给{name}去投一票吧。"));
            //ClickTexts.Add(new ClickText("主人主人，{name}参加了sbeam大奖赛哦，给人家投一票喵"));
            //ClickTexts.Add(new ClickText("那个。。主人。。\n人家参加了sbeam大奖赛哦。能不能。。给{name}投一票呢～"));
            //ClickTexts.Add(new ClickText("电脑里有一款《虚拟桌宠模拟器》的游戏正在参加2023的sbeam大奖赛，快来给桌宠投一票吧"));
            //"如果你觉得目前功能太少,那就多挂会机. 宠物会自己动的".Translate(),
            //"你知道吗? 你可以在设置里面修改游戏的缩放比例".Translate(),
            //"你现在乱点说话是说话系统的一部分,不过还没做,在做了在做了ing".Translate(),
            //"你添加了虚拟主播模拟器和虚拟桌宠模拟器到愿望单了吗? 快去加吧".Translate(),
            //"这游戏开发这么慢,都怪画师太咕了".Translate(),
            //"欢迎加入 虚拟主播模拟器群 430081239".Translate()

            await Dispatcher.InvokeAsync(new Action(() => LoadingText.Content = "尝试加载Steam内容".Translate()));
            //给正在玩这个游戏的主播/游戏up主做个小功能
            if (IsSteamUser)
            {
                ClickTexts.Add(new ClickText("关注 {0} 谢谢喵")
                {
                    TranslateText = "关注 {0} 谢谢喵".Translate(SteamClient.Name)
                });
                //Steam成就
                GameSavesData.Statistics.StatisticChanged += Statistics_StatisticChanged;
                //Steam通知
                SteamFriends.SetRichPresence("username", Core.Save.Name);
                SteamFriends.SetRichPresence("mode", (Core.Save.Mode.ToString() + "ly").Translate());
                SteamFriends.SetRichPresence("steam_display", "#Status_IDLE");
                SteamFriends.SetRichPresence("idel", "闲逛".Translate());
                if (HashCheck)
                {
                    SteamFriends.SetRichPresence("lv", $" (lv{GameSavesData.GameSave.Level})");
                }
                else
                {
                    SteamFriends.SetRichPresence("lv", " ");
                }
            }
            else
            {
                ClickTexts.Add(new ClickText("关注 {0} 谢谢喵")
                {
                    TranslateText = "关注 {0} 谢谢喵".Translate(Environment.UserName)
                });
            }

            //音乐识别timer加载
            MusicTimer = new System.Timers.Timer(100)
            {
                AutoReset = false
            };
            MusicTimer.Elapsed += MusicTimer_Elapsed;



            await Dispatcher.InvokeAsync(new Action(() => LoadingText.Content = "尝试加载游戏动画".Translate()));
            await Dispatcher.InvokeAsync(new Action(() =>
            {
                LoadingText.Content = "尝试加载动画和生成缓存".Translate();

                Core.Graph = petloader.Graph(Set.Resolution);
                Main = new Main(Core);
                Main.NoFunctionMOD = Set.CalFunState;

                //加载数据合理化:工作
                if (!Set["gameconfig"].GetBool("noAutoCal"))
                {
                    foreach (var work in Core.Graph.GraphConfig.Works)
                    {
                        if (work.IsOverLoad())
                        {
                            work.MoneyLevel = 0.5;
                            work.MoneyBase = 8;
                            if (work.Type == Work.WorkType.Work)
                            {
                                work.StrengthDrink = 2.5;
                                work.StrengthFood = 3.5;
                                work.Feeling = 1.5;
                                work.FinishBonus = 0;
                            }
                            else
                            {
                                work.Feeling = 1;
                                work.FinishBonus = 0;
                                work.StrengthDrink = 1;
                                work.StrengthFood = 1;
                            }
                        }
                    }
                }


                LoadingText.Content = "正在加载游戏".Translate();
                var m = new System.Windows.Controls.MenuItem()
                {
                    Header = "MOD管理".Translate(),
                    HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center,
                };
                m.Click += (x, y) =>
                {
                    Main.ToolBar.Visibility = Visibility.Collapsed;
                    winSetting.MainTab.SelectedIndex = 5;
                    winSetting.Show();
                };
                Main.FunctionSpendHandle += lowStrength;
                Main.WorkTimer.E_FinishWork += WorkTimer_E_FinishWork;
                Main.ToolBar.MenuMODConfig.Items.Add(m);
                try
                {
                    //加载游戏创意工坊插件
                    foreach (MainPlugin mp in Plugins)
                        mp.LoadPlugin();
                }
                catch (Exception e)
                {
                    new winReport(this, "由于插件引起的游戏启动错误".Translate() + "\n" + e.ToString()).Show();
                }
                Foods.ForEach(item => item.LoadImageSource(this));
                Main.TimeHandle += Handle_Music;
                if (IsSteamUser)
                    Main.TimeHandle += Handle_Steam;
                Main.TimeHandle += (x) => DiagnosisUPLoad();

                switch (Set["CGPT"][(gstr)"type"])
                {
                    case "DIY":
                        TalkAPIIndex = TalkAPI.FindIndex(x => x.APIName == Set["CGPT"][(gstr)"DIY"]);
                        LoadTalkDIY();
                        break;
                    //case "API":
                    //    TalkBox = new TalkBoxAPI(this);
                    //    Main.ToolBar.MainGrid.Children.Add(TalkBox);
                    //    break;
                    case "LB":
                        //if (IsSteamUser)
                        //{
                        //    TalkBox = new TalkSelect(this);
                        //    Main.ToolBar.MainGrid.Children.Add(TalkBox);
                        //}
                        TalkBox = new TalkSelect(this);
                        Main.ToolBar.MainGrid.Children.Add(TalkBox);
                        break;
                }

                //窗口部件
                winSetting = new winGameSetting(this);
                winBetterBuy = new winBetterBuy(this);

                Main.DefaultClickAction = () =>
                {
                    if (new TimeSpan(DateTime.Now.Ticks - lastclicktime).TotalSeconds > 20)
                    {
                        lastclicktime = DateTime.Now.Ticks;
                        var rt = GetClickText();
                        if (rt != null)
                        {
                            //聊天效果
                            if (rt.Exp != 0)
                            {
                                if (rt.Exp > 0)
                                {
                                    GameSavesData.Statistics[(gint)"stat_say_exp_p"]++;
                                }
                                else
                                    GameSavesData.Statistics[(gint)"stat_say_exp_d"]++;
                            }
                            if (rt.Likability != 0)
                            {
                                if (rt.Likability > 0)
                                    GameSavesData.Statistics[(gint)"stat_say_like_p"]++;
                                else
                                    GameSavesData.Statistics[(gint)"stat_say_like_d"]++;
                            }
                            if (rt.Money != 0)
                            {
                                if (rt.Money > 0)
                                    GameSavesData.Statistics[(gint)"stat_say_money_p"]++;
                                else
                                    GameSavesData.Statistics[(gint)"stat_say_money_d"]++;
                            }
                            Main.Core.Save.EatFood(rt);
                            Main.Core.Save.Money += rt.Money;
                            Main.SayRnd(rt.TranslateText, desc: rt.FoodToDescription());
                        }
                    }
                };
                Main.PlayVoiceVolume = Set.VoiceVolume;
                Main.FunctionSpendHandle += StatisticsCalHandle;
                DisplayGrid.Child = Main;
                Task.Run(async () =>
                {
                    while (Main.IsWorking)
                    {
                        Thread.Sleep(100);
                    }
                    await Dispatcher.InvokeAsync(() => LoadingText.Visibility = Visibility.Collapsed);
                });

                Main.ToolBar.AddMenuButton(ToolBar.MenuType.Setting, "退出桌宠".Translate(), () => { Main.ToolBar.Visibility = Visibility.Collapsed; Close(); });
                Main.ToolBar.AddMenuButton(ToolBar.MenuType.Setting, "开发控制台".Translate(), () => { Main.ToolBar.Visibility = Visibility.Collapsed; new winConsole(this).Show(); });
                Main.ToolBar.AddMenuButton(ToolBar.MenuType.Setting, "操作教程".Translate(), () =>
                {
                    if (LocalizeCore.CurrentCulture == "zh-Hans")
                        ExtensionSetting.StartURL(ExtensionValue.BaseDirectory + @"\Tutorial.html");
                    else if (LocalizeCore.CurrentCulture == "zh-Hant")
                        ExtensionSetting.StartURL(ExtensionValue.BaseDirectory + @"\Tutorial_zht.html");
                    else
                        ExtensionSetting.StartURL(ExtensionValue.BaseDirectory + @"\Tutorial_en.html");
                });
                Main.ToolBar.AddMenuButton(ToolBar.MenuType.Setting, "反馈中心".Translate(), () => { Main.ToolBar.Visibility = Visibility.Collapsed; new winReport(this).Show(); });
                Main.ToolBar.AddMenuButton(ToolBar.MenuType.Setting, "设置面板".Translate(), () =>
                {
                    Main.ToolBar.Visibility = Visibility.Collapsed;
                    winSetting.Show();
                });

                //this.Background = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/Res/TopLogo2019.PNG")));

                //Main.ToolBar.AddMenuButton(VPet_Simulator.Core.ToolBar.MenuType.Feed, "喂食测试", () =>
                //    {
                //        Main.ToolBar.Visibility = Visibility.Collapsed;
                //        IRunImage eat = (IRunImage)Core.Graph.FindGraph(GraphType.Eat, GameSave.ModeType.Nomal);
                //        var b = Main.FindDisplayBorder(eat);
                //        eat.Run(b, new BitmapImage(new Uri("pack://application:,,,/Res/汉堡.png")), Main.DisplayToNomal);
                //    }
                //);
                Main.ToolBar.AddMenuButton(ToolBar.MenuType.Feed, "吃饭".Translate(), () =>
                {
                    winBetterBuy.Show(Food.FoodType.Meal);
                });
                Main.ToolBar.AddMenuButton(ToolBar.MenuType.Feed, "喝水".Translate(), () =>
                {
                    winBetterBuy.Show(Food.FoodType.Drink);
                });
                Main.ToolBar.AddMenuButton(ToolBar.MenuType.Feed, "收藏".Translate(), () =>
                {
                    winBetterBuy.Show(Food.FoodType.Star);
                });
                Main.ToolBar.AddMenuButton(ToolBar.MenuType.Feed, "药品".Translate(), () =>
                {
                    winBetterBuy.Show(Food.FoodType.Drug);
                });
                Main.ToolBar.AddMenuButton(ToolBar.MenuType.Feed, "礼品".Translate(), () =>
                {
                    winBetterBuy.Show(Food.FoodType.Gift);
                });
                Main.SetMoveMode(Set.AllowMove, Set.SmartMove, Set.SmartMoveInterval * 1000);
                Main.SetLogicInterval((int)(Set.LogicInterval * 1000));
                if (Set.MessageBarOutside)
                    Main.MsgBar.SetPlaceOUT();

                Main.ToolBar.WorkCheck = WorkCheck;

                //加载图标
                notifyIcon = new NotifyIcon();
                notifyIcon.Text = "虚拟桌宠模拟器".Translate() + PrefixSave;
                ContextMenu m_menu;

                if (Set.PetHelper)
                    LoadPetHelper();



                m_menu = new ContextMenu();
                m_menu.Popup += (x, y) => { GameSavesData.Statistics[(gint)"stat_menu_pop"]++; };
                var hitThrough = new MenuItem("鼠标穿透".Translate(), (x, y) => { SetTransparentHitThrough(); })
                {
                    Name = "NotifyIcon_HitThrough",
                    Checked = HitThrough
                };
                m_menu.MenuItems.Add(hitThrough);
                m_menu.MenuItems.Add(new MenuItem("操作教程".Translate(), (x, y) =>
                {
                    if (LocalizeCore.CurrentCulture == "zh-Hans")
                        ExtensionSetting.StartURL(ExtensionValue.BaseDirectory + @"\Tutorial.html");
                    else if (LocalizeCore.CurrentCulture == "zh-Hant")
                        ExtensionSetting.StartURL(ExtensionValue.BaseDirectory + @"\Tutorial_zht.html");
                    else
                        ExtensionSetting.StartURL(ExtensionValue.BaseDirectory + @"\Tutorial_en.html");
                }));
                m_menu.MenuItems.Add(new MenuItem("重置位置与状态".Translate(), (x, y) =>
                {
                    Main.CleanState();
                    Main.DisplayToNomal();
                    Left = (SystemParameters.PrimaryScreenWidth - Width) / 2;
                    Top = (SystemParameters.PrimaryScreenHeight - Height) / 2;
                }));
                m_menu.MenuItems.Add(new MenuItem("反馈中心".Translate(), (x, y) => { new winReport(this).Show(); }));
                m_menu.MenuItems.Add(new MenuItem("开发控制台".Translate(), (x, y) => { new winConsole(this).Show(); }));

                m_menu.MenuItems.Add(new MenuItem("设置面板".Translate(), (x, y) =>
                {
                    winSetting.Show();
                }));
                m_menu.MenuItems.Add(new MenuItem("退出桌宠".Translate(), (x, y) => Close()));

                LoadDIY();

                notifyIcon.ContextMenu = m_menu;

                notifyIcon.Icon = new System.Drawing.Icon(Application.GetResourceStream(new Uri("pack://application:,,,/vpeticon.ico")).Stream);

                notifyIcon.Visible = true;
                notifyIcon.BalloonTipClicked += (a, b) =>
                {
                    winSetting.Show();
                };
                if (Set.StartUPBoot == true && !Set["v"][(gbol)"newverstartup"])
                {//更新到最新版开机启动方式
                    try
                    {
                        winSetting.GenStartUP();
                        Set["v"][(gbol)"newverstartup"] = true;
                    }
                    catch
                    {

                    }
                }


                //成就和统计 
                GameSavesData.Statistics[(gint)"stat_open_times"]++;
                Main.MoveTimer.Elapsed += MoveTimer_Elapsed;
                Main.OnSay += Main_OnSay;
                Main.Event_TouchHead += Main_Event_TouchHead;
                Main.Event_TouchBody += Main_Event_TouchBody;

                HashCheck = HashCheck;

                //添加捏脸动画(若有)
                if (Core.Graph.GraphConfig.Data.ContainsLine("pinch"))
                {
                    var pin = Core.Graph.GraphConfig.Data["pinch"];
                    Main.Core.TouchEvent.Insert(0, new TouchArea(
                        new Point(pin[(gdbe)"px"], pin[(gdbe)"py"]), new Size(pin[(gdbe)"sw"], pin[(gdbe)"sh"])
                        , DisplayPinch, true));
                }


                if (Set.HitThrough)
                {
                    if (!Set["v"][(gbol)"HitThrough"])
                    {
                        Set["v"][(gbol)"HitThrough"] = true;
                        Set.HitThrough = false;
                    }
                    else
                        SetTransparentHitThrough();
                }

                if (File.Exists(ExtensionValue.BaseDirectory + @"\Tutorial.html") && Set["SingleTips"].GetDateTime("tutorial") <= new DateTime(2023, 10, 20) && App.MainWindows.Count == 1)
                {
                    Set["SingleTips"].SetDateTime("tutorial", DateTime.Now);
                    if (LocalizeCore.CurrentCulture == "zh-Hans")
                        ExtensionSetting.StartURL(ExtensionValue.BaseDirectory + @"\Tutorial.html");
                    else if (LocalizeCore.CurrentCulture == "zh-Hant")
                        ExtensionSetting.StartURL(ExtensionValue.BaseDirectory + @"\Tutorial_zht.html");
                    else
                        ExtensionSetting.StartURL(ExtensionValue.BaseDirectory + @"\Tutorial_en.html");
                }
                if (!Set["SingleTips"].GetBool("helloworld"))
                {
                    Task.Run(() =>
                    {
                        Thread.Sleep(2000);
                        Set["SingleTips"].SetBool("helloworld", true);
                        NoticeBox.Show("欢迎使用虚拟桌宠模拟器!\n如果遇到桌宠爬不见了,可以在我这里设置居中或退出桌宠".Translate(),
                           "你好".Translate() + (IsSteamUser ? SteamClient.Name : Environment.UserName));
                        //Thread.Sleep(2000);
                        //Main.SayRnd("欢迎使用虚拟桌宠模拟器\n这是个中期的测试版,若有bug请多多包涵\n欢迎加群虚拟主播模拟器430081239或在菜单栏-管理-反馈中提交bug或建议".Translate());
                    });
                }
#if DEMO
                else
                {
                    notifyIcon.ShowBalloonTip(10, "正式版更新通知".Translate(), //本次更新内容
                        "虚拟桌宠模拟器 现已发布正式版, 赶快前往下载吧!", ToolTipIcon.Info);
                    Process.Start("https://store.steampowered.com/app/1920960/VPet/");
                }
#else
                //else if (Set["SingleTips"].GetDateTime("update") <= new DateTime(2023, 8, 11) && LocalizeCore.CurrentCulture.StartsWith("cn"))
                //{
                //    if (Set["SingleTips"].GetDateTime("update") > new DateTime(2023, 8, 1)) // 上次更新日期时间
                //        notifyIcon.ShowBalloonTip(10, "更新通知 08/11", //本次更新内容
                //        "新增跳舞功能,桌宠会在播放音乐的时候跳舞\n新增不开心大部分系列动画\n更好买支持翻页", ToolTipIcon.Info);
                //    else// 累计更新内容
                //        notifyIcon.ShowBalloonTip(10, "更新通知 08/01",
                //    "更新了新的动画系统\n新增桌宠会在播放音乐的时候跳舞\n新增不开心大部分系列动画\n更好买支持翻页", ToolTipIcon.Info);
                //    Set["SingleTips"].SetDateTime("update", DateTime.Now);
                //}
#endif
                //MOD报错
                foreach (CoreMOD cm in CoreMODs)
                    if (!cm.SuccessLoad)
                        if (cm.Tag.Contains("该模组已损坏"))
                            MessageBoxX.Show("模组 {0} 插件损坏\n虚拟桌宠模拟器未能成功加载该插件\n请联系作者修复该问题".Translate(cm.Name) + '\n' + cm.ErrorMessage, "该模组已损坏".Translate());
                        else if (Set.IsPassMOD(cm.Name))
                            MessageBoxX.Show("模组 {0} 的代码插件损坏\n虚拟桌宠模拟器未能成功加载该插件\n请联系作者修复该问题".Translate(cm.Name) + '\n' + cm.ErrorMessage, "{0} 未加载代码插件".Translate(cm.Name));
                        else if (Set.IsMSGMOD(cm.Name))
                            MessageBoxX.Show("由于 {0} 包含代码插件\n虚拟桌宠模拟器已自动停止加载该插件\n请手动前往设置允许启用该mod 代码插件".Translate(cm.Name), "{0} 未加载代码插件".Translate(cm.Name));

            }));

            ////游戏提示
            //if (Set["SingleTips"][(gint)"open"] == 0 && Set.StartUPBoot == true && Set.StartUPBootSteam == true)
            //{
            //    await Dispatcher.InvokeAsync(new Action(() =>
            //    {
            //        MessageBoxX.Show("检测到您开启了开机启动, 以下是开机启动相关提示信息: (仅显示一次)".Translate() + "\n------\n" +
            //             "游戏开机启动的实现方式是创建快捷方式,不是注册表,更健康,所以游戏卸了也不知道\n如果游戏打不开,可以去这里手动删除游戏开机启动快捷方式:\n%appdata%\\Microsoft\\Windows\\Start Menu\\Programs\\Startup\\".Translate()
            //          , "关于卸载不掉的问题是因为开启了开机启动".Translate(), Panuon.WPF.UI.MessageBoxIcon.Info);
            //        Set["SingleTips"][(gint)"open"] = 1;
            //    }));
            //}

        }
        /// <summary>
        /// 显示捏脸情况
        /// </summary>
        public bool DisplayPinch()
        {
            if (Core.Graph.FindGraphs("pinch", AnimatType.A_Start, Core.Save.Mode) == null)
            {
                return false;
            }
            Main.CountNomal = 0;

            if (Core.Controller.EnableFunction && Core.Save.Strength >= 10 && Core.Save.Feeling < 100)
            {
                Core.Save.StrengthChange(-2);
                Core.Save.FeelingChange(1);
                Core.Save.Mode = Core.Save.CalMode();
                Main.LabelDisplayShowChangeNumber(LocalizeCore.Translate("体力-{0:f0} 心情+{1:f0}"), 2, 1);
            }
            if (Main.DisplayType.Name == "pinch")
            {
                if (Main.DisplayType.Animat == AnimatType.A_Start)
                    return false;
                else if (Main.DisplayType.Animat == AnimatType.B_Loop)
                    if (Dispatcher.Invoke(() => Main.PetGrid.Tag) is IGraph ig && ig.GraphInfo.Name == "pinch" && ig.GraphInfo.Animat == AnimatType.B_Loop)
                    {
                        ig.IsContinue = true;
                        return true;
                    }
                    else if (Dispatcher.Invoke(() => Main.PetGrid2.Tag) is IGraph ig2 && ig2.GraphInfo.Name == "pinch" && ig2.GraphInfo.Animat == AnimatType.B_Loop)
                    {
                        ig2.IsContinue = true;
                        return true;
                    }
            }
            Main_Event_TouchHead();
            Main_Event_TouchBody();
            Main.Display("pinch", AnimatType.A_Start, () =>
               Main.Display("pinch", AnimatType.B_Loop, DisplayPinch_loop));
            return true;
        }
        private void DisplayPinch_loop()
        {
            if (Main.isPress && Main.DisplayType.Name == "pinch" && Main.DisplayType.Animat == AnimatType.B_Loop)
            {
                if (Core.Controller.EnableFunction && Core.Save.Strength >= 10 && Core.Save.Feeling < 100)
                {
                    Core.Save.StrengthChange(-2);
                    Core.Save.FeelingChange(1);
                    Core.Save.Mode = Core.Save.CalMode();
                    Main.LabelDisplayShowChangeNumber(LocalizeCore.Translate("体力-{0:f0} 心情+{1:f0}"), 2, 1);
                }
                Main.Display("pinch", AnimatType.B_Loop, DisplayPinch_loop);
            }
            else
            {
                Main.DisplayCEndtoNomal("pinch");
            }
        }
    }
}
