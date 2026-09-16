using Avalonia.Controls;
using Avalonia.Platform;
using LinePutScript;
using LinePutScript.Converter;
using LinePutScript.Localization;
using System;
using VPet_Simulator.Core.MutiPlatform.Graph;
using static VPet_Simulator.Windows.Interface.Photo.UnlockCondition;
using static VPet_Simulator.Core.MutiPlatform.GraphHelper;
using Steamworks;
using Steamworks.Data;
using System.IO;
using System.Text;
using System.Net;
using LinePutScript.Dictionary;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VPet_Simulator.Core;
using VPet_Simulator.Core.MutiPlatform;
using VPet_Simulator.Core.MutiPlatform.Display;
using VPet_Simulator.Core.MutiPlatform.Display.Shell;
using VPet_Simulator.Unified.Services;
using VPet_Simulator.Windows.Interface;
using static VPet_Simulator.Core.GraphInfo;

namespace VPet_Simulator.MutiPlatform;

/// <summary>
/// 桌宠主窗口: 说话、投喂、背包、菜单、托盘、自定按钮
/// </summary>
/// 对应 Windows 版 VPet-Simulator.Windows/MainWindow.cs, 各段逐项对应 (GetClickText / lowStrength /
/// TakeItem / ItemsAdd / LoadDIY / RunDIY / WorkStar / 菜单与托盘). 各项打开的窗口按移植进度逐个换成
/// 同名的 winXxx.
public partial class MainWindow
{
    /// <summary>
    /// 获得自动点击的文本
    /// </summary>
    /// <returns>说话内容</returns>
    public ClickText? GetClickText()
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
        switch (Core.Save!.Mode)
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

