using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

using System;
using System.Collections.Generic;
using System.Timers;
using static VPet_Simulator.Core.GraphInfo;
using Timer = System.Timers.Timer;

namespace VPet_Simulator.Core.MutiPlatform.Display;

/// <summary>
/// 工具栏
/// </summary>
/// 对应 Windows 版 VPet-Simulator.Core/Display/ToolBar.xaml.cs.
/// 右键桌宠弹出, 4 秒无操作自动收起.
public partial class ToolBar : UserControl, IDisposable
{
    private readonly PetMain m;
    public Timer CloseTimer;
    bool onFocus = false;
    Timer closePanelTimer;

    /// <summary>
    /// 无参构造仅供 Avalonia 设计器使用
    /// </summary>
    public ToolBar() : this(null!)
    {
    }

    public ToolBar(PetMain m)
    {
        InitializeComponent();
        this.m = m;
        CloseTimer = new Timer()
        {
            Interval = 4000,
            AutoReset = false,
            Enabled = false
        };
        CloseTimer.Elapsed += Closetimer_Elapsed;
        closePanelTimer = new Timer();
        closePanelTimer.Elapsed += ClosePanelTimer_Tick;
        if (m != null)
            m.TimeUIHandle += M_TimeUIHandle;
        LoadDIY();
    }

    public void LoadClean()
    {
        MenuWork.Click -= MenuWork_Click;
        MenuWork.IsVisible = true;
        MenuStudy.Click -= MenuStudy_Click;
        MenuStudy.IsVisible = true;
        MenuPlay.Click -= MenuPlay_Click;
        // Windows 版这里漏了把"玩耍"改回可见, 重新加载工作列表时它一旦被隐藏就再也
        // 回不来了. 这边补上
        MenuPlay.IsVisible = true;

        MenuWork.Items.Clear();
        MenuStudy.Items.Clear();
        MenuPlay.Items.Clear();
    }

    /// <summary>
    /// 加载默认工作
    /// </summary>
    public void LoadWork()
    {
        LoadClean();

        m.WorkList(out List<GraphHelper.Work> ws, out List<GraphHelper.Work> ss, out List<GraphHelper.Work> ps);

        if (ws.Count == 0)
        {
            MenuWork.IsVisible = false;
        }
        else if (ws.Count == 1)
        {
            MenuWork.Click += MenuWork_Click;
            wwork = ws[0];
            MenuWork.Header = ws[0].NameTrans;
        }
        else
        {
            foreach (var w in ws)
            {
                var mi = new MenuItem()
                {
                    Header = w.NameTrans
                };
                mi.Click += (s, e) => StartWork(w);

                MenuWork.Items.Add(mi);
            }
        }
        if (ss.Count == 0)
        {
            MenuStudy.IsVisible = false;
        }
        else if (ss.Count == 1)
        {
            MenuStudy.Click += MenuStudy_Click;
            wstudy = ss[0];
            MenuStudy.Header = ss[0].NameTrans;
        }
        else
        {
            foreach (var w in ss)
            {
                var mi = new MenuItem()
                {
                    Header = w.NameTrans
                };
                mi.Click += (s, e) => StartWork(w);
                MenuStudy.Items.Add(mi);
            }
        }
        if (ps.Count == 0)
        {
            MenuPlay.IsVisible = false;
        }
        else if (ps.Count == 1)
        {
            MenuPlay.Click += MenuPlay_Click;
            wplay = ps[0];
            MenuPlay.Header = ps[0].NameTrans;
        }
        else
        {
            foreach (var w in ps)
            {
                var mi = new MenuItem()
                {
                    Header = w.NameTrans
                };
                mi.Click += (s, e) => StartWork(w);
                MenuPlay.Items.Add(mi);
            }
        }
        // 投喂菜单是宿主用 AddMenuButton 填的, 跨平台版还没有食物 MOD, 空着就先藏起来:
        // 留个点不开的空菜单只会让人以为功能坏了
        MenuFeed.IsVisible = MenuFeed.Items.Count > 0;
    }

    /// <summary>
    /// 自动显示和隐藏DIY菜单
    /// </summary>
    /// Windows 版这里还要顺便把 Menu.Tag 改成 4 或 5 来调列数,
    /// Avalonia 侧的 UniformGrid 按可见子项自动分列, 不用管.
    public void LoadDIY()
    {
        MenuDIY.IsVisible = MenuDIY.Items.Count > 0;
    }

    private void MenuStudy_Click(object? sender, RoutedEventArgs e)
    {
        StartWork(wstudy);
    }

    GraphHelper.Work? wwork;
    GraphHelper.Work? wstudy;
    GraphHelper.Work? wplay;

    private void MenuWork_Click(object? sender, RoutedEventArgs e)
    {
        StartWork(wwork);
    }

    private void MenuPlay_Click(object? sender, RoutedEventArgs e)
    {
        StartWork(wplay);
    }

    /// <summary>
    /// 开始工作
    /// </summary>
    /// 只有真的开始了才收起工具栏: 等级不够或者生病时开不了工, 这时候把菜单收掉
    /// 会让人没法接着挑别的
    public void StartWork(GraphHelper.Work? w)
    {
        if (m.StartWork(w))
            IsVisible = false;
    }

