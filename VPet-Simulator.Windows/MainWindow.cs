using LinePutScript;
using LinePutScript.Dictionary;
using LinePutScript.Localization.WPF;
using NAudio.CoreAudioApi;
using Panuon.WPF.UI;
using Panuon.WPF.UI.Configurations;
using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.Data;
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
using System.Windows.Media;
using System.Windows.Threading;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;
using static VPet_Simulator.Core.GraphHelper;
using static VPet_Simulator.Core.GraphInfo;
using static VPet_Simulator.Windows.Interface.ExtensionFunction;
using static VPet_Simulator.Windows.Interface.Food;
using Application = System.Windows.Application;
using Color = System.Windows.Media.Color;
using ContextMenu = System.Windows.Forms.ContextMenuStrip;
using Image = System.Windows.Controls.Image;
using Line = LinePutScript.Line;
using MenuItem = System.Windows.Forms.ToolStripMenuItem;
using MessageBox = System.Windows.MessageBox;
using Timer = System.Timers.Timer;
using ToolBar = VPet_Simulator.Core.ToolBar;

namespace VPet_Simulator.Windows
{
    public partial class MainWindow : IMainWindow
    {

        /// <summary>
        /// 加载主题
        /// </summary>
        /// <param name="themename">主题名称</param>
        public void LoadTheme(string themename)
        {
            Theme ctheme = Themes.Find(x => x.xName == themename);
            if (ctheme == null)
            {
                return;
            }
            Theme = ctheme;

            //加载图片包
            ImageSources.AddSources(ctheme.Images);

            //阴影颜色
            Application.Current.Resources["ShadowColor"] = Function.HEXToColor('#' + ctheme.ThemeColor[(gstr)"ShadowColor"]);

            foreach (ILine lin in ctheme.ThemeColor.Assemblage.FindAll(x => !x.Name.Contains("Color")))
                Application.Current.Resources[lin.Name] = new SolidColorBrush(Function.HEXToColor('#' + lin.info));

            //系统生成部分颜色
            Color c = Function.HEXToColor('#' + ctheme.ThemeColor["Primary"].info);
            c.A = 204;
            Application.Current.Resources["PrimaryTrans"] = new SolidColorBrush(c);
            c.A = 44;
            Application.Current.Resources["PrimaryTrans4"] = new SolidColorBrush(c);
            c.A = 170;
            Application.Current.Resources["PrimaryTransA"] = new SolidColorBrush(c);
            c.A = 238;
            Application.Current.Resources["PrimaryTransE"] = new SolidColorBrush(c);

            c = Function.HEXToColor('#' + ctheme.ThemeColor["Secondary"].info);
            c.A = 204;
            Application.Current.Resources["SecondaryTrans"] = new SolidColorBrush(c);
            c.A = 44;
            Application.Current.Resources["SecondaryTrans4"] = new SolidColorBrush(c);
            c.A = 170;
            Application.Current.Resources["SecondaryTransA"] = new SolidColorBrush(c);
            c.A = 238;
            Application.Current.Resources["SecondaryTransE"] = new SolidColorBrush(c);


            c = Function.HEXToColor('#' + ctheme.ThemeColor["DARKPrimary"].info);
            c.A = 204;
            Application.Current.Resources["DARKPrimaryTrans"] = new SolidColorBrush(c);
            c.A = 44;
            Application.Current.Resources["DARKPrimaryTrans4"] = new SolidColorBrush(c);
            c.A = 170;
            Application.Current.Resources["DARKPrimaryTransA"] = new SolidColorBrush(c);
            c.A = 238;
            Application.Current.Resources["DARKPrimaryTransE"] = new SolidColorBrush(c);
        }