    /// <summary>
    /// 挂上点击说话和低状态说话
    /// </summary>
    private void LoadTalk(Main m)
    {
        m.DefaultClickAction = () =>
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
                            GameSavesData.Statistics![(gint)"stat_say_exp_p"]++;
                        }
                        else
                            GameSavesData.Statistics![(gint)"stat_say_exp_d"]++;
                    }
                    if (rt.Likability != 0)
                    {
                        if (rt.Likability > 0)
                            GameSavesData.Statistics![(gint)"stat_say_like_p"]++;
                        else
                            GameSavesData.Statistics![(gint)"stat_say_like_d"]++;
                    }
                    if (rt.Money != 0)
                    {
                        if (rt.Money > 0)
                            GameSavesData.Statistics![(gint)"stat_say_money_p"]++;
                        else
                            GameSavesData.Statistics![(gint)"stat_say_money_d"]++;
                    }
                    Main.Core.Save!.EatFood(rt);
                    Main.Core.Save!.Money += rt.Money;
                    Main.SayRnd(rt.TranslateTextConvert(Main), desc: rt.FoodToDescription());
                }
            }
        };
        m.PlayVoiceVolume = Set.VoiceVolume;
        m.FunctionSpendHandle += lowStrength;
    }

    int lowstrengthAskCountFood = 20;
    int lowstrengthAskCountDrink = 20;
    private void lowStrength()
    {
        var sm = Core.Save!.StrengthMax;
        var sm75 = sm * PurchaseRules.LowRate;
        //外层这个余额判断不能省: 开着自动购买但钱不够时, 桌宠还是要照常喊饿
        if (Set.AutoBuy && Core.Save!.Money >= PurchaseRules.AutoBuyMinMoney)
        {
            //买什么、预算多少、看得上哪些食物, 判断都在共享后端里
            var need = PurchaseRules.WhatToBuy(
                Core.Save!.StrengthFood + Core.Save!.StoreStrengthFood,
                Core.Save!.StrengthDrink + Core.Save!.StoreStrengthDrink,
                sm, Core.Save!.Feeling, Core.Save!.FeelingMax, Core.Save!.Money, Set.AutoGift);
            if (need != PurchaseRules.AutoBuyNeed.None)
            {
                var havemoney = PurchaseRules.AutoBuyBudget(Core.Save!.Money);
                List<Food> food = Foods.FindAll(x => PurchaseRules.IsAutoBuyCandidate(
                    x.Price, x.Health, x.Exp, x.Likability, havemoney, x.IsOverLoad()));

                switch (need)
                {
                    case PurchaseRules.AutoBuyNeed.Meal:
                        food = food.FindAll(x => x.Type == Food.FoodType.Meal && x.StrengthFood > PurchaseRules.MealThreshold(sm));
                        break;
                    case PurchaseRules.AutoBuyNeed.Drink:
                        food = food.FindAll(x => x.Type == Food.FoodType.Drink && x.StrengthDrink > PurchaseRules.DrinkThreshold(sm));
                        break;
                    case PurchaseRules.AutoBuyNeed.Gift:
                        food = food.FindAll(x => x.Type == Food.FoodType.Gift && x.Feeling > PurchaseRules.GiftThreshold(Core.Save!.FeelingMax));
                        break;
                    case PurchaseRules.AutoBuyNeed.Snack:
                        // 没开自动购买礼物的, 买零食能加点是一点
                        food = food.FindAll(x => x.Type == Food.FoodType.Snack && x.Feeling > PurchaseRules.SnackThreshold(Core.Save!.FeelingMax));
                        break;
                }
                if (food.Count == 0)
                    return;

                var item = food[Function.Rnd.Next(food.Count)];
                Core.Save!.Money -= PurchaseRules.AutoBuyCost(item.Price);
                switch (need)
                {
                    case PurchaseRules.AutoBuyNeed.Meal:
                        TakeItemHandle(item, 1, "autofood");
                        break;
                    case PurchaseRules.AutoBuyNeed.Drink:
                        TakeItemHandle(item, 1, "autodrink");
                        break;
                    default:
                        TakeItemHandle(item, 1, "autofeel");
                        break;
                }
                TakeItem(item);
                if (need == PurchaseRules.AutoBuyNeed.Meal || need == PurchaseRules.AutoBuyNeed.Drink)
                    GameSavesData.Statistics![(gint)"stat_autobuy"]++;
                else
                    GameSavesData.Statistics![(gint)"stat_autogift"]++;
                Main.Display(item.GetGraph(), item.ImageSource, Main.DisplayToNomal);
            }
        }
        else if (Core.Save!.Mode == IGameSave.ModeType.Happy || Core.Save!.Mode == IGameSave.ModeType.Nomal)
        {
            if (Core.Save!.StrengthFood < sm75 && Function.Rnd.Next(lowstrengthAskCountFood--) == 0)
            {
                lowstrengthAskCountFood = Set.InteractionCycle;
                var like = Core.Save!.Likability < 40 ? 0 : (Core.Save!.Likability < 70 ? 1 : (Core.Save!.Likability < 100 ? 2 : 3));
                var txt = LowFoodText.FindAll(x => x.Mode == LowText.ModeType.H && (int)x.Like <= like);
                if (txt.Count != 0)
                    if (Core.Save!.StrengthFood > sm * 0.60)
                    {
                        txt = txt.FindAll(x => x.Strength == LowText.StrengthType.L);
                        if (txt.Count != 0)
                            Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateTextConvert(Main));
                    }
                    else if (Core.Save!.StrengthFood > sm * 0.40)
                    {
                        txt = txt.FindAll(x => x.Strength == LowText.StrengthType.M);
                        if (txt.Count != 0)
                            Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateTextConvert(Main));
                    }
                    else
                    {
                        txt = txt.FindAll(x => x.Strength == LowText.StrengthType.S);
                        if (txt.Count != 0)
                            Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateTextConvert(Main));
                    }
                Main.DisplayStopForce(() => Main.Display(GraphType.Switch_Hunger, AnimatType.Single, Main.DisplayToNomal));
                return;
            }
            if (Core.Save!.StrengthDrink < sm75 && Function.Rnd.Next(lowstrengthAskCountDrink--) == 0)
            {
                lowstrengthAskCountDrink = Set.InteractionCycle;
                var like = Core.Save!.Likability < 40 ? 0 : (Core.Save!.Likability < 70 ? 1 : (Core.Save!.Likability < 100 ? 2 : 3));
                var txt = LowDrinkText.FindAll(x => x.Mode == LowText.ModeType.H && (int)x.Like <= like);
                if (txt.Count != 0)
                    if (Core.Save!.StrengthDrink > sm * 0.60)
                    {
                        txt = txt.FindAll(x => x.Strength == LowText.StrengthType.L);
                        if (txt.Count != 0)
                            Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateTextConvert(Main));
                    }
                    else if (Core.Save!.StrengthDrink > sm * 0.40)
                    {
                        txt = txt.FindAll(x => x.Strength == LowText.StrengthType.M);
                        if (txt.Count != 0)
                            Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateTextConvert(Main));
                    }
                    else
                    {
                        txt = txt.FindAll(x => x.Strength == LowText.StrengthType.S);
                        if (txt.Count != 0)
                            Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateTextConvert(Main));
                    }
                Main.DisplayStopForce(() => Main.Display(GraphType.Switch_Thirsty, AnimatType.Single, Main.DisplayToNomal));
                return;
            }
        }
        else
        {
            var sm20 = sm * 0.20;
            if (Core.Save!.StrengthFood < sm * 0.60 && Function.Rnd.Next(lowstrengthAskCountFood--) == 0)
            {
                lowstrengthAskCountFood = Set.InteractionCycle;
                var like = Core.Save!.Likability < 40 ? 0 : (Core.Save!.Likability < 70 ? 1 : (Core.Save!.Likability < 100 ? 2 : 3));
                var txt = LowFoodText.FindAll(x => x.Mode == LowText.ModeType.L && (int)x.Like < like);
                if (Core.Save!.StrengthFood > sm * 0.40)
                {
                    txt = txt.FindAll(x => x.Strength == LowText.StrengthType.L);
                    if (txt.Count != 0)
                        Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateTextConvert(Main));
                }
                else if (Core.Save!.StrengthFood > sm20)
                {
                    txt = txt.FindAll(x => x.Strength == LowText.StrengthType.M);
                    if (txt.Count != 0)
                        Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateTextConvert(Main));
                }
                else
                {
                    txt = txt.FindAll(x => x.Strength == LowText.StrengthType.S);
                    if (txt.Count != 0)
                        Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateTextConvert(Main));
                }
                Main.DisplayStopForce(() => Main.Display(GraphType.Switch_Hunger, AnimatType.Single, Main.DisplayToNomal));
                return;
            }
            if (Core.Save!.StrengthDrink < sm * 0.60 && Function.Rnd.Next(lowstrengthAskCountDrink--) == 0)
            {
                lowstrengthAskCountDrink = Set.InteractionCycle;
                var like = Core.Save!.Likability < 40 ? 0 : (Core.Save!.Likability < 70 ? 1 : (Core.Save!.Likability < 100 ? 2 : 3));
                var txt = LowDrinkText.FindAll(x => x.Mode == LowText.ModeType.L && (int)x.Like < like);
                if (Core.Save!.StrengthDrink > sm * 0.40)
                {
                    txt = txt.FindAll(x => x.Strength == LowText.StrengthType.L);
                    if (txt.Count != 0)
                        Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateTextConvert(Main));
                }
                else if (Core.Save!.StrengthDrink > sm20)
                {
                    txt = txt.FindAll(x => x.Strength == LowText.StrengthType.M);
                    if (txt.Count != 0)
                        Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateTextConvert(Main));
                }
                else
                {
                    txt = txt.FindAll(x => x.Strength == LowText.StrengthType.S);
                    if (txt.Count != 0)
                        Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateTextConvert(Main));
                }
                Main.DisplayStopForce(() => Main.Display(GraphType.Switch_Thirsty, AnimatType.Single, Main.DisplayToNomal));
                return;
            }
        }
    }

    /// <summary>
    /// 呼叫事件 Event_TakeItemHandle
    /// </summary>
    /// <param name="item">物品</param>
    /// <param name="count">个数</param>
    /// <param name="from">来源</param>
    public void TakeItemHandle(Food item, int count, string from)
    {
        Event_TakeItemHandle?.Invoke(item, count, from);
    }
    /// <summary>
    /// 使用/食用物品 (不包括显示动画)
    /// </summary>
    /// <param name="item">物品</param>
    public void TakeItem(Food item)
    {
        //吃腻度的算式在共享后端里
        Main.LastInteractionTime = DateTime.Now;
        LastTakeItemTime = DateTime.Now;
        DateTime now = DateTime.Now;
        var buytime = GameSavesData[FeedingRules.BuyTimeLineName];
        double eattimes = FeedingRules.RemainingBoredom(buytime.GetDateTime(item.Name, now), now);
        double eatuseps = FeedingRules.Effectiveness(eattimes, item.Type == Food.FoodType.Gift);
        //开始加点
        Core.Save!.EatFood(item, eatuseps);
        //吃腻了
        eattimes += FeedingRules.AddedBoredom(item.Likability, item.Feeling);
        buytime.SetDateTime(item.Name, now.AddHours(eattimes));
        //通知
        item.LoadEatTimeSource(this);
        item.NotifyOfPropertyChange("Description");

        //统计
        if (GameSavesData.Statistics == null)
            return;
        GameSavesData.Statistics[(gint)FeedingRules.BuyTimesStat]++;
        GameSavesData.Statistics[(gint)FeedingRules.BuyCountStat(item.Name)]++;
        GameSavesData.Statistics[(gdbe)FeedingRules.TotalSpendStat] += item.Price;
        var spend = FeedingRules.SpendStat((FeedingRules.FoodKind)(int)item.Type);
        if (spend != null)
            GameSavesData.Statistics[(gdbe)spend] += item.Price;
        if (item.Type == Food.FoodType.Drug)
            GameSavesData.Statistics[(gdbe)FeedingRules.DrugExpStat] += item.Exp;
        else if (item.Type == Food.FoodType.Gift)
            GameSavesData.Statistics[(gdbe)FeedingRules.GiftLikeStat] += item.Likability;

        Event_TakeItem?.Invoke(item);
    }

    /// <summary>
    /// 往背包里加物品 (同名的合并数量)
    /// </summary>
    public void ItemsAdd(Item item)
    {
        var sameitem = Items.Find(x => x.Name == item.Name);
        if (sameitem != null)
        {
            sameitem.Count += item.Count;
        }
        else
        {
            Items.Add(item);
        }
    }


    /// <summary>
    /// 收藏的工作菜单
    /// </summary>
    public MenuItem WorkStarMenu = null!;

    /// <summary>
    /// 托盘图标
    /// </summary>
    internal TrayIcon notifyIcon = null!;
    private NativeMenuItem NotifyIcon_HitThrough = null!;
    internal NativeMenuItem NotifyIcon_TopMost = null!;

    /// <summary>
    /// 是否鼠标穿透
    /// </summary>
    public bool HitThrough { get; private set; }

    /// <summary>
    /// 加载工具栏菜单与托盘
    /// </summary>
    /// 与 Windows 版 MainWindow.cs 的 GameLoad 后半段逐项对应
    private void LoadToolBarMenu(Main m)
    {
        var toolBar = m.ToolBar;
        toolBar.LoadClean();
        m.WorkList(out List<GraphHelper.Work> ws, out List<GraphHelper.Work> ss, out List<GraphHelper.Work> ps);

        //日程表加载
        ScheduleTask = new ScheduleTask(this);

        if (ws.Count == 0)
        {
            toolBar.MenuWork.IsVisible = false;
        }
        else
        {
            toolBar.MenuWork.DoubleTapped += (x, y) =>
            {
                toolBar.Hide();
                ShowWorkMenu(GraphHelper.Work.WorkType.Work);
            };
            toolBar.MenuWork.Click += (x, y) =>
            {
                toolBar.Hide();
                if (toolBar.MenuWork.Items.Count == 0)
                    ShowWorkMenu(GraphHelper.Work.WorkType.Work);
            };
        }
        if (ss.Count == 0)
        {
            toolBar.MenuStudy.IsVisible = false;
        }
        else
        {
            toolBar.MenuStudy.DoubleTapped += (x, y) =>
            {
                toolBar.Hide();
                ShowWorkMenu(GraphHelper.Work.WorkType.Study);
            };
            toolBar.MenuStudy.Click += (x, y) =>
            {
                toolBar.Hide();
                if (toolBar.MenuStudy.Items.Count == 0) ShowWorkMenu(GraphHelper.Work.WorkType.Study);
            };
        }
        if (ps.Count == 0)
        {
            toolBar.MenuPlay.IsVisible = false;
        }
        else
        {
            toolBar.MenuPlay.DoubleTapped += (x, y) =>
            {
                toolBar.Hide();
                ShowWorkMenu(GraphHelper.Work.WorkType.Play);
            };
            toolBar.MenuPlay.Click += (x, y) =>
            {
                toolBar.Hide();
                if (toolBar.MenuPlay.Items.Count == 0) ShowWorkMenu(GraphHelper.Work.WorkType.Play);
            };
        }
        WorkStarMenu = new MenuItem()
        {
            Header = "收藏".Translate(),
        };
        foreach (var w in WorkStar())
        {
            var mi = new MenuItem()
            {
                Header = w.NameTrans
            };
            mi.Click += (s, e) => toolBar.StartWork(w.Double(Set["workmenu"].GetInt("double_" + w.Name, 1)));
            WorkStarMenu.Items.Add(mi);
        }
        toolBar.MenuInteract.Items.Add(WorkStarMenu);

        var mod = new MenuItem()
        {
            Header = "MOD管理".Translate(),
        };
        mod.Click += (x, y) =>
        {
            toolBar.Hide();
            ShowSetting(5);
        };
        toolBar.MenuMODConfig.Items.Add(mod);

        toolBar.AddMenuButton(ToolBar.MenuType.Setting, "退出桌宠".Translate(), () => { toolBar.Hide(); Close(); });
        if (Set.DeBug)
            toolBar.AddMenuButton(ToolBar.MenuType.Setting, "开发控制台".Translate(), () => { toolBar.Hide(); ShowConsole(); });
        toolBar.AddMenuButton(ToolBar.MenuType.Setting, "照片图库".Translate(), ShowGallery);
        toolBar.AddMenuButton(ToolBar.MenuType.Setting, "操作教程".Translate(), () =>
        {
            if (LocalizeCore.CurrentCulture == "zh-Hans")
                ExtensionFunction.StartURL("https://wiki.exlb.net/vpet/tutorial");
            else if (LocalizeCore.CurrentCulture == "zh-Hant")
                ExtensionFunction.StartURL("https://wiki.exlb.net/zh-hant/vpet/tutorial");
            else
                ExtensionFunction.StartURL("https://wiki.exlb.net/en/vpet/tutorial");
        });
        toolBar.AddMenuButton(ToolBar.MenuType.Setting, "反馈中心".Translate(), () => { toolBar.Hide(); ShowReport(); });
        toolBar.AddMenuButton(ToolBar.MenuType.Setting, "设置面板".Translate(), () =>
        {
            toolBar.Hide();
            ShowSetting();
        });

        toolBar.AddMenuButton(ToolBar.MenuType.Feed, "吃饭".Translate(), () =>
        {
            winBetterBuy!.Show(Food.FoodType.Meal);
        });
        toolBar.AddMenuButton(ToolBar.MenuType.Feed, "喝水".Translate(), () =>
        {
            winBetterBuy!.Show(Food.FoodType.Drink);
        });
        toolBar.AddMenuButton(ToolBar.MenuType.Feed, "收藏".Translate(), () =>
        {
            winBetterBuy!.Show(Food.FoodType.Star);
        });
        toolBar.AddMenuButton(ToolBar.MenuType.Feed, "药品".Translate(), () =>
        {
            winBetterBuy!.Show(Food.FoodType.Drug);
        });
        toolBar.AddMenuButton(ToolBar.MenuType.Feed, "礼品".Translate(), () =>
        {
            winBetterBuy!.Show(Food.FoodType.Gift);
        });
        toolBar.AddMenuButton(ToolBar.MenuType.Feed, "背包".Translate(), () =>
        {
            if (winInventory != null && !winInventory.IsClosed)
                winInventory.Show();
            else
            {
                winInventory = new winInventory(this);
                winInventory.Show();
            }
        });

        Main.WorkCheck = WorkCheck;

        //加载图标
        notifyIcon = new TrayIcon();
        notifyIcon.ToolTipText = "虚拟桌宠模拟器".Translate() + PrefixSave;
        NativeMenu m_menu;

        m_menu = new NativeMenu();
        m_menu.Opening += (x, y) => { if (GameSavesData.Statistics != null) GameSavesData.Statistics[(gint)"stat_menu_pop"]++; };
        NotifyIcon_HitThrough = new NativeMenuItem("鼠标穿透".Translate())
        {
            ToggleType = MenuItemToggleType.CheckBox,
            IsChecked = HitThrough
        };
        NotifyIcon_HitThrough.Click += (x, y) => { SetTransparentHitThrough(); };
        m_menu.Items.Add(NotifyIcon_HitThrough);
        NotifyIcon_TopMost = new NativeMenuItem("置于顶层".Translate())
        {
            ToggleType = MenuItemToggleType.CheckBox,
            IsChecked = Topmost
        };
        NotifyIcon_TopMost.Click += (x, y) =>
        {
            Topmost = NotifyIcon_TopMost.IsChecked;
        };
        m_menu.Items.Add(NotifyIcon_TopMost);
        var tutorial = new NativeMenuItem("操作教程".Translate());
        tutorial.Click += (x, y) =>
        {
            if (LocalizeCore.CurrentCulture == "zh-Hans")
                ExtensionFunction.StartURL("https://wiki.exlb.net/vpet/tutorial");
            else if (LocalizeCore.CurrentCulture == "zh-Hant")
                ExtensionFunction.StartURL("https://wiki.exlb.net/zh-hant/vpet/tutorial");
            else
                ExtensionFunction.StartURL("https://wiki.exlb.net/en/vpet/tutorial");
        };
        m_menu.Items.Add(tutorial);
        var reset = new NativeMenuItem("重置位置与状态".Translate());
        reset.Click += (x, y) =>
        {
            Main.CleanState();
            Main.DisplayToNomal();
            //跨平台: 没有 SystemParameters, 用主屏幕的工作区居中
            var screen = Screens.Primary?.WorkingArea;
            if (screen != null)
                Position = new Avalonia.PixelPoint(
                    screen.Value.X + (screen.Value.Width - (int)(Width * RenderScaling)) / 2,
                    screen.Value.Y + (screen.Value.Height - (int)(Height * RenderScaling)) / 2);
        };
        m_menu.Items.Add(reset);
        var report = new NativeMenuItem("反馈中心".Translate());
        report.Click += (x, y) => { ShowReport(); };
        m_menu.Items.Add(report);
        if (Set.DeBug)
        {
            var console = new NativeMenuItem("开发控制台".Translate());
            console.Click += (x, y) => { ShowConsole(); };
            m_menu.Items.Add(console);
        }
        var setting = new NativeMenuItem("设置面板".Translate());
        setting.Click += (x, y) =>
        {
            ShowSetting();
        };
        m_menu.Items.Add(setting);
        var restart = new NativeMenuItem("重启桌宠".Translate());
        restart.Click += (x, y) => Restart();
        m_menu.Items.Add(restart);
        var exit = new NativeMenuItem("退出桌宠".Translate());
        exit.Click += (x, y) => Close();
        m_menu.Items.Add(exit);

        LoadDIY();

        notifyIcon.Menu = m_menu;
        notifyIcon.Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://VPet-Simulator.MutiPlatform/vpeticon.ico")));
        notifyIcon.IsVisible = true;
        //跨平台: 托盘图标要挂到 Application 上才会显示, 多开时每只桌宠一个
        var icons = TrayIcon.GetIcons(Avalonia.Application.Current!) ?? new TrayIcons();
        icons.Add(notifyIcon);
        TrayIcon.SetIcons(Avalonia.Application.Current!, icons);
        Closed += (_, _) =>
        {
            notifyIcon.IsVisible = false;
            icons.Remove(notifyIcon);
            notifyIcon.Dispose();
        };
    }

    public bool WorkCheck(GraphHelper.Work work)
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
    /// 重载DIY按钮区域
    /// </summary>
    public void LoadDIY()
    {
        Main.ToolBar!.MenuDIY.Items.Clear();

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
                var menuItem = new MenuItem()
                {
                    Header = "桌宠多开".Translate(),
                };
                foreach (var win in list)
                {
                    var mo = new MenuItem()
                    {
                        Header = win.Translate(),
                    };
                    mo.Click += (s, e) =>
                    {
                        if (App.MainWindows.FirstOrDefault(x => x.PrefixSave.Trim('-') == win) == null)
                        {
                            new MainWindow(win, this).Show();
                        }
                        menuItem.Items.Remove(s!);
                    };
                    menuItem.Items.Add(mo);
                }
                Main.ToolBar.MenuDIY.Items.Add(menuItem);
            }
        }

        foreach (ISub sub in Set["diy"])
            Main.ToolBar.AddMenuButton(ToolBar.MenuType.DIY, sub.Name, () =>
            {
                Main.ToolBar.Hide();
                RunDIY(sub.Info);
            });

        //统一契约插件
        foreach (var uh in UnifiedHosts)
            try
            {
                uh.Plugin.LoadDIY();
            }
            catch (Exception e)
            {
                MessageBoxX.Show(e.ToString(), "由于插件引起的自定按钮加载错误".Translate() + '-' + uh.Plugin.PluginName);
            }
        Main.ToolBar.LoadDIY();
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
                //跨平台: 没有 WinForms 的 SendKeys, 用 Function/SendKeys.cs 里照它写法实现的那个 (只有 Windows 有)
                SendKeys.SendWait(content);
            }
            catch (Exception e)
            {
                MessageBoxX.Show("快捷键运行失败:无法运行指定内容".Translate() + '\n' + e.Message);
            }
        }
    }

    /// <summary>
    /// 获取收藏的工作
    /// </summary>
    public List<GraphHelper.Work> WorkStar()
    {
        List<GraphHelper.Work> works = new List<GraphHelper.Work>();
        foreach (var work in Core.Graph!.GraphConfig!.Works)
        {
            if (Set["work_star"].GetBool(work.Name))
                works.Add(work);
        }
        return works;
    }

    /// <summary>
    /// 设置点击穿透到后面透明的窗口
    /// </summary>
    /// Windows 上照 Windows 版做: 给窗口加 WS_EX_TRANSPARENT.
    /// Linux/macOS 的穿透 (X11 输入区域 / NSWindow.ignoresMouseEvents) 本期不做, 菜单项留着, 点了不生效.
    public void SetTransparentHitThrough()
    {
        HitThrough = !HitThrough;
        NotifyIcon_HitThrough.IsChecked = HitThrough;
        //跨平台: 鼠标穿透靠 Win32 的 WS_EX_TRANSPARENT, Linux/macOS 本期留空操作 (菜单项与状态照旧)
        var handle = OperatingSystem.IsWindows() ? (TryGetPlatformHandle()?.Handle ?? IntPtr.Zero) : IntPtr.Zero;
        if (HitThrough)
        {
            if (handle != IntPtr.Zero)
                Win32.User32.SetWindowLongPtr(handle, Win32.GetWindowLongFields.GWL_EXSTYLE,
                    (IntPtr)(int)((long)Win32.User32.GetWindowLongPtr(handle, Win32.GetWindowLongFields.GWL_EXSTYLE) | (long)Win32.ExtendedWindowStyles.WS_EX_TRANSPARENT));
            petHelper?.SetOpacity(false);
            if (Set.OpacityHitThrough)
                Opacity = Set.Opacity;
        }
        else
        {
            if (handle != IntPtr.Zero)
                Win32.User32.SetWindowLongPtr(handle, Win32.GetWindowLongFields.GWL_EXSTYLE,
                    (IntPtr)(int)((long)Win32.User32.GetWindowLongPtr(handle, Win32.GetWindowLongFields.GWL_EXSTYLE) & ~(long)Win32.ExtendedWindowStyles.WS_EX_TRANSPARENT));
            petHelper?.SetOpacity(true);
            if (Set.OpacityMain)
                Opacity = Set.Opacity;
            else
                Opacity = 1;
        }
    }

    public List<ITalkAPI> TalkAPI { get; } = new List<ITalkAPI>();
    /// <summary>
    /// 当前选择的对话框index
    /// </summary>
    public int TalkAPIIndex = -1;
    /// <summary>
    /// 当前对话框
    /// </summary>
    public ITalkAPI? TalkBoxCurr
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
            Main.ToolBar!.MainGrid.Children.Remove(TalkBox);
            TalkBox = null;
        }
        if (TalkAPIIndex == -1)
            return;
        Main.ToolBar!.MainGrid.Children.Remove(TalkAPI[TalkAPIIndex].This);
    }
    /// <summary>
    /// 加载自定义对话框
    /// </summary>
    public void LoadTalkDIY()
    {
        RemoveTalkBox();
        if (TalkAPIIndex == -1)
            return;
        Main.ToolBar!.MainGrid.Children.Add(TalkAPI[TalkAPIIndex].This);
    }

    /// <summary>
    /// 获得当前系统音乐播放音量
    /// </summary>
    /// 跨平台: Windows 版靠 NAudio 的 WASAPI 读默认输出设备的峰值, 别的平台没有对应物;
    /// 这里与 Windows 版"设备不支持"那条路径一样返回 -1, 音乐识别功能自然不触发
    public float AudioPlayingVolume()
    {
        return -1;
    }

    public void LoadPetHelper()
    {
        petHelper = new PetHelper(this);
        //跨平台: Windows 版是显示后用 GWL_HWNDPARENT 挂到桌宠窗口上, 这里直接以桌宠为属主显示
        petHelper.Show(this);
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
    private void Statistics_StatisticChanged(Statistics sender, string name, SetObject? value)
    {
        if (name.StartsWith("stat_") && value != null)
        {
            SteamUserStats.SetStat(name, Convert.ToInt32(value.Value));
        }
    }
    /// <summary>
    /// 计算统计数据
    /// </summary>
    private void StatisticsCalHandle()
    {
        var stat = GameSavesData.Statistics;
        if (stat == null) return;
        var save = (IGameSave)Core.Save!;
        stat["stat_money"] = (SetObject)save.Money;
        stat["stat_level"] = save.Level;
        stat["stat_likability"] = save.Likability;

        stat[(gi64)"stat_total_time"] += (int)Set.LogicInterval;
        switch (Main.State)
        {
            case Main.WorkingState.Work:
                if (Main.NowWork?.Type == Work.WorkType.Work)
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
            if (Core.Save!.Mode == IGameSave.ModeType.Ill)
            {
                SteamFriends.SetRichPresence("steam_display", "#Status_Ill");
            }
            else
            {
                SteamFriends.SetRichPresence("mode", (Core.Save!.Mode.ToString() + "ly").Translate());
                switch (obj.State)
                {
                    case Main.WorkingState.Work:
                        if (obj.NowWork == null) break;
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

    /// <summary>
    /// 音乐检测器
    /// </summary>
    private void Handle_Music(Main obj)
    {
        if (MusicTimer.Enabled == false && Core.Graph!.FindGraphs("music", AnimatType.B_Loop, Core.Save!.Mode) != null &&
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
                    GameSavesData.Statistics![(gint)"stat_music"]++;
                    Main.Display(Core.Graph!.FindGraph("music", AnimatType.A_Start, Core.Save!.Mode), Display_Music);
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
                var mg = Core.Graph!.FindGraph("music", AnimatType.Single, Core.Save!.Mode);
                mg ??= Core.Graph!.FindGraph("music", AnimatType.B_Loop, Core.Save!.Mode);
                Main.Display(mg, Display_Music);
            }
            else
            {
                Main.Display(Core.Graph!.FindGraph("music", AnimatType.B_Loop, Core.Save!.Mode), Display_Music);
            }
        }
        else
        {
            Main.Display("music", AnimatType.C_End, Main.DisplayToNomal);
        }
    }
    private void MusicTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs? e)
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

    public System.Timers.Timer MusicTimer = null!;
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
        sb.AppendLine(System.Web.HttpUtility.UrlEncode(Core.Save!.ToLine().ToString() + Set.ToString()));
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

    private void like520()
    {
        var date = DateTime.Now.Day + DateTime.Now.Month * 100;
        var thisy77 = GetLunarDate(7, 7);
        if (date == 520 || date == 521 || date == 214 || date == (thisy77.Day + thisy77.Month * 100))
        {
            Task.Run(() =>
            {
                Thread.Sleep(52000);
                Main.Display("like520", AnimatType.Single, Main.DisplayNomal);
            });
        }
    }



    TextBlock tlvplus = null!;

    public event Action<IMPWindows>? MutiPlayerHandle;
    public void MutiPlayerStart(IMPWindows mp)
    {
        MutiPlayerHandle?.Invoke(mp);
    }

    
    private void MWUIHandle(Main main)
    {
        if (Main.ToolBar!.BdrPanel.IsVisible)
        {
            if (GameSavesData.GameSave.LevelMax != 0)
                tlvplus.Text = $" / {1000 + GameSavesData.GameSave.LevelMax * 100} x{GameSavesData.GameSave.LevelMax}";
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
    /// <summary>
    /// 事件:新的一天
    /// </summary>
    public event Action? Event_NewDay;
#if NewYear
    /// <summary>
    /// 新年说
    /// </summary>
    private void NewYearSay()
    {
        string sayny;
        switch (newday)
        {

            case 16:
                sayny = "白龙马，蹄朝西~马儿你跑快点啊~神马都是浮云~\n小马萝莉斯祝主人马年顺利，万事顺利，前途无阻，万马奔腾不停歇！".Translate();
                break;
            case 17:
                sayny = "马什么梅？什么冬梅？马冬什么？\n演员萝莉斯祝主人马年大智，学业进步，智商增加，考试满分！".Translate();
                break;
            case 18:
                sayny = "老马啊！！！哎！老马啊——！\n主播萝莉斯祝主人马年快乐，心情轻松快乐，烦恼统统飞走！".Translate();
                break;
            case 19:
                sayny = "我大意了啊没有闪，小主人你不讲武德！吃我闪电五连鞭！\n马掌门人萝莉斯祝主人马年安康，功夫有长进，技术有进步，能力会出众！".Translate();
                break;
            case 20:
                sayny = "哈基米曼波~马儿跳马儿跳~\n六星萝莉斯祝主人马年大运，抽卡出金一发入魂，装备掉落出红满仓！".Translate();
                break;
            case 21:
                sayny = "哎致命空枪，哎打腿没死，哎又空枪。我柜子动了我不玩了。\n游戏萝莉斯祝主人马年变强，枪枪爆头好运连连，把把第一永不马枪！".Translate();
                break;
            case 22:
                sayny = "待我高头大马，许你十里桃花！\n马猴烧酒萝莉斯祝主人马年马上有对象！千里姻缘一马牵，万水千山有马子！".Translate();
                break;
            default:
            case 23:
                sayny = "马喽的命也是命！\n打工人萝莉斯祝主人马年发财，返工赚大钱，今年一定发！马到成功！".Translate();
                break;
        }
        Main.SayRnd(sayny);
    }
#endif

    /// <summary>
    /// 显示捏脸情况
    /// </summary>
    public bool DisplayPinch()
    {
        if (Core.Graph!.FindGraphs("pinch", AnimatType.A_Start, Core.Save!.Mode) == null)
        {
            return false;
        }
        Main.CountNomal = 0;

        if (Core.Controller!.EnableFunction && Core.Save!.Strength >= 10 && Core.Save!.Feeling < Core.Save!.FeelingMax)
        {
            Core.Save!.StrengthChange(-2);
            Core.Save!.FeelingChange(1);
            Core.Save!.Mode = Core.Save!.CalMode();
            Main.LabelDisplayShowChangeNumber(LocalizeCore.Translate("体力-{0:f0} 心情+{1:f0}"), 2, 1);
        }
        if (Main.DisplayType.Name == "pinch")
        {
            if (Main.DisplayType.Animat == AnimatType.A_Start)
                return false;
            else if (Main.DisplayType.Animat == AnimatType.B_Loop)
                //跨平台: 动画接口叫 IAvaloniaGraph (Windows 的 IGraph 是 WPF 的)
                if (Dispatcher.Invoke(() => Main.PetGrid.Tag) is IAvaloniaGraph ig && ig.GraphInfo.Name == "pinch" && ig.GraphInfo.Animat == AnimatType.B_Loop)
                {
                    ig.SetContinue();
                    return true;
                }
                else if (Dispatcher.Invoke(() => Main.PetGrid2.Tag) is IAvaloniaGraph ig2 && ig2.GraphInfo.Name == "pinch" && ig2.GraphInfo.Animat == AnimatType.B_Loop)
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
            if (Core.Controller!.EnableFunction && Core.Save!.Strength >= 10 && Core.Save!.Feeling < Core.Save!.FeelingMax)
            {
                Core.Save!.StrengthChange(-2);
                Core.Save!.FeelingChange(1);
                Core.Save!.Mode = Core.Save!.CalMode();
                Main.LabelDisplayShowChangeNumber(LocalizeCore.Translate("体力-{0:f0} 心情+{1:f0}"), 2, 1);
            }
            Main.Display("pinch", AnimatType.B_Loop, DisplayPinch_loop);
        }
        else
        {
            Main.DisplayCEndtoNomal("pinch");
        }
    }

    public void CheckGalleryUnlock()
    {
        //要花钱的那种不自动解锁, 玩家得自己去图库里买 —— 判断在共享后端里
        var ps = Photos.FindAll(x => GalleryUnlockRules.ShouldAutoUnlock(
            x.IsUnlock, x.UnlockAble.SellBoth, x.UnlockAble.Check(GameSavesData)));
        if (ps.Count == 0) return;
        StringBuilder sb = new StringBuilder();
        foreach (Photo p in ps)
        {
            sb.Append(", ");
            p.Unlock(this);
            sb.Append(p.TranslateName);
        }
        ActivityLogs.Add(new ActivityLog("photo_unlock", sb.ToString().AsSpan(2).ToString()));
        Dispatcher.Invoke(() =>
        NoticeBox.Show(string.Concat(sb.ToString().AsSpan(2), "\n", "以上照片已解锁".Translate()), "新的照片已解锁".Translate()
        , 5000));
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

        while (true)
        {
            if (authheycache != 0)
            {
                DateTime dt = GetDateFromAuthKey(authheycache);
                if (!(dt > DateTime.UtcNow.AddDays(1) || dt < DateTime.UtcNow.AddHours(-2)))
                {
                    return authheycache;
                }
            }

            // 加 ConfigureAwait(false)
            Leaderboard? leaderboard = await SteamUserStats
                .FindLeaderboardAsync("chatgpt_auth")
                .ConfigureAwait(false);

            if (!leaderboard.HasValue)
                return 0;

            var lb = leaderboard.Value;

            // 加 ConfigureAwait(false)
            LeaderboardEntry[] key = await lb
                .GetScoresAroundUserAsync(0, 0)
                .ConfigureAwait(false);

            if (key == null || key.Length == 0 || genck)
            {
                int hoursSince2020 = (int)(DateTime.UtcNow - StartDate).TotalHours;
                authheycache = hoursSince2020 * 10000 + Function.Rnd.Next(10000);
                await lb.ReplaceScore(authheycache).ConfigureAwait(false);
                return authheycache;
            }
            else
            {
                authheycache = key.First().Score;
                genck = true;
            }
        }
    }
    public static object LogsLock = new object();
    private void ActivityLogs_WriteFile(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs? e)
    {
        if (e != null)
            if (e.NewItems != null)
            {
                List<string> sb = new List<string>();
                foreach (ActivityLog log in e.NewItems)
                {
                    sb.Add(log.ToString(Main));
                }
                string logPath = Path.Combine(ExtensionValue.BaseDirectory, $"Logs{PrefixSave}.txt");
                lock (LogsLock)
                {
                    if (File.Exists(logPath) && new FileInfo(logPath).Length > 1024 * 1024)
                    {
                        var allLines = File.ReadAllLines(logPath);
                        if (allLines.Length > 2000)
                        {
                            File.WriteAllLines(logPath, allLines.Skip(allLines.Length - 2000));
                        }
                    }
                    File.AppendAllLines(logPath, sb);
                }
            }
    }


    private void everydaygift()
    {
        //一天一份的判断在共享后端里
        var giftLine = Set[PurchaseRules.DailyGiftLineName];
        if (!PurchaseRules.ShouldGiveDailyGift(giftLine[(gint)PurchaseRules.DailyGiftSubName], DateTime.Now))
        {
            return;
        }
        giftLine[(gint)PurchaseRules.DailyGiftSubName] = DateTime.Now.DayOfYear;
        var itm = new Item
        {
            Name = "每日礼包",
            Desc = "物品系统附赠的每日礼包, 打开后会获得3个随机物品. 教程还送礼物, 萝莉丝真大方.".Translate(),
            ItemType = "Mail",
            Price = 15,
        };
        Dispatcher.Invoke(() => itm.LoadSource(this));
        ItemsAdd(itm);
    }

    public void LevelUP(GameSave_VPet.LevelUpEventArgs args)
    {
        var gf = Core.Graph!.FindGraph("levelup", GraphInfo.AnimatType.Single, GameSavesData.GameSave.Mode);
        if (gf != null)
        {
            Main.Say("邦邦咔邦,{0}等级突破了!".Translate(Name), "levelup", true);
        }
        if (args.IsLevelMaxUp)
        {//告知用户上限等级上升
            Task.Run(() =>
            {
                Thread.Sleep(5000);
                Dispatcher.Invoke(() =>
                {
                    MessageBoxX.Show("系统提示\n您的桌宠等级已经突破\nLv{0}→LV{1} x{2}\n已突破为尊贵的x{3}阶".Translate(
                        1000 + args.BeforeLevelMax * 100, 100 * GameSavesData.GameSave.LevelMax, GameSavesData.GameSave.LevelMax),
                        "桌宠等级突破".Translate());
                });
            });
        }
    }

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
            var totalhour = (int)(tmp.Statistics![(gint)"stat_total_time"] / 3600);//总计游玩时间/小时
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
        Items.Clear();
        foreach (var line in GameSavesData.Data.Assemblage.Where(x => x.Key.StartsWith("item")))
        {
            var itm = Item.CreateItem(this, line.Value);
            if (itm != null)
            {
                Dispatcher.Invoke(() => itm.LoadSource(this));
                ItemsAdd(itm);
            }
        }
        //临时修复下生日更新
        if (DateTime.Now.Month == 8 || DateTime.Now.Month == 9)
            foreach (var item in Items)
            {
                if (item.Name == "每日礼包")
                {
                    if (item.Count > 400)
                        item.Count = 365;
                }
                else if (item.Count > 32)
                    item.Count = 32;
                else if (item.Count <= 0)
                    item.Count = 1;
            }

        HashCheck = HashCheck;
        GameSavesData.GameSave.Event_LevelUp += LevelUP;
        return true;
    }

    public void ShowBetterBuy(Food.FoodType type)
    {
        winBetterBuy!.Show(type);
    }

    public void ShowWorkMenu(GraphHelper.Work.WorkType type)
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

    // ---- 各窗口的入口: 移植到同名 winXxx 之前先记日志 (其余在窗口批 B/C) ----
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
    public void ShowSetting(int page = -1)
    {
        if (page >= 0 && page <= 6)
            winSetting!.MainTab.SelectedIndex = page;
        winSetting!.Show();
    }
    public void ShowReport() => new winReport(this).Show();
    public void ShowConsole() => new winConsole(this).Show();
}
