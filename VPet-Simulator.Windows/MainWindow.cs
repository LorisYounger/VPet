using CSCore.CoreAudioAPI;
using LinePutScript;
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
using System.Windows.Media.Imaging;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;
using static VPet_Simulator.Core.GraphHelper;
using static VPet_Simulator.Core.GraphInfo;
using Timer = System.Timers.Timer;
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
        public int version { get; } = 105;
        /// <summary>
        /// 版本号
        /// </summary>
        public string Version => $"{version / 100}.{version % 100}";

        public List<LowText> LowFoodText { get; set; } = new List<LowText>();

        public List<LowText> LowDrinkText { get; set; } = new List<LowText>();

        public List<SelectText> SelectTexts { get; set; } = new List<SelectText>();

        public List<ClickText> ClickTexts { get; set; } = new List<ClickText>();
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
                    mt = ClickText.ModeType.PoorCondition;
                    break;
                default:
                case GameSave.ModeType.Nomal:
                    mt = ClickText.ModeType.Nomal;
                    break;
                case GameSave.ModeType.Happy:
                    mt = ClickText.ModeType.Happy;
                    break;
                case GameSave.ModeType.Ill:
                    mt = ClickText.ModeType.Ill;
                    break;
            }
            var list = ClickTexts.FindAll(x => x.DaiTime.HasFlag(dt) && x.Mode.HasFlag(mt) && x.CheckState(Main));
            if (list.Count == 0)
                return null;
            return list[Function.Rnd.Next(list.Count)];
        }
        private Image hashcheckimg;
        public void HashCheckOff()
        {
            HashCheck = false;
        }
        /// <summary>
        /// 存档 Hash检查 是否通过
        /// </summary>
        public bool HashCheck
        {
            get => hashCheck;
            set
            {
                hashCheck = value;
                Main?.Dispatcher.Invoke(() =>
                {
                    if (hashCheck)
                    {
                        if (hashcheckimg == null)
                        {
                            hashcheckimg = new Image();
                            hashcheckimg.Source = new BitmapImage(new Uri("pack://application:,,,/Res/hash.png"));
                            hashcheckimg.HorizontalAlignment = HorizontalAlignment.Right;
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
                Set.StartRecordLastPoint = new Point(Left, Top);
                File.WriteAllText(ExtensionValue.BaseDirectory + @"\Setting.lps", Set.ToString());

                if (!Directory.Exists(ExtensionValue.BaseDirectory + @"\BackUP"))
                    Directory.CreateDirectory(ExtensionValue.BaseDirectory + @"\BackUP");

                if (Core != null && Core.Save != null)
                {
                    var ds = new List<string>(Directory.GetFiles(ExtensionValue.BaseDirectory + @"\BackUP", "*.lps")).FindAll(x => x.Contains('_')).OrderBy(x =>
                    {
                        if (int.TryParse(x.Split('_')[1], out int i))
                            return i;
                        return 0;
                    }).ToList();
                    while (ds.Count > Set.BackupSaveMaxNum)
                    {
                        File.Delete(ds[0]);
                        ds.RemoveAt(0);
                    }
                    if (File.Exists(ExtensionValue.BaseDirectory + $"\\BackUP\\Save_{st}.lps"))
                        File.Delete(ExtensionValue.BaseDirectory + $"\\BackUP\\Save_{st}.lps");

                    if (File.Exists(ExtensionValue.BaseDirectory + @"\Save.lps"))
                        File.Move(ExtensionValue.BaseDirectory + @"\Save.lps", ExtensionValue.BaseDirectory + $"\\BackUP\\Save_{st}.lps");
                    var l = Core.Save.ToLine();
                    if (HashCheck)
                    {
                        l[(gi64)"hash"] = new Line(l.ToString()).GetLongHashCode();
                    }
                    else
                        l[(gint)"hash"] = -1;
                    File.WriteAllText(ExtensionValue.BaseDirectory + @"\Save.lps", l.ToString());
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
                try
                {
                    System.Windows.Forms.SendKeys.SendWait(content);
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
                var havemoney = Core.Save.Money * 1.2;
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
            DateTime now = DateTime.Now;
            DateTime eattime = Set.PetData.GetDateTime("buytime_" + item.Name, now);
            double eattimes = 0;
            if (eattime > now)
            {
                eattimes = (eattime - now).TotalHours;
            }
            //开始加点
            Core.Save.EatFood(item, Math.Max(0.5, 1 - Math.Pow(eattimes, 2) * 0.01));
            //吃腻了
            eattimes += 2;
            Set.PetData.SetDateTime("buytime_" + item.Name, now.AddHours(eattimes));
            //通知
            item.LoadEatTimeSource(this);
            item.NotifyOfPropertyChange("Description");

            Core.Save.Money -= item.Price;
            //统计
            Set.Statistics[(gint)("buy_" + item.Name)]++;
            Set.Statistics[(gdbe)"stat_betterbuy"] += item.Price;
            switch (item.Type)
            {
                case Food.FoodType.Food:
                    Set.Statistics[(gdbe)"stat_bb_food"] += item.Price;
                    break;
                case Food.FoodType.Drink:
                    Set.Statistics[(gdbe)"stat_bb_drink"] += item.Price;
                    break;
                case Food.FoodType.Drug:
                    Set.Statistics[(gdbe)"stat_bb_drug"] += item.Price;
                    break;
                case Food.FoodType.Snack:
                    Set.Statistics[(gdbe)"stat_bb_snack"] += item.Price;
                    break;
                case Food.FoodType.Functional:
                    Set.Statistics[(gdbe)"stat_bb_functional"] += item.Price;
                    break;
                case Food.FoodType.Meal:
                    Set.Statistics[(gdbe)"stat_bb_meal"] += item.Price;
                    break;
                case Food.FoodType.Gift:
                    Set.Statistics[(gdbe)"stat_bb_gift"] += item.Price;
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
                    if (Main.nowWork.Type == GraphHelper.Work.WorkType.Work)
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
                Task.Run(Steamworks.SteamUserStats.StoreStats);
            }
        }
        /// <summary>
        /// 加载游戏
        /// </summary>
        public bool GameLoad(ILine line)
        {
            if (line == null)
                return false;
            if (string.IsNullOrWhiteSpace(line.ToString()))
                return false;

            Core.Save = GameSave.Load(line);

            if (Core.Save.Money == 0 && Core.Save.Likability == 0 && Core.Save.Exp == 0
                && Core.Save.StrengthDrink == 0 && Core.Save.StrengthFood == 0)//数据全是0,可能是bug
                return false;
            long hash = line.GetInt64("hash");
            if (line.Remove("hash"))
            {
                HashCheck = line.GetLongHashCode() == hash;
            }
            return true;
        }
        private void Handle_Steam(Main obj)
        {
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
                            SteamFriends.SetRichPresence("steam_display", "#Status_IDLE");
                        break;
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
        private bool hashCheck = true;

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
            sb.Append($"&steamid={Steamworks.SteamClient.SteamId.Value}");
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
                var spend = (Math.Pow(work.StrengthFood * 2 + 1, 2) / 6 + Math.Pow(work.StrengthDrink * 2 + 1, 2) / 9 +
               Math.Pow(work.Feeling * 2 + 1, 2) / 12) * (Math.Pow(work.LevelLimit / 2 + 1, 0.5) / 4 + 1) - 0.5;
                var get = (work.MoneyBase + work.MoneyLevel * 10) * (work.MoneyLevel + 1) * (1 + work.FinishBonus / 2);
                if (work.Type != Work.WorkType.Work)
                {
                    get /= 12;//经验值换算
                }
                var rel = get / spend;
                if (MessageBoxX.Show("当前工作数据属性超模,是否继续工作?\n超模工作可能会导致游戏发生不可预料的错误\n超模工作不影响大部分成就解锁\n当前数据比率 {0:f2}\n推荐比率<1.5"
                    .Translate(rel), "超模工作提醒".Translate(), MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                {
                    return false;
                }
                HashCheck = false;
            }
            return true;
        }

    }
}