        public void LoadFont(string fontname)
        {
            IFont cfont = Fonts.Find(x => x.Name == fontname);
            if (cfont == null)
            {
                return;
            }
            var font = cfont.Font;
            Application.Current.Resources["MainFont"] = font;
            Panuon.WPF.UI.GlobalSettings.Setting.FontFamily = font;
        }


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
                case IGameSave.ModeType.PoorCondition:
                    mt = ClickText.ModeType.PoorCondition;
                    break;
                default:
                case IGameSave.ModeType.Nomal:
                    mt = ClickText.ModeType.Nomal;
                    break;
                case IGameSave.ModeType.Happy:
                    mt = ClickText.ModeType.Happy;
                    break;
                case IGameSave.ModeType.Ill:
                    mt = ClickText.ModeType.Ill;
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
            MGrid.Width = 500 * zl;
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
            //保存日程表
            ScheduleTask?.Save();
            try
            {
                //保存插件
                foreach (MainPlugin mp in Plugins)
                    mp.Save();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), "由于插件引起的保存错误".Translate());
            }
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
                if (PrefixSave == "" && File.Exists(ExtensionValue.BaseDirectory + @"\Setting.lps"))
                {//对于主设置的备份
                    if (new FileInfo(ExtensionValue.BaseDirectory + @"\Setting.lps").Length < 10)
                    {//文件大小小于10字节,可能是损坏的文件
                        File.Delete(ExtensionValue.BaseDirectory + @"\Setting.lps");
                    }
                    else
                    {
                        if (File.Exists(ExtensionValue.BaseDirectory + @"\Setting.bkp"))
                            File.Delete(ExtensionValue.BaseDirectory + @"\Setting.bkp");
                        File.Move(ExtensionValue.BaseDirectory + @"\Setting.lps", ExtensionValue.BaseDirectory + @"\Setting.bkp");
                    }

                }
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
                                new MainWindow(win, this).Show();
                            }
                            menuItem.Items.Remove(s);
                        };
                        menuItem.Items.Add(mo);
                    }
                    Main.ToolBar.MenuDIY.Items.Add(menuItem);
                }
            }

            foreach (ISub sub in Set["diy"])
                Main.ToolBar.AddMenuButton(ToolBar.MenuType.DIY, sub.Name, () =>
                {
                    Main.ToolBar.Visibility = Visibility.Collapsed;
                    RunDIY(sub.Info);
                });

            //加载游戏创意工坊插件
            foreach (MainPlugin mp in Plugins)
                try//不要识图用!DEBUG去掉try, 不在主线程导致错误显示不出来的
                {
                    mp.LoadDIY();
                }
                catch (Exception e)
                {
                    MessageBoxX.Show(e.ToString(), "由于插件引起的自定按钮加载错误".Translate() + '-' + mp.PluginName);
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

        public void RunDIY(string content)
        {
            if (content.Contains(@":\"))
            {
                try
                {
                    if (!Set["v"][(gbol)"rundiy"])
                    {
                        MessageBoxX.Show("由于操作系统的设计，通过我们软件启动的程序可能会在任务管理器中归类为我们软件的子进程，这可能导致CPU/内存占用显示较高".Translate(),
                            "关于CPU/内存占用显示较高的一次性提示".Translate());
                        Set["v"][(gbol)"rundiy"] = true;
                    }
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = content;
                    startInfo.UseShellExecute = false;
                    Process.Start(startInfo);
                }
                catch
                {
                    try
                    {
                        try
                        {
                            Process.Start(content);
                        }
                        catch
                        {
                            var psi = new ProcessStartInfo
                            {
                                FileName = content,
                                UseShellExecute = true
                            };
                            Process.Start(psi);
                        }
                    }
                    catch (Exception e)
                    {
                        MessageBoxX.Show("快捷键运行失败:无法运行指定内容".Translate() + '\n' + e.Message);
                    }
                }
            }
            else if (content.Contains("://"))
            {
                try
                {
                    ExtensionFunction.StartURL(content);
                }
                catch (Exception e)
                {
                    MessageBoxX.Show("快捷键运行失败:无法运行指定内容".Translate() + '\n' + e.Message);
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
                    MessageBoxX.Show("快捷键运行失败:无法运行指定内容".Translate() + '\n' + e.Message);
                }
            }
        }

        public void ShowSetting(int page = -1)
        {
            if (page >= 0 && page <= 6)
                winSetting.MainTab.SelectedIndex = page;
            winSetting.Show();
        }
        public void ShowWorkMenu(Work.WorkType type)
        {
            if (winWorkMenu == null)
            {
                winWorkMenu = new winWorkMenu(this, type);
                winWorkMenu.Show();
            }
            else
            {
                winWorkMenu.LsbCategory.SelectedIndex = (int)type;
                winWorkMenu.Focus();
                winWorkMenu.Topmost = true;
            }
        }
        public void ShowBetterBuy(Food.FoodType type)
        {
            winBetterBuy.Show(type);
        }
        public void ShowGallery()
        {
            if (winGallery != null)
            {
                winGallery.Show();
                winGallery.Focus();
            }
            else
            {
                winGallery = new winGallery(this);
                winGallery.Show();
            }
        }
        int lowstrengthAskCountFood = 20;
        int lowstrengthAskCountDrink = 20;
        private void lowStrength()
        {
            var sm = Core.Save.StrengthMax;
            var sm75 = sm * 0.70;
            if (Set.AutoBuy && Core.Save.Money >= 100)
            {
                var havemoney = Core.Save.Money * 0.8;
                List<Food> food = Foods.FindAll(x => x.Price >= 2 && x.Health >= -5 && x.Exp >= -10 && x.Likability >= 0 && x.Price < havemoney //桌宠不吃负面的食物
                 && !x.IsOverLoad() // 不吃超模食物
                );

                if ((Core.Save.StrengthFood + Core.Save.StoreStrengthFood) < sm75)
                {//饿了就该吃正餐
                    food = food.FindAll(x => x.Type == Food.FoodType.Meal && x.StrengthFood > Math.Min(sm * 0.20, 100));
                    if (food.Count == 0)
                        return;
                    var item = food[Function.Rnd.Next(food.Count)];
                    Core.Save.Money -= item.Price * 0.2;
                    TakeItem(item);
                    GameSavesData.Statistics[(gint)"stat_autobuy"]++;
                    Main.Display(item.GetGraph(), item.ImageSource, Main.DisplayToNomal);
                }
                else if ((Core.Save.StrengthDrink + Core.Save.StoreStrengthDrink) < sm75)
                {
                    food = food.FindAll(x => x.Type == Food.FoodType.Drink && x.StrengthDrink > Math.Min(sm * 0.20, 50));
                    if (food.Count == 0)
                        return;
                    var item = food[Function.Rnd.Next(food.Count)];
                    Core.Save.Money -= item.Price * 0.2;
                    TakeItem(item);
                    GameSavesData.Statistics[(gint)"stat_autobuy"]++;
                    Main.Display(item.GetGraph(), item.ImageSource, Main.DisplayToNomal);
                }
                else if (Core.Save.Feeling < Core.Save.FeelingMax * 0.50)
                {
                    if (Set.AutoGift)
                    {
                        food = food.FindAll(x => x.Type == Food.FoodType.Gift && x.Feeling > Math.Min(Core.Save.FeelingMax * 0.10, 50));
                        if (food.Count == 0)
                            return;
                    }
                    else // 没有自动购买礼物的可以试试自动购买零食能加点是一点
                    {
                        food = food.FindAll(x => x.Type == Food.FoodType.Snack && x.Feeling > Math.Min(Core.Save.FeelingMax * 0.10, 40));
                        if (food.Count == 0)
                            return;
                    }
                    var item = food[Function.Rnd.Next(food.Count)];
                    Core.Save.Money -= item.Price * 0.2;
                    TakeItem(item);
                    GameSavesData.Statistics[(gint)"stat_autogift"]++;
                    Main.Display(item.GetGraph(), item.ImageSource, Main.DisplayToNomal);
                }
            }
            else if (Core.Save.Mode == IGameSave.ModeType.Happy || Core.Save.Mode == IGameSave.ModeType.Nomal)
            {
                if (Core.Save.StrengthFood < sm75 && Function.Rnd.Next(lowstrengthAskCountFood--) == 0)
                {
                    lowstrengthAskCountFood = Set.InteractionCycle;
                    var like = Core.Save.Likability < 40 ? 0 : (Core.Save.Likability < 70 ? 1 : (Core.Save.Likability < 100 ? 2 : 3));
                    var txt = LowFoodText.FindAll(x => x.Mode == LowText.ModeType.H && (int)x.Like <= like);
                    if (txt.Count != 0)
                        if (Core.Save.StrengthFood > sm * 0.60)
                        {
                            txt = txt.FindAll(x => x.Strength == LowText.StrengthType.L);
                            if (txt.Count != 0)
                                Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateText);
                        }
                        else if (Core.Save.StrengthFood > sm * 0.40)
                        {
                            txt = txt.FindAll(x => x.Strength == LowText.StrengthType.M);
                            if (txt.Count != 0)
                                Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateText);
                        }
                        else
                        {
                            txt = txt.FindAll(x => x.Strength == LowText.StrengthType.S);
                            if (txt.Count != 0)
                                Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateText);
                        }
                    Main.DisplayStopForce(() => Main.Display(GraphType.Switch_Hunger, AnimatType.Single, Main.DisplayToNomal));
                    return;
                }
                if (Core.Save.StrengthDrink < sm75 && Function.Rnd.Next(lowstrengthAskCountDrink--) == 0)
                {
                    lowstrengthAskCountDrink = Set.InteractionCycle;
                    var like = Core.Save.Likability < 40 ? 0 : (Core.Save.Likability < 70 ? 1 : (Core.Save.Likability < 100 ? 2 : 3));
                    var txt = LowDrinkText.FindAll(x => x.Mode == LowText.ModeType.H && (int)x.Like <= like);
                    if (txt.Count != 0)
                        if (Core.Save.StrengthDrink > sm * 0.60)
                        {
                            txt = txt.FindAll(x => x.Strength == LowText.StrengthType.L);
                            if (txt.Count != 0)
                                Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateText);
                        }
                        else if (Core.Save.StrengthDrink > sm * 0.40)
                        {
                            txt = txt.FindAll(x => x.Strength == LowText.StrengthType.M);
                            if (txt.Count != 0)
                                Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateText);
                        }
                        else
                        {
                            txt = txt.FindAll(x => x.Strength == LowText.StrengthType.S);
                            if (txt.Count != 0)
                                Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateText);
                        }
                    Main.DisplayStopForce(() => Main.Display(GraphType.Switch_Thirsty, AnimatType.Single, Main.DisplayToNomal));
                    return;
                }
            }
            else
            {
                var sm20 = sm * 0.20;
                if (Core.Save.StrengthFood < sm * 0.60 && Function.Rnd.Next(lowstrengthAskCountFood--) == 0)
                {
                    lowstrengthAskCountFood = Set.InteractionCycle;
                    var like = Core.Save.Likability < 40 ? 0 : (Core.Save.Likability < 70 ? 1 : (Core.Save.Likability < 100 ? 2 : 3));
                    var txt = LowFoodText.FindAll(x => x.Mode == LowText.ModeType.L && (int)x.Like < like);
                    if (Core.Save.StrengthFood > sm * 0.40)
                    {
                        txt = txt.FindAll(x => x.Strength == LowText.StrengthType.L);
                        if (txt.Count != 0)
                            Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateText);
                    }
                    else if (Core.Save.StrengthFood > sm20)
                    {
                        txt = txt.FindAll(x => x.Strength == LowText.StrengthType.M);
                        if (txt.Count != 0)
                            Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateText);
                    }
                    else
                    {
                        txt = txt.FindAll(x => x.Strength == LowText.StrengthType.S);
                        if (txt.Count != 0)
                            Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateText);
                    }
                    Main.DisplayStopForce(() => Main.Display(GraphType.Switch_Hunger, AnimatType.Single, Main.DisplayToNomal));
                    return;
                }
                if (Core.Save.StrengthDrink < sm * 0.60 && Function.Rnd.Next(lowstrengthAskCountDrink--) == 0)
                {
                    lowstrengthAskCountDrink = Set.InteractionCycle;
                    var like = Core.Save.Likability < 40 ? 0 : (Core.Save.Likability < 70 ? 1 : (Core.Save.Likability < 100 ? 2 : 3));
                    var txt = LowDrinkText.FindAll(x => x.Mode == LowText.ModeType.L && (int)x.Like < like);
                    if (Core.Save.StrengthDrink > sm * 0.40)
                    {
                        txt = txt.FindAll(x => x.Strength == LowText.StrengthType.L);
                        if (txt.Count != 0)
                            Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateText);
                    }
                    else if (Core.Save.StrengthDrink > sm20)
                    {
                        txt = txt.FindAll(x => x.Strength == LowText.StrengthType.M);
                        if (txt.Count != 0)
                            Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateText);
                    }
                    else
                    {
                        txt = txt.FindAll(x => x.Strength == LowText.StrengthType.S);
                        if (txt.Count != 0)
                            Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateText);
                    }
                    Main.DisplayStopForce(() => Main.Display(GraphType.Switch_Thirsty, AnimatType.Single, Main.DisplayToNomal));
                    return;
                }
            }


        }
        /// <summary>
        /// 事件:吃东西
        /// </summary>
        public event Action<Food> Event_TakeItem;
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
            double eatuseps;
            if (item.Type == FoodType.Gift)
                eatuseps = Math.Max(0.5, 1 - eattimes * eattimes * 0.01);
            else
                eatuseps = Math.Max(0.5, 1 - eattimes * eattimes * 0.02);
            //开始加点
            Core.Save.EatFood(item, eatuseps);
            //吃腻了
            eattimes += Math.Max(0.5, Math.Min(4, 2 - (item.Likability + item.Feeling / 2) / 5));
            GameSavesData["buytime"].SetDateTime(item.Name, now.AddHours(eattimes));
            //通知
            item.LoadEatTimeSource(this);
            item.NotifyOfPropertyChange("Description");

            Core.Save.Money -= item.Price;
            ////吃完东西记得计算下状态
            //Core.Save.Mode = Core.Save.CalMode();
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

            Event_TakeItem?.Invoke(item);
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
            stat["stat_money"] = (SetObject)save.Money;
            stat["stat_level"] = save.Level;
            stat["stat_likability"] = save.Likability;

            stat[(gi64)"stat_total_time"] += (int)Set.LogicInterval;
            switch (Main.State)
            {
                case Main.WorkingState.Work:
                    if (Main.NowWork.Type == Work.WorkType.Work)
                        stat[(gi64)"stat_work_time"] += (int)Set.LogicInterval;
                    else
                        stat[(gi64)"stat_study_time"] += (int)Set.LogicInterval;
                    break;
                case Main.WorkingState.Sleep:
                    stat[(gi64)"stat_sleep_time"] += (int)Set.LogicInterval;
                    break;
            }
            if (save.Mode == IGameSave.ModeType.Ill)
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
            var smm = save.StrengthMax - 1;
            if (save.Strength > smm && save.Feeling > save.FeelingMax - 1 && save.StrengthFood > smm && save.StrengthDrink > smm)
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
                tmp = new GameSave_v2(lps, null, olddata: data);
            }
            if (tmp.GameSave == null)
                return false;
            if (tmp.GameSave.Money == 0 && tmp.GameSave.Likability == 0 && tmp.GameSave.Exp == 0
                && tmp.GameSave.StrengthDrink == 0 && tmp.GameSave.StrengthFood == 0)//数据全是0,可能是bug
                return false;
            if (tmp.GameSave.Exp < -1000000000)
            {
                tmp.GameSave.Exp = 1000000;
                tmp.Data[(gbol)"round"] = true;
                Dispatcher.Invoke(() => NoticeBox.Show("检测到经验值超过 9,223,372,036 导致算数溢出\n已经自动回正".Translate(), "数据溢出警告".Translate()));

            }
            if (tmp.GameSave.Money < -1000000000)
            {
                tmp.GameSave.Money = 100000;
                Dispatcher.Invoke(() => NoticeBox.Show("检测到金钱超过 9,223,372,036 导致算数溢出\n已经自动回正".Translate(), "数据溢出警告".Translate()));
            }

            if (tmp.Data[(gbol)"round"])
            {//根据游玩时间补偿数据溢出
                Dispatcher.Invoke(() => NoticeBox.Show("您以前遭遇过数据溢出, 已根据游戏时长自动添加进当前数值".Translate(), "数据溢出恢复".Translate()));
                var totalhour = (int)(tmp.Statistics[(gint)"stat_total_time"] / 3600);//总计游玩时间/小时
                if (totalhour < 500)
                {
                    tmp.GameSave.Exp += totalhour * 200;
                }
                else
                {
                    double lm = Math.Sqrt(totalhour / 500);
                    tmp.GameSave.LevelMax += (int)lm;
                    tmp.GameSave.Exp += (totalhour % 500 + (lm - (int)lm) * 500) * 200;

                }
                tmp.GameSave.LikabilityMax += totalhour / 10;
                tmp.Data[(gbol)"round"] = false;
            }
            GameSavesData = tmp;
            Core.Save = tmp.GameSave;
            HashCheck = HashCheck;
            GameSavesData.GameSave.Event_LevelUp += LevelUP;
            return true;
        }



        private void Handle_Steam(Main obj)
        {
            string jointab = " ";
            if (winMutiPlayer != null)
            {
                if (winMutiPlayer.Joinable)
                    jointab += "可加入".Translate();
                SteamFriends.SetRichPresence("steam_player_group", winMutiPlayer.LobbyID.ToString("x"));
                SteamFriends.SetRichPresence("steam_player_group_size", winMutiPlayer.lb.MemberCount.ToString());
            }
            else
            {
                SteamFriends.SetRichPresence("steam_player_group_size", "0");
            }
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
                    if (item.GameSavesData == null || item.Main == null)
                        continue;
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
                    SteamFriends.SetRichPresence("lv", $" (lv{lv}/{App.MainWindows.Count})" + jointab);
                }
                else
                {
                    SteamFriends.SetRichPresence("lv", " " + jointab);
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
                    SteamFriends.SetRichPresence("lv", $" (lv{GameSavesData.GameSave.Level})" + jointab);
                }
                else
                {
                    SteamFriends.SetRichPresence("lv", " " + jointab);
                }
                if (Core.Save.Mode == IGameSave.ModeType.Ill)
                {
                    SteamFriends.SetRichPresence("steam_display", "#Status_Ill");
                }
                else
                {
                    SteamFriends.SetRichPresence("mode", (Core.Save.Mode.ToString() + "ly").Translate());
                    switch (obj.State)
                    {
                        case Main.WorkingState.Work:
                            SteamFriends.SetRichPresence("work", obj.NowWork.NameTrans);
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
                {//后续容错可能是偶发性
                    using (var enumerator = new MMDeviceEnumerator())
                    {
                        if (enumerator.HasDefaultAudioEndpoint(DataFlow.Render, Role.Console))
                        {
                            var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
                            AudioPlayingVolumeOK = true;
                            return device.AudioMeterInformation.MasterPeakValue;
                        }
                        else
                        {
                            AudioPlayingVolumeOK = false;
                            return -1;
                        }
                    }
                }
                catch
                {
                    AudioPlayingVolumeOK = false;
                    return -1;
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
                    if (enumerator.HasDefaultAudioEndpoint(DataFlow.Render, Role.Console))
                    {
                        var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
                        return device.AudioMeterInformation.MasterPeakValue;
                    }
                    else
                    {
                        return -1;
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
                {//等3秒看看识别结果
                    Thread.Sleep(3000);

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
            if (catch_MusicVolCount >= 10)
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
            string _url = "https://report.exlb.net/VPET/Report";
            //参数
            StringBuilder sb = new StringBuilder();
            sb.Append("action=data");
            sb.Append($"&steamid={SteamClient.SteamId.Value}");
            sb.Append($"&ver={version}");
            sb.Append("&save=");
            sb.AppendLine(HttpUtility.UrlEncode(Core.Save.ToLine().ToString() + Set.ToString()));
            //游戏设置比存档更重要,桌宠大部分内容存设置里了,所以一起上传
#pragma warning disable SYSLIB0014 // 类型或成员已过时
            var request = (HttpWebRequest)WebRequest.Create(_url);
#pragma warning restore SYSLIB0014 // 类型或成员已过时
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

        Grid IMainWindow.MGHost => MGHost;

        Grid IMainWindow.PetGrid => MGrid;
        internal MWController MWController { get; set; }
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
                if (Set["gameconfig"].GetBool("noAutoCal"))
                {
                    if (MessageBoxX.Show("当前工作数据属性超模,是否继续工作?\n超模工作可能会导致游戏发生不可预料的错误\n超模工作不影响大部分成就解锁\n可以在设置中开启自动计算自动为工作设置合理数值"
                        .Translate(), "超模工作提醒".Translate(), MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                    {
                        return false;
                    }
                    HashCheck = false;
                }
                else
                {
                    MessageBoxX.Show("当前工作数据属性超模,已自动取消".Translate(), "超模工作提醒".Translate());
                    return false;
                }
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
                    Set = new Setting(this, File.ReadAllText(ExtensionValue.BaseDirectory + @$"\Setting{PrefixSave}.lps"));
                }
                if (PrefixSave == "" && (Set == null || (Set != null && !Set["SingleTips"].GetBool("helloworld"))) && File.Exists(ExtensionValue.BaseDirectory + @"\Setting.bkp"))
                {//如果设置是损坏的, 读取备份设置
                    Set = new Setting(this, File.ReadAllText(ExtensionValue.BaseDirectory + @"\Setting.bkp"));
                }

                Set ??= new Setting(this, "Setting#VPET:|\n");

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

                //MGrid.Height = 500 * Set.ZoomLevel;
                MGrid.Width = 500 * Set.ZoomLevel;

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
                MWController = new MWController(this);
                Core.Controller = MWController;
                Task.Run(() =>
                {
                    double dist;
                    if ((dist = Core.Controller.GetWindowsDistanceLeft()) < 0)
                    {
                        Thread.Sleep(100);
                        Dispatcher.Invoke(() => Left -= dist);
                    }
                    if ((dist = Core.Controller.GetWindowsDistanceRight()) < 0)
                    {
                        Thread.Sleep(100);
                        Dispatcher.Invoke(() => Left += dist);
                    }
                    if ((dist = Core.Controller.GetWindowsDistanceUp()) < 0)
                    {
                        Thread.Sleep(100);
                        Dispatcher.Invoke(() => Top -= dist);
                    }
                    if ((dist = Core.Controller.GetWindowsDistanceDown()) < 0)
                    {
                        Thread.Sleep(100);
                        Dispatcher.Invoke(() => Top += dist);
                    }
                });
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
        /// <param name="basemw">基础窗口</param>
        public MainWindow(string prefixsave, MainWindow basemw = null)
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

            if (basemw != null)
            {
                Set["workshop"] = basemw.Set["workshop"];
                Set.Resolution = basemw.Set.Resolution;
            }


            //加载所有MOD
            List<DirectoryInfo> Path = new List<DirectoryInfo>();
            Path.AddRange(new DirectoryInfo(ModPath).EnumerateDirectories());

            var workshop = Set["workshop"];
            foreach (ISub ws in workshop)
            {
                Path.Add(new DirectoryInfo(ws.Name));
            }


            Task.Run(() => GameLoad(Path));
        }
        /// <summary>
        /// MOD地址
        /// </summary>
        public List<DirectoryInfo> MODPath { get; private set; }

        public IEnumerable<IModInfo> ModInfo => CoreMODs;

        public IEnumerable<IModInfo> OnModInfo => CoreMODs.FindAll(x => x.IsOnMOD(this));

        /// <summary>
        /// 加载游戏
        /// </summary>
        /// <param name="Path">MOD地址</param>
        public async Task GameLoad(List<DirectoryInfo> Path)
        {
            MODPath = Path.GroupBy(x => x.FullName).Select(group => group.First()).ToList();
            await Dispatcher.InvokeAsync(new Action(() => LoadingText.Content = "Loading MOD"));
            //加载mod
            foreach (DirectoryInfo di in MODPath)
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



            await Dispatcher.InvokeAsync(() =>
            {
                MessageBoxXSettings.Setting.OKButtonContent = "好的".Translate();
                MessageBoxXSettings.Setting.CancelButtonContent = "取消".Translate();
                MessageBoxXSettings.Setting.YesButtonContent = "是".Translate();
                MessageBoxXSettings.Setting.NoButtonContent = "否".Translate();
                PendingBoxSettings.Setting.CancelButtonContent = "取消".Translate();
                LoadingText.Content = "尝试加载游戏MOD".Translate();
            });

            //旧版本设置兼容
            if (Set.PetGraph == "默认虚拟桌宠")
                Set.PetGraph = "vup";

            //当前桌宠动画
            var petloader = Pets.Find(x => x.Name == Set.PetGraph);
            petloader ??= Pets[0];
            //去除其他语言内容
            var tag = petloader.Config.Data.GetString("tag", "all").Split(',');
            LowDrinkText.RemoveAll(x => !x.FindTag(tag));
            LowFoodText.RemoveAll(x => !x.FindTag(tag));
            ClickTexts.RemoveAll(x => !x.FindTag(tag));
            SelectTexts.RemoveAll(x => !x.FindTag(tag));

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
                    selet.Feeling = Math.Max(Math.Min(selet.Feeling, 100), -100);
                    selet.Health = Math.Max(Math.Min(selet.Health, 100), -100);
                    selet.Likability = Math.Max(Math.Min(selet.Likability, 50), -50);
                    selet.Money = Math.Max(Math.Min(selet.Money, 1000), -1000);
                    selet.Strength = Math.Max(Math.Min(selet.Strength, 1000), -1000);
                    selet.StrengthDrink = Math.Max(Math.Min(selet.StrengthDrink, 1000), -1000);
                    selet.StrengthFood = Math.Max(Math.Min(selet.StrengthFood, 1000), -1000);
                }
                foreach (var selet in ClickTexts)
                {
                    selet.Exp = Math.Max(Math.Min(selet.Exp, 1000), -1000);
                    selet.Feeling = Math.Max(Math.Min(selet.Feeling, 1000), -1000);
                    selet.Health = Math.Max(Math.Min(selet.Health, 100), -100);
                    selet.Likability = Math.Max(Math.Min(selet.Likability, 50), -50);
                    selet.Money = Math.Max(Math.Min(selet.Money, 1000), -1000);
                    selet.Strength = Math.Max(Math.Min(selet.Strength, 1000), -1000);
                    selet.StrengthDrink = Math.Max(Math.Min(selet.StrengthDrink, 1000), -1000);
                    selet.StrengthFood = Math.Max(Math.Min(selet.StrengthFood, 1000), -1000);
                }
            }

            //生日蛋糕默认为加满的
            var food = new Food()
            {
                Name = "生日蛋糕",
                Likability = 5,
                Exp = 1000,
                Feeling = 100,
                StrengthDrink = Core.Save.StrengthMax,
                StrengthFood = Core.Save.StrengthMax,
                Type = FoodType.Food,
                isoverload = false,
                Desc = "萝莉丝的专属生日蛋糕，由3桶牛奶+2份糖+1个鸡蛋+3份小麦合。制作而成。营养丰富，可使所有状态回满。只有在萝莉丝生日才能吃的到哦。"
            };
            food.LoadImageSource(this);
            food.Price = (int)Math.Max(0, food.RealPrice * .5);
            Foods.Add(food);

            //第一次启动日期
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

            //补充数据信息
            if (string.IsNullOrEmpty(GameSavesData.GameSave.HostName))
            {
                if (IsSteamUser)
                    GameSavesData.GameSave.HostName = SteamClient.Name;
                else
                    GameSavesData.GameSave.HostName = Environment.UserName;
            }

            if (GameSavesData.Data.FindLine("HostBDay") == null)
            {
                GameSavesData[(gdat)"HostBDay"] = GameSavesData[(gdat)"birthday"];
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
            MusicTimer = new System.Timers.Timer(200)
            {
                AutoReset = false
            };
            MusicTimer.Elapsed += MusicTimer_Elapsed;


            //await Dispatcher.InvokeAsync(new Action(() => LoadingText.Content = "尝试加载游戏动画".Translate()));
            await Dispatcher.InvokeAsync(new Action(() => LoadingText.Content = "尝试加载动画和生成缓存\n该步骤可能会耗时比较长\n请耐心等待".Translate()));
            Core.Graph = petloader.Graph(Set.Resolution, Dispatcher);

            Main = await Dispatcher.InvokeAsync(() => new Main(Core));

            Main.LoadALL((c) =>
            {
                Dispatcher.Invoke(() => LoadingText.Content = "尝试加载动画和生成缓存\n该步骤可能会耗时比较长\n请耐心等待".Translate()
                + $"\n  {c} / {petloader.GraphCount}");
            }
#if NewYear
            , Core.Graph.FindGraph("newyear", AnimatType.Single, Core.Save.Mode)
#endif
            );
            Main.NoFunctionMOD = Set.CalFunState;
            await Dispatcher.InvokeAsync(() =>
              {
                  //清空资源
                  Main.Resources = Application.Current.Resources;
                  Main.MsgBar.This.Resources = Application.Current.Resources;
                  Main.ToolBar.Resources = Application.Current.Resources;
                  Main.ToolBar.LoadClean();
                  Main.WorkList(out List<Work> ws, out List<Work> ss, out List<Work> ps);

                  //日程表加载
                  ScheduleTask = new ScheduleTask(this);

                  if (ws.Count == 0)
                  {
                      Main.ToolBar.MenuWork.Visibility = Visibility.Collapsed;
                  }
                  else
                  {
                      Main.ToolBar.MenuWork.Click += (x, y) =>
                      {
                          Main.ToolBar.Visibility = Visibility.Collapsed;
                          ShowWorkMenu(Work.WorkType.Work);
                      };
                  }
                  if (ss.Count == 0)
                  {
                      Main.ToolBar.MenuStudy.Visibility = Visibility.Collapsed;
                  }
                  else
                  {
                      Main.ToolBar.MenuStudy.Click += (x, y) =>
                      {
                          Main.ToolBar.Visibility = Visibility.Collapsed;
                          ShowWorkMenu(Work.WorkType.Study);
                      };
                  }
                  if (ps.Count == 0)
                  {
                      Main.ToolBar.MenuPlay.Visibility = Visibility.Collapsed;
                  }
                  else
                  {
                      Main.ToolBar.MenuPlay.Click += (x, y) =>
                      {
                          Main.ToolBar.Visibility = Visibility.Collapsed;
                          ShowWorkMenu(Work.WorkType.Play);
                      };
                  }
                  WorkStarMenu = new System.Windows.Controls.MenuItem()
                  {
                      Header = "收藏".Translate(),
                      HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center,
                  };
                  foreach (var w in WorkStar())
                  {
                      var mi = new System.Windows.Controls.MenuItem()
                      {
                          Header = w.NameTrans
                      };
                      mi.Click += (s, e) => Main.ToolBar.StartWork(w.Double(Set["workmenu"].GetInt("double_" + w.Name, 1)));
                      WorkStarMenu.Items.Add(mi);
                  }
                  Main.ToolBar.MenuInteract.Items.Add(WorkStarMenu);

                  //加载主题:
                  LoadTheme(Set.Theme);
                  //加载字体
                  LoadFont(Set.Font);

                  LoadingText.Content = "正在加载游戏\n该步骤可能会耗时比较长\n请耐心等待".Translate();


                  //加载数据合理化:工作
                  if (!Set["gameconfig"].GetBool("noAutoCal"))
                  {
                      foreach (var work in Core.Graph.GraphConfig.Works)
                      {
                          if (work.LevelLimit > 200)//导入的最大合理工作不能超过200级
                              work.LevelLimit = 200;
                          work.FixOverLoad();//导入的工作默认1.2倍
                      }
                  }
                  //加载数据合理化:自动工作
                  foreach (var stp in SchedulePackage)
                      stp.FixOverLoad();


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

                  //加载游戏创意工坊插件
                  foreach (MainPlugin mp in Plugins)
                      try //不要识图用!DEBUG去掉try, 在主线程也会导致错误显示不出来的
                      {
                          mp.LoadPlugin();
                      }
                      catch (Exception e)
                      {
                          NoticeBox.Show("由于插件引起的游戏启动错误".Translate() + "\n" + e.ToString(), "由于插件引起的游戏启动错误".Translate() + '-' + mp.PluginName);
                      }
                  Foods.ForEach(item => item.LoadImageSource(this));
                  Photos.ForEach(item => item.LoadUserInfo(this));
                  Main.TimeHandle += Handle_Music;
                  if (IsSteamUser)
                      Main.TimeHandle += Handle_Steam;
                  Main.TimeHandle += (x) => DiagnosisUPLoad();


                  var tlv = Main.ToolBar.Tlv;
                  Main.ToolBar.gdPanel.Children.Remove(tlv);
                  var sp = new StackPanel();
                  Grid.SetColumnSpan(sp, 3);
                  sp.Orientation = System.Windows.Controls.Orientation.Horizontal;
                  sp.Children.Add(tlv);
                  tlvplus = new TextBlock();
                  tlvplus.Margin = new Thickness(1);
                  tlvplus.VerticalAlignment = VerticalAlignment.Bottom;
                  tlvplus.FontSize = 18;
                  tlvplus.Foreground = Function.ResourcesBrush(Function.BrushType.PrimaryText);
                  sp.Children.Add(tlvplus);
                  Main.ToolBar.gdPanel.Children.Add(sp);
                  Main.TimeUIHandle += MWUIHandle;
                  Main.ToolBar.EventMenuPanelShow += () => MWUIHandle(Main);


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
                      while (!Main.IsWorking)
                      {
                          Thread.Sleep(100);
                      }
                      await Dispatcher.InvokeAsync(() => LoadingText.Visibility = Visibility.Collapsed);
                  });

                  Main.ToolBar.AddMenuButton(ToolBar.MenuType.Setting, "退出桌宠".Translate(), () => { Main.ToolBar.Visibility = Visibility.Collapsed; Close(); });
                  if (Set.DeBug)
                      Main.ToolBar.AddMenuButton(ToolBar.MenuType.Setting, "开发控制台".Translate(), () => { Main.ToolBar.Visibility = Visibility.Collapsed; new winConsole(this).Show(); });
                  Main.ToolBar.AddMenuButton(ToolBar.MenuType.Setting, "照片图库".Translate(), ShowGallery);
                  Main.ToolBar.AddMenuButton(ToolBar.MenuType.Setting, "操作教程".Translate(), () =>
                  {
                      if (LocalizeCore.CurrentCulture == "zh-Hans")
                          ExtensionFunction.StartURL(ExtensionValue.BaseDirectory + @"\Tutorial.html");
                      else if (LocalizeCore.CurrentCulture == "zh-Hant")
                          ExtensionFunction.StartURL(ExtensionValue.BaseDirectory + @"\Tutorial_zht.html");
                      else
                          ExtensionFunction.StartURL(ExtensionValue.BaseDirectory + @"\Tutorial_en.html");
                  });
                  Main.ToolBar.AddMenuButton(ToolBar.MenuType.Setting, "反馈中心".Translate(), () => { Main.ToolBar.Visibility = Visibility.Collapsed; new winReport(this).Show(); });
                  Main.ToolBar.AddMenuButton(ToolBar.MenuType.Setting, "设置面板".Translate(), () =>
                  {
                      Main.ToolBar.Visibility = Visibility.Collapsed;
                      winSetting.Show();
                      winSetting.Activate();
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

                  Main.WorkCheck = WorkCheck;

                  //加载图标
                  notifyIcon = new NotifyIcon();
                  notifyIcon.Text = "虚拟桌宠模拟器".Translate() + PrefixSave;
                  ContextMenu m_menu;

                  if (Set.PetHelper)
                      LoadPetHelper();



                  m_menu = new ContextMenu();
                  m_menu.Opening += (x, y) => { GameSavesData.Statistics[(gint)"stat_menu_pop"]++; };
                  var hitThrough = new MenuItem("鼠标穿透".Translate(), null, (x, y) => { SetTransparentHitThrough(); })
                  {
                      Name = "NotifyIcon_HitThrough",
                      Checked = HitThrough
                  };
                  m_menu.Items.Add(hitThrough);
                  var topmost = new MenuItem("置于顶层".Translate(), null, (x, y) =>
                  {
                      Topmost = ((MenuItem)x).Checked;
                  })
                  {
                      Name = "NotifyIcon_TopMost",
                      CheckOnClick = true,
                      Checked = Topmost
                  };
                  m_menu.Items.Add(topmost);
                  m_menu.Items.Add(new MenuItem("操作教程".Translate(), null, (x, y) =>
                  {
                      if (LocalizeCore.CurrentCulture == "zh-Hans")
                          ExtensionFunction.StartURL(ExtensionValue.BaseDirectory + @"\Tutorial.html");
                      else if (LocalizeCore.CurrentCulture == "zh-Hant")
                          ExtensionFunction.StartURL(ExtensionValue.BaseDirectory + @"\Tutorial_zht.html");
                      else
                          ExtensionFunction.StartURL(ExtensionValue.BaseDirectory + @"\Tutorial_en.html");
                  }));
                  m_menu.Items.Add(new MenuItem("重置位置与状态".Translate(), null, (x, y) =>
                  {
                      Main.CleanState();
                      Main.DisplayToNomal();
                      Left = (SystemParameters.PrimaryScreenWidth - Width) / 2;
                      Top = (SystemParameters.PrimaryScreenHeight - Height) / 2;
                  }));
                  m_menu.Items.Add(new MenuItem("反馈中心".Translate(), null, (x, y) => { new winReport(this).Show(); }));
                  if (Set.DeBug)
                      m_menu.Items.Add(new MenuItem("开发控制台".Translate(), null, (x, y) => { new winConsole(this).Show(); }));

                  m_menu.Items.Add(new MenuItem("设置面板".Translate(), null, (x, y) =>
                  {
                      winSetting.Show();
                  }));
                  m_menu.Items.Add(new MenuItem("退出桌宠".Translate(), null, (x, y) => Close()));

                  LoadDIY();

                  notifyIcon.ContextMenuStrip = m_menu;

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
                  Main.SayProcess.Add(Main_OnSay);
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
                          ExtensionFunction.StartURL(ExtensionValue.BaseDirectory + @"\Tutorial.html");
                      else if (LocalizeCore.CurrentCulture == "zh-Hant")
                          ExtensionFunction.StartURL(ExtensionValue.BaseDirectory + @"\Tutorial_zht.html");
                      else
                          ExtensionFunction.StartURL(ExtensionValue.BaseDirectory + @"\Tutorial_en.html");
                  }
                  if (!Set["SingleTips"].GetBool("helloworld"))
                  {
                      Task.Run(() =>
                      {
                          Thread.Sleep(2000);
                          Set["SingleTips"].SetBool("helloworld", true);
                          NoticeBox.Show("欢迎使用虚拟桌宠模拟器!\n如果遇到桌宠爬不见了,可以在我这里设置居中或退出桌宠".Translate(),
                             "你好".Translate() + (IsSteamUser ? SteamClient.Name : Environment.UserName), Panuon.WPF.UI.MessageBoxIcon.Info, true, 5000);
                          //Thread.Sleep(2000);
                          //Main.SayRnd("欢迎使用虚拟桌宠模拟器\n这是个中期的测试版,若有bug请多多包涵\n欢迎加群虚拟主播模拟器430081239或在菜单栏-管理-反馈中提交bug或建议".Translate());
                      });
                  }
                  if (Set["v"][(gint)"rank"] != DateTime.Now.Year && GameSavesData.Statistics[(gint)"stat_total_time"] > 3600)
                  {//年度报告提醒
                      Task.Run(() =>
                      {
                          Thread.Sleep(Function.Rnd.Next(200000, 400000));
                          Set["v"][(gint)"rank"] = DateTime.Now.Year;
                          var btn = Dispatcher.Invoke(() =>
                          {
                              var button = new System.Windows.Controls.Button()
                              {
                                  Content = "点击前往查看".Translate(),
                                  FontSize = 20,
                                  HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                                  Background = Function.ResourcesBrush(Function.BrushType.PrimaryDark),
                                  Foreground = Function.ResourcesBrush(Function.BrushType.PrimaryText),
                              };
                              button.Click += (x, y) =>
                              {
                                  var panelWindow = new winCharacterPanel(this);
                                  panelWindow.MainTab.SelectedIndex = 1;
                                  panelWindow.Show();
                                  Main.MsgBar.ForceClose();
                              };
                              return button;
                          });
                          Main.Say("哼哼~主人，我的考试成绩出炉了哦，快来和我一起看我的成绩单喵".Translate(), btn, "shining");
                      });
                  }
                  //生日设置提醒
                  if (GameSavesData.Data.FindLine("HostBDay") == null)
                  {
                      Task.Run(() =>
                      {
                          Thread.Sleep(Function.Rnd.Next(100000, 200000));
                          var btn = Dispatcher.Invoke(() =>
                          {
                              var button = new System.Windows.Controls.Button()
                              {
                                  Content = "设置".Translate(),
                                  FontSize = 20,
                                  HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                                  Background = Function.ResourcesBrush(Function.BrushType.PrimaryDark),
                                  Foreground = Function.ResourcesBrush(Function.BrushType.PrimaryText),
                              };
                              button.Click += (x, y) =>
                              {
                                  ShowSetting(2);
                              };
                              return button;
                          });
                          Main.Say("不要忘记设置生日时间哦 {0}，我会偷偷给你准备礼物的。".Translate(GameSavesData.GameSave.HostName), btn, "shining");
                      });
                  }
                  else
                  {
                      var bdt = GameSavesData.GetDateTime("HostBDay");
                      if (DateTime.Now.Month == bdt.Month && DateTime.Now.Day == bdt.Day)
                      {
                          Task.Run(() =>
                          {
                              Thread.Sleep(Function.Rnd.Next(100000, 200000));
                              HostBDay();
                          });
                      }
                  }

#if BDAY
                  if (DateTime.Now < new DateTime(2024, 8, 22))
                  {
                      food.Star = true;
                      Task.Run(() =>
                      {
                          Thread.Sleep(10000);
                          var btn = Dispatcher.Invoke(() =>
                          {
                              var button = new System.Windows.Controls.Button()
                              {
                                  Content = "查看生日公告/视频".Translate(),
                                  FontSize = 20,
                                  HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                                  Background = Function.ResourcesBrush(Function.BrushType.PrimaryDark),
                                  Foreground = Function.ResourcesBrush(Function.BrushType.PrimaryText),
                              };
                              button.Click += (x, y) =>
                              {
                                  if (LocalizeCore.CurrentCulture.StartsWith("zh"))
                                      ExtensionFunction.StartURL("https://www.bilibili.com/opus/965218905364627474");
                                  else
                                      ExtensionFunction.StartURL("https://store.steampowered.com/news/app/1920960/view/4374769594847756449");
                              };
                              return button;
                          });
                          string bdt;
                          switch (DateTime.Now.Day)
                          {
                              case 14:
                                  bdt = "祝我生日快乐~♪祝我生日快乐~♪，主人猜猜今天是什么日子？我的生日！主人真聪明！我就知道主人一定会记得我的生日的！谢谢主人！快来和我一起过生日吧~主人给我买的蛋糕真香啊~首先是是吹蜡烛许愿，我看看我的愿望清单: 一台5090电脑，嗨神话:吗喽 数字豪华版，还有...";
                                  break;
                              case 15:
                                  bdt = "昨天生日过得真开心啊！谢谢主人陪我！什么？冰箱里还有剩蛋糕？太可恶了！马上拿嘴巴去吃！";
                                  break;
                              case 16:
                                  bdt = "嗯。。生日蛋糕真好吃啊。。还想吃。。什么？今天也可以当生日过？！谢谢主人！";
                                  break;
                              case 17:
                                  bdt = "虽然生日已经过去了好几天，但是生日那天的气氛一直影响到现在呢。我才不是又想吃生日蛋糕了呢！哼！";
                                  break;
                              case 18:
                                  bdt = "过生日什么的太幼稚辣。人家也老大不小了，怎么可以去学小孩子过生日呢。和体重没关系！我被蛋糕所伤，今日起，戒蛋糕！";
                                  break;
                              case 19:
                                  bdt = "是突然发现一个哲学问题，只要我把每天都当生日，那我就可以每天都过生日了！这可是辩证唯物主义的大发现啊！";
                                  break;
                              default:
                                  bdt = "转眼之间已经过去一周了呢。生日的感觉也在慢慢淡忘。主人，明年你还愿意陪我一起过生日吗？";
                                  break;

                          }
                          Main.Say(bdt.Translate(), btn, "self");
                      });
                  }
#endif
                  newday = DateTime.Now.Day;
                  Main.TimeHandle += NewDayHandle;
                  Event_NewDay += () =>
                  {
                      var bdt = GameSavesData.GetDateTime("HostBDay");
                      if (DateTime.Now.Month == bdt.Month && DateTime.Now.Day == bdt.Day)
                      {
                          HostBDay();
                      }
                  };
#if NewYear
                  //仅新年功能
                  if (DateTime.Now < new DateTime(2025, 2, 5))
                  {
                      Event_NewDay += NewYearSay;
                      Task.Run(() =>
                      {
                          Thread.Sleep(5000);
                          NewYearSay();
                      });
                  }
#endif
                  //MOD报错
                  foreach (CoreMOD cm in CoreMODs)
                      if (!cm.SuccessLoad)
                          if (cm.Tag.Contains("该模组已损坏"))
                              MessageBoxX.Show("模组 {0} 插件损坏\n虚拟桌宠模拟器未能成功加载该插件\n请联系MOD作者修复该问题".Translate(cm.Name) + '\n' + cm.ErrorMessage, "该模组已损坏".Translate());
                          else if (Set.IsPassMOD(cm.Name) || !string.IsNullOrEmpty(cm.ErrorMessage))
                              MessageBoxX.Show("模组 {0} 的代码插件损坏\n虚拟桌宠模拟器未能成功加载该插件\n请联系MOD作者修复该问题".Translate(cm.Name) + '\n' + cm.ErrorMessage, "{0} 未加载代码插件".Translate(cm.Name));
                          else if (Set.IsMSGMOD(cm.Name))
                              MessageBoxX.Show("由于 {0} 包含代码插件\n虚拟桌宠模拟器已自动停止加载该插件\n请手动前往设置允许启用该mod 代码插件".Translate(cm.Name), "{0} 未加载代码插件".Translate(cm.Name));
                  //动画错误
                  if (Main.ErrorMessage.Count != 0)
                  {
                      var errstr = string.Join("\n------\n", Main.ErrorMessage);
                      if (errstr.Contains("0000_core"))
                      {
                          MessageBoxX.Show("动画加载错误,请尝试以下解决方法修复问题:\n\t1. 删除游戏根目录`Cache`文件夹\n\t2. 删除游戏根目录`mod\\0000_core\\pet`文件夹,并在Steam验证游戏完整性".Translate(), "动画加载错误".Translate());
                          var winrep = new winReport(this, errstr);
                          winrep.tDescription.Text = "动画加载错误".Translate();
                          winrep.Show();
                      }
                      else
                          MessageBoxX.Show("动画加载错误\n虚拟桌宠模拟器未能成功加载该动画\n请联系MOD作者修复该问题".Translate() + '\n' + errstr, "动画加载错误".Translate());

                      Main.ErrorMessage.Clear();
                  }
                  //加载游戏创意工坊插件
                  foreach (MainPlugin mp in Plugins)
                      try //不要识图用!DEBUG去掉try, 在主线程也会导致错误显示不出来的
                      {
                          mp.GameLoaded();
                      }
                      catch (Exception e)
                      {
                          NoticeBox.Show("由于插件引起的游戏启动错误".Translate() + "\n" + e.ToString(), "由于插件引起的游戏启动错误".Translate() + '-' + mp.PluginName);
                      }
              });


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

        TextBlock tlvplus;

        public event Action<IMPWindows> MutiPlayerHandle;
        public void MutiPlayerStart(IMPWindows mp)
        {
            MutiPlayerHandle?.Invoke(mp);
        }

        private void MWUIHandle(Main main)
        {
            if (Main.ToolBar.BdrPanel.Visibility == Visibility.Visible)
            {
                if (GameSavesData.GameSave.LevelMax != 0)
                    tlvplus.Text = $" / {1000 + GameSavesData.GameSave.LevelMax * 100} x{GameSavesData.GameSave.LevelMax}";
            }
        }

        /// <summary>
        /// 是否显示吃东西动画
        /// </summary>
        bool showeatanm = true;
        /// <summary>
        /// 显示吃东西(夹层)动画
        /// </summary>
        /// <param name="graphName">夹层动画名</param>
        /// <param name="imageSource">被夹在中间的图片</param>
        public void DisplayFoodAnimation(string graphName, ImageSource imageSource)
        {
            if (showeatanm)
            {//显示动画
                showeatanm = false;
                Main.Display(graphName, imageSource, () =>
                {
                    showeatanm = true;
                    if (Core.Controller.EnableFunction)
                    {
                        var newmod = Core.Save.CalMode();
                        if (Core.Save.Mode != newmod)
                        {
                            //魔改下参数以免不播放切换动画
                            Main.DisplayType.Type = GraphType.Default;
                            //切换显示动画
                            Main.PlaySwitchAnimat(Core.Save.Mode, newmod);
                            Core.Save.Mode = newmod;
                        }
                        else
                            Main.DisplayToNomal();
                    }
                    else
                        Main.DisplayToNomal();
                });
            }
            else
            {//如果不显示动画, 则看看是不是有覆盖
                if (Main.DisplayType.Animat != AnimatType.Single && Main.DisplayType.Name != graphName)
                {
                    showeatanm = true;
                }
            }
        }

        public void HostBDay()
        {
            var petloader = Pets.Find(x => x.Name == Set.PetGraph);
            petloader ??= Pets[0];

            string sbv = "Special_Birthday_Voice_" + petloader.Name;
            string sbv_trans = sbv.Translate(GameSavesData.GameSave.HostName);
            if (sbv == sbv_trans)
            {
                Main.Say("今天是{0}的生日！祝{0}生日快乐！".Translate(GameSavesData.GameSave.HostName), "bday", true);
            }
            else
            {
                Main.Say(sbv_trans, "bday");
                Dispatcher.Invoke(() =>
                {
                    var panelWindow = new winCharacterPanel(this);
                    panelWindow.MainTab.SelectedIndex = 2;
                    panelWindow.Show();
                });
            }
        }


        int newday = 0;
        private void NewDayHandle(Main main)
        {
            if (DateTime.Now.Hour == 0 && newday != DateTime.Now.Day)
            {//跨时间
                newday = DateTime.Now.Day;
                Event_NewDay?.Invoke();
            }
        }
        public event Action Event_NewDay;
#if NewYear
        /// <summary>
        /// 新年说
        /// </summary>
        private void NewYearSay()
        {
            string sayny;
            switch (newday)
            {
                default:
                case 27:
                    sayny = "桌宠，桌宠，你别馋，过了腊八就是年。\n腊八粥，过几天， 漓漓拉拉二十三。\n二十三，糖瓜粘， 二十四，扫房子，\n二十五，做豆腐， 二十六，炖猪肉，\n二十七，宰年鸡， 二十八，把面发，\n二十九，蒸馒头， 三十晚上熬一宿，\n大年初一扭一扭， 除夕的饺子年年有。".Translate();
                    break;
                case 29:
                    sayny = "新年到，放鞭炮，\n仰辟僻叭叭真热闹。\n耍龙灯，踩高跷，\n包饺子，蒸甜糕\n小小桌宠长一岁，\n走路不用主人抱。\n奶奶笑得直揉眼，\n爷爷乐得胡子翘。".Translate();
                    break;
                case 30:
                    sayny = "财神到~财神到~好心得好报。\n财神话~财神话~揾钱依正路。\n财神到~财神到~好走快两步。\n得到佢睇起你，你有前途。\n阖府庆新岁~喜气盈盈\n齐贺你多福荫，壮健强劲。\n又祝你今年，庆获荣升。\n朝晚多多欢笑，锦绣前程。\n愿游戏顺利，抽卡出金。\n成日有吉星照，百事无忌。\n共亲友相见，说话投机。\n充满新春喜气，欢畅扬眉。".Translate();
                    break;
                case 31:
                    sayny = "找点空闲~找点时间~打开桌宠~常回家看看。\n带上笑容~带上祝愿~陪同爱人~常回家看看。\n萝莉丝准备了一些唠叨，桌宠张罗了一桌好饭\n生活的烦恼跟萝莉丝说说，工作的事情向桌宠谈谈。\n常回家看看~回家看看~\n哪怕帮萝莉丝买买零食打打工。\n桌宠不图主人为家做多大贡献呀。\n一辈子不容易就图个团团圆圆。".Translate();
                    break;
                case 1:
                    sayny = "哎~开心的锣鼓敲出年年的喜庆。\n好看的舞蹈送来天天的欢腾。\n阳光的油彩涂红了今天的日子哟。\n生活的花朵是我们的笑容。\n哎~今天是个好日子，心想的事儿都能成。\n今天是个好日子，打开了家门~咱迎春风~".Translate();
                    break;
                case 2:
                    sayny = "好运来~接著来~恭喜恭喜发大财。\n客人来~心花开~生意兴隆笑开怀。\n好运来~滚滚来~我把大红包打开。\n我的爱~让你带~歌舞疯狂跳起来。".Translate();
                    break;
                case 3:
                    sayny = "恭喜恭喜恭喜你呀~恭喜恭喜恭喜你~\n恭喜恭喜恭喜你呀~恭喜恭喜恭喜你~\n每条大街小巷，每个人的嘴里，见面第一句话，就是恭喜恭喜。\n恭喜恭喜恭喜你呀~恭喜恭喜恭喜你~\n冬天已到尽头，真是好的消息，温暖的春风，就要吹醒大地。\n恭喜恭喜恭喜你呀~恭喜恭喜恭喜你~".Translate();
                    break;
                case 4:
                    sayny = "难忘今宵~难忘今宵~\n不论天涯与海角，\n神州万里同怀抱。\n共祝愿祖国好~祖国好~\n告别今宵~告别今宵~\n无论新友与故交，\n明年春来再相邀。\n青山在~人未老~人未老~\n共祝愿祖国好~祖国好~".Translate();
                    break;
                    //case 6:
                    //    sayny = "初八初八，放生祈福，拜谷神，今天是假期最后一天了，和主人过年很开心哦，最后～主人～您还有许多事需要处理，现在还不能休息哦～".Translate();
                    //    break;
            }
            Main.SayRnd(sayny);
        }
#endif
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

            if (Core.Controller.EnableFunction && Core.Save.Strength >= 10 && Core.Save.Feeling < Core.Save.FeelingMax)
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
                        ig.SetContinue();
                        return true;
                    }
                    else if (Dispatcher.Invoke(() => Main.PetGrid2.Tag) is IGraph ig2 && ig2.GraphInfo.Name == "pinch" && ig2.GraphInfo.Animat == AnimatType.B_Loop)
                    {
                        ig2.SetContinue();
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
                if (Core.Controller.EnableFunction && Core.Save.Strength >= 10 && Core.Save.Feeling < Core.Save.FeelingMax)
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
        /// <summary>
        /// 获取收藏的工作
        /// </summary>
        public List<Work> WorkStar()
        {
            List<Work> works = new List<Work>();
            foreach (var work in Core.Graph.GraphConfig.Works)
            {
                if (Set["work_star"].GetBool(work.Name))
                    works.Add(work);
            }
            return works;
        }
        public System.Windows.Controls.MenuItem WorkStarMenu;

        public void LevelUP(GameSave_VPet.LevelUpEventArgs args)
        {
            var gf = Core.Graph.FindGraph("levelup", GraphInfo.AnimatType.Single, GameSavesData.GameSave.Mode);
            if (gf != null)
            {
                Task.Run(() =>
                {
                    Thread.Sleep(5000);
                    Main.Display(gf, Main.DisplayToNomal);
                });
            }
            if (args.IsLevelMaxUp)
            {//告知用户上限等级上升
                Task.Run(() =>
                {
                    Thread.Sleep(5000);
                    Dispatcher.Invoke(() =>
                    {
                        Main.Say("邦邦咔邦,{0}等级突破了!".Translate(Name));
                        MessageBoxX.Show("系统提示\n您的桌宠等级已经突破\nLv{0}→LV{1} x{2}\n已突破为尊贵的x{3}阶".Translate(
                            1000 + args.BeforeLevelMax * 100, 100 * GameSavesData.GameSave.LevelMax, GameSavesData.GameSave.LevelMax),
                            "桌宠等级突破".Translate());
                    });
                });
            }
        }

        public void CheckGalleryUnlock()
        {
            var ps = Photos.FindAll(x => !x.IsUnlock && !x.UnlockAble.SellBoth && x.UnlockAble.Check(GameSavesData));
            if (ps.Count == 0) return;
            StringBuilder sb = new StringBuilder();
            foreach (Photo p in ps)
            {
                sb.Append(", ");
                p.Unlock(this);
                sb.Append(p.TranslateName);
            }
            Dispatcher.Invoke(() =>
            NoticeBox.Show(string.Concat(sb.ToString().AsSpan(2), "\n", "以上照片已解锁".Translate()), "新的照片已解锁".Translate()
            , Panuon.WPF.UI.MessageBoxIcon.Info, true, 5000));
        }
        static readonly DateTime StartDate = new(2023, 8, 14, 0, 0, 0, DateTimeKind.Utc);
        static int authheycache;
        static DateTime GetDateFromAuthKey(int authKey)
        {
            // 从验证键中解析出小时数
            int hoursSince2020 = authKey / 10000;

            // 计算日期和时间
            DateTime date = StartDate.AddHours(hoursSince2020);

            return date;
        }
        public async Task<int> GenerateAuthKey()
        {
            if (!IsSteamUser)
                return 0;

            bool genck = false;
            long steamId = (long)SteamClient.SteamId.Value;
        gencheck:
            if (authheycache != 0)
            {
                DateTime dt = GetDateFromAuthKey(authheycache);
                if (!(dt > DateTime.UtcNow.AddDays(1) || dt < DateTime.UtcNow.AddHours(-2)))
                {
                    return authheycache;
                }
            }
            Leaderboard? leaderboard = await SteamUserStats.FindLeaderboardAsync("chatgpt_auth");
            if (!leaderboard.HasValue)
            {
                return 0;
            }
            else
            {
                var lb = leaderboard.Value;
                LeaderboardEntry[] key = await lb.GetScoresAroundUserAsync(0, 0);
                if (key == null || key.Length == 0 || genck)
                {
                    int hoursSince2020 = (int)(DateTime.UtcNow - StartDate).TotalHours;
                    int lastFourDigits = (int)(steamId % 10000);
                    authheycache = hoursSince2020 * 10000 + lastFourDigits;
                    await leaderboard?.ReplaceScore(authheycache);
                    return authheycache;
                }
                else
                {
                    authheycache = key.First().Score;
                    genck = true;
                    goto gencheck;
                }

            }
        }
    }
}