    /// <summary>
    /// 刷新显示UI
    /// </summary>
    public void M_TimeUIHandle(PetMain m)
    {
        if (BdrPanel.IsVisible)
        {
            Tlv.Text = "Lv " + m.Core.Save!.Level.ToString();
            tExp.Text = "x" + m.Core.Save!.ExpBonus.ToString("f2");
            tMoney.Text = "$ " + m.Core.Save!.Money.ToString("N2");
            if (m.Core.Controller!.EnableFunction)
            {
                till.IsVisible = m.Core.Save!.Mode == IGameSave.ModeType.Ill;
                tfun.IsVisible = false;
            }
            else
            {
                till.IsVisible = false;
                tfun.IsVisible = true;
            }
            var max = m.Core.Save!.LevelUpNeed();
            pExp.Value = 0;
            pExp.Maximum = max;
            if (m.Core.Save!.Exp < 0)
            {
                pExp.Minimum = m.Core.Save!.Exp;
            }
            else
            {
                pExp.Minimum = 0;
            }
            pExp.Value = m.Core.Save!.Exp;

            pStrengthFood.Value = 0;
            pStrengthDrink.Value = 0;
            pStrength.Value = 0;
            pFeeling.Value = 0;

            pStrengthFood.Maximum = m.Core.Save!.StrengthMax;
            pStrengthDrink.Maximum = m.Core.Save!.StrengthMax;
            pStrength.Maximum = m.Core.Save!.StrengthMax;
            pFeeling.Maximum = m.Core.Save!.FeelingMax;

            pStrength.Value = m.Core.Save!.Strength;
            pFeeling.Value = m.Core.Save!.Feeling;

            pStrengthFood.Value = m.Core.Save!.StrengthFood;
            pStrengthDrink.Value = m.Core.Save!.StrengthDrink;
            pStrengthFoodMax.Value = Math.Min(100, (m.Core.Save!.StrengthFood + m.Core.Save!.StoreStrengthFood) / m.Core.Save!.StrengthMax * 100);
            pStrengthDrinkMax.Value = Math.Min(100, (m.Core.Save!.StrengthDrink + m.Core.Save!.StoreStrengthDrink) / m.Core.Save!.StrengthMax * 100);

            // Windows 版这几段文字和配色是 Panuon 的 GeneratingPercentText 回调里做的,
            // Avalonia 没有对应机制, 直接在这里一起算掉
            tExpValue.Text = $"{pExp.Value:f2} / {pExp.Maximum:f0}";
            tStrengthValue.Text = $"{pStrength.Value:f2} / {pStrength.Maximum:f0}";
            tFeelingValue.Text = $"{pFeeling.Value:f2} / {pFeeling.Maximum:f0}";
            tStrengthFoodValue.Text = $"{pStrengthFood.Value:f2} / {pStrengthFood.Maximum:f0}";
            tStrengthDrinkValue.Text = $"{pStrengthDrink.Value:f2} / {pStrengthDrink.Maximum:f0}";
            pFeeling.Foreground = GetForeground(pFeeling.Value / pFeeling.Maximum);
            pStrengthFood.Foreground = GetForeground(pStrengthFood.Value / pStrengthFood.Maximum);
            pStrengthDrink.Foreground = GetForeground(pStrengthDrink.Value / pStrengthDrink.Maximum);

            if (Math.Abs(m.Core.Save!.ChangeStrength) > 1)
                tStrength.Text = $"{m.Core.Save!.ChangeStrength:f1}/t";
            else
                tStrength.Text = $"{m.Core.Save!.ChangeStrength:f2}/t";
            if (Math.Abs(m.Core.Save!.ChangeFeeling) > 1)
                tFeeling.Text = $"{m.Core.Save!.ChangeFeeling:f1}/t";
            else
                tFeeling.Text = $"{m.Core.Save!.ChangeFeeling:f2}/t";
            if (Math.Abs(m.Core.Save!.ChangeStrengthDrink) > 1)
                tStrengthDrink.Text = $"{m.Core.Save!.ChangeStrengthDrink:f1}/t";
            else
                tStrengthDrink.Text = $"{m.Core.Save!.ChangeStrengthDrink:f2}/t";
            if (Math.Abs(m.Core.Save!.ChangeStrengthFood) > 1)
                tStrengthFood.Text = $"{m.Core.Save!.ChangeStrengthFood:f1}/t";
            else
                tStrengthFood.Text = $"{m.Core.Save!.ChangeStrengthFood:f2}/t";
        }
    }

    private void ClosePanelTimer_Tick(object? sender, EventArgs e)
    {
        PetMain.RunOnUi(() =>
        {
            if (BdrPanel.IsPointerOver
                || MenuPanel.IsPointerOver)
            {
                closePanelTimer.Stop();
                return;
            }
            BdrPanel.IsVisible = false;
        });
    }

    private void Closetimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        if (onFocus)
        {
            onFocus = false;
            CloseTimer.Start();
        }
        else
            PetMain.RunOnUi(() => IsVisible = false);
    }

    /// <summary>
    /// ToolBar显示事件
    /// </summary>
    public event Action? EventShow;

    public void Show()
    {
        EventShow?.Invoke();
        if (m.UIGrid.Children.IndexOf(this) != m.UIGrid.Children.Count - 1)
        {
            ZIndex = m.UIGrid.Children.Count;
        }
        IsVisible = true;
        if (CloseTimer.Enabled)
            onFocus = true;
        else
            CloseTimer.Start();
    }

    private void UserControl_PointerEntered(object? sender, PointerEventArgs e)
    {
        CloseTimer.Enabled = false;
    }

    private void UserControl_PointerExited(object? sender, PointerEventArgs e)
    {
        CloseTimer.Start();
    }

    /// <summary>
    /// 窗口类型
    /// </summary>
    public enum MenuType
    {
        /// <summary>
        /// 投喂
        /// </summary>
        Feed,
        /// <summary>
        /// 互动
        /// </summary>
        Interact,
        /// <summary>
        /// 自定
        /// </summary>
        DIY,
        /// <summary>
        /// 设置
        /// </summary>
        Setting,
    }

    /// <summary>
    /// 添加按钮
    /// </summary>
    /// <param name="parentMenu">按钮位置</param>
    /// <param name="displayName">显示名称</param>
    /// <param name="clickCallback">功能</param>
    public void AddMenuButton(MenuType parentMenu,
        string displayName,
        Action clickCallback)
    {
        var menuItem = new MenuItem()
        {
            Header = displayName
        };
        menuItem.Click += delegate
        {
            clickCallback?.Invoke();
        };
        var parent = FindMenu(parentMenu);
        parent.Items.Add(menuItem);
        parent.IsVisible = true;
        if (parentMenu == MenuType.DIY)
            LoadDIY();
    }

    /// <summary>
    /// 添加带二级分组的按钮
    /// </summary>
    /// <param name="parentMenu">按钮位置</param>
    /// <param name="groupName">分组名称, 没有就新建一个</param>
    /// <param name="displayName">显示名称</param>
    /// <param name="clickCallback">功能</param>
    /// Windows 版的"投喂"点开是一整个商店窗口, 跨平台版没有那个窗口, 改成在菜单里
    /// 按食物类型分组直接列出来, 所以需要这一层.
    public void AddMenuButton(MenuType parentMenu,
        string groupName,
        string displayName,
        Action clickCallback)
    {
        var parent = FindMenu(parentMenu);
        MenuItem? group = null;
        foreach (var item in parent.Items)
        {
            if (item is MenuItem mi && (mi.Header as string) == groupName)
            {
                group = mi;
                break;
            }
        }
        if (group == null)
        {
            group = new MenuItem() { Header = groupName };
            parent.Items.Add(group);
            parent.IsVisible = true;
            if (parentMenu == MenuType.DIY)
                LoadDIY();
        }
        var menuItem = new MenuItem() { Header = displayName };
        menuItem.Click += delegate
        {
            clickCallback?.Invoke();
        };
        group.Items.Add(menuItem);
    }

    private MenuItem FindMenu(MenuType parentMenu) => parentMenu switch
    {
        MenuType.Feed => MenuFeed,
        MenuType.Interact => MenuInteract,
        MenuType.DIY => MenuDIY,
        _ => MenuSetting,
    };

    private IBrush? GetForeground(double value)
    {
        if (value >= .6)
        {
            return this.FindResource("SuccessProgressBarForeground") as IBrush;
        }
        else if (value >= .3)
        {
            return this.FindResource("WarningProgressBarForeground") as IBrush;
        }
        else
        {
            return this.FindResource("DangerProgressBarForeground") as IBrush;
        }
    }

    /// <summary>
    /// MenuPanel显示事件
    /// </summary>
    public event Action? EventMenuPanelShow;

    private void MenuPanel_PointerEntered(object? sender, PointerEventArgs e)
    {
        BdrPanel.IsVisible = true;
        M_TimeUIHandle(m);
        EventMenuPanelShow?.Invoke();
    }

    private void MenuPanel_PointerExited(object? sender, PointerEventArgs e)
    {
        closePanelTimer.Start();
    }

    public void Dispose()
    {
        CloseTimer.Dispose();
        closePanelTimer.Dispose();
        GC.SuppressFinalize(this);
    }

    private void Sleep_Click(object? sender, RoutedEventArgs e)
    {
        if (m.State == PetMain.WorkingState.Sleep)
        {
            if (m.Core.Save!.Mode == IGameSave.ModeType.Ill)
                return;
            m.State = PetMain.WorkingState.Nomal;
            m.Display(GraphType.Sleep, AnimatType.C_End, m.DisplayNomal);
        }
        else if (m.State == PetMain.WorkingState.Nomal)
            m.DisplaySleep(true);
        else
        {
            m.WorkTimer?.Stop(() => m.DisplaySleep(true), WorkTimer.FinishWorkInfo.StopReason.MenualStop);
        }
    }
}
