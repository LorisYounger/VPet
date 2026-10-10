using Panuon.WPF.UI;
using System;
using System.Collections.Generic;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using static VPet_Simulator.Core.GraphHelper;
using static VPet_Simulator.Core.GraphInfo;
using static VPet_Simulator.Core.Main;
using Timer = System.Timers.Timer;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// ToolBar.xaml 的交互逻辑
    /// </summary>
    public partial class ToolBar : UserControl, IDisposable
    {
        Main m;
        public Timer CloseTimer;
        bool onFocus = false;
        public Timer closePanelTimer;

        public ToolBar(Main m)
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
            closePanelTimer = new Timer { AutoReset = false };
            closePanelTimer.Elapsed += ClosePanelTimer_Tick;
            m.TimeUIHandle += M_TimeUIHandle;
            //LoadWork();
            LoadDIY();
        }
        public void LoadClean()
        {
            MenuWork.Click -= MenuWork_Click;
            MenuWork.Visibility = Visibility.Visible;
            MenuStudy.Click -= MenuStudy_Click;
            MenuStudy.Visibility = Visibility.Visible;
            MenuPlay.Click -= MenuPlay_Click;


            MenuWork.Items.Clear();
            MenuStudy.Items.Clear();
            MenuPlay.Items.Clear();
        }
        public void StartWork(Work? w)
        {
            if (m.StartWork(w))
                Visibility = Visibility.Collapsed;
        }
        /// <summary>
        /// 加载默认工作
        /// </summary>
        public void LoadWork()
        {
            LoadClean();

            m.WorkList(out List<Work> ws, out List<Work> ss, out List<Work> ps);

            if (ws.Count == 0)
            {
                MenuWork.Visibility = Visibility.Collapsed;
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
                MenuStudy.Visibility = Visibility.Collapsed;
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
                MenuPlay.Visibility = Visibility.Collapsed;
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
        }
        /// <summary>
        /// 自动显示和隐藏DIY菜单
        /// </summary>
        public void LoadDIY()
        {
            if (MenuDIY.Items.Count > 0)
            {
                if (MenuDIY.Visibility == Visibility.Visible)
                    return;
                MenuDIY.Visibility = Visibility.Visible;
                ToolBarMenu.Tag = 5;
            }
            else
            {
                if (MenuDIY.Visibility == Visibility.Collapsed)
                    return;
                MenuDIY.Visibility = Visibility.Collapsed;
                ToolBarMenu.Tag = 4;
            }
        }
        private void MenuStudy_Click(object sender, RoutedEventArgs e)
        {
            StartWork(wstudy);
        }
        Work? wwork;
        Work? wstudy;
        Work? wplay;


        private void MenuWork_Click(object sender, RoutedEventArgs e)
        {
            StartWork(wwork);
        }
        private void MenuPlay_Click(object sender, RoutedEventArgs e)
        {
            StartWork(wplay);
        }
        /// <summary>
        /// 刷新显示UI
        /// </summary>
        public void M_TimeUIHandle(Main m)
        {
            if (BdrPanel.Visibility == Visibility.Visible)
            {
                var save = m.Core.Save!;
                // Tag 保存控件上次呈现的数值，未变化时不再格式化文本。
                void UpdateText(TextBlock text, double value, string format)
                {
                    if (text.Tag is double previous && previous == value) return;
                    text.Tag = value;
                    text.Text = string.Format(format, value);
                }
                void UpdateProgress(ProgressBar progress, double value, double maximum, double minimum = 0)
                {
                    if (progress.Maximum != maximum || progress.Minimum != minimum)
                    {
                        // Panuon 的百分比文字由 ValueChanged 刷新，仅在范围改变时保留清零。
                        progress.Value = 0;
                        progress.Maximum = maximum;
                        progress.Minimum = minimum;
                    }
                    if (progress.Value != value) progress.Value = value;
                }
                UpdateText(Tlv, save.Level, "Lv {0:0}");
                UpdateText(tExp, save.ExpBonus, "x{0:f2}");
                UpdateText(tMoney, save.Money, "$ {0:N2}");
                if (m.Core.Controller!.EnableFunction)
                {
                    till.Visibility = save.Mode == IGameSave.ModeType.Ill ? Visibility.Visible : Visibility.Collapsed;
                    tfun.Visibility = Visibility.Collapsed;
                }
                else
                {
                    till.Visibility = Visibility.Collapsed;
                    tfun.Visibility = Visibility.Visible;
                }
                double exp = save.Exp;
                UpdateProgress(pExp, exp, save.LevelUpNeed(), Math.Min(0, exp));
                double strengthMax = save.StrengthMax;
                UpdateProgress(pStrength, save.Strength, strengthMax);
                UpdateProgress(pFeeling, save.Feeling, save.FeelingMax);
                UpdateProgress(pStrengthFood, save.StrengthFood, strengthMax);
                UpdateProgress(pStrengthDrink, save.StrengthDrink, strengthMax);
                UpdateProgress(pStrengthFoodMax, Math.Min(100, (save.StrengthFood + save.StoreStrengthFood) / strengthMax * 100), 100);
                UpdateProgress(pStrengthDrinkMax, Math.Min(100, (save.StrengthDrink + save.StoreStrengthDrink) / strengthMax * 100), 100);

                UpdateText(tStrength, save.ChangeStrength, Math.Abs(save.ChangeStrength) > 1 ? "{0:f1}/t" : "{0:f2}/t");
                UpdateText(tFeeling, save.ChangeFeeling, Math.Abs(save.ChangeFeeling) > 1 ? "{0:f1}/t" : "{0:f2}/t");
                UpdateText(tStrengthDrink, save.ChangeStrengthDrink, Math.Abs(save.ChangeStrengthDrink) > 1 ? "{0:f1}/t" : "{0:f2}/t");
                UpdateText(tStrengthFood, save.ChangeStrengthFood, Math.Abs(save.ChangeStrengthFood) > 1 ? "{0:f1}/t" : "{0:f2}/t");
            }
        }

        private void ClosePanelTimer_Tick(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (BdrPanel.IsMouseOver || MenuPanel.IsMouseOver) return;
                BdrPanel.Visibility = Visibility.Collapsed;
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
                Dispatcher.Invoke(() => this.Visibility = Visibility.Collapsed);
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
                Panel.SetZIndex(this, m.UIGrid.Children.Count);
            }
            Visibility = Visibility.Visible;
            if (CloseTimer.Enabled)
                onFocus = true;
            else
                CloseTimer.Start();
        }

        private void UserControl_MouseEnter(object sender, MouseEventArgs e)
        {
            CloseTimer.Enabled = false;
        }

        private void UserControl_MouseLeave(object sender, MouseEventArgs e)
        {
            CloseTimer.Start();
        }

        private void MenuPanel_Click(object sender, RoutedEventArgs e)
        {
            m.Core.Controller!.ShowPanel();
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
                Header = displayName,
                HorizontalContentAlignment = HorizontalAlignment.Center
            };
            menuItem.Click += delegate
            {
                clickCallback?.Invoke();
            };
            switch (parentMenu)
            {
                case MenuType.Feed:
                    MenuFeed.Items.Add(menuItem);
                    break;
                case MenuType.Interact:
                    MenuInteract.Items.Add(menuItem);
                    break;
                case MenuType.DIY:
                    MenuDIY.Items.Add(menuItem);
                    LoadDIY();
                    break;
                case MenuType.Setting:
                    MenuSetting.Items.Add(menuItem);
                    break;
            }
        }

        private void PgbExperience_GeneratingPercentText(object sender, GeneratingPercentTextRoutedEventArgs e)
        {
            e.Text = $"{e.Value:f2} / {pExp.Maximum:f0}";
        }

        private void PgbStrength_GeneratingPercentText(object sender, GeneratingPercentTextRoutedEventArgs e)
        {
            e.Text = $"{e.Value:f2} / {pStrength.Maximum:f0}";
        }

        private void PgbSpirit_GeneratingPercentText(object sender, GeneratingPercentTextRoutedEventArgs e)
        {
            var progressBar = (ProgressBar)sender;
            progressBar.Foreground = GetForeground(e.Value / pFeeling.Maximum);
            e.Text = $"{e.Value:f2} / {pFeeling.Maximum:f0}";
        }

        private void PgbHunger_GeneratingPercentText(object sender, GeneratingPercentTextRoutedEventArgs e)
        {
            var progressBar = (ProgressBar)sender;
            progressBar.Foreground = GetForeground(e.Value / pStrength.Maximum);
            e.Text = $"{e.Value:f2} / {pStrength.Maximum:f0}";
        }

        private void PgbThirsty_GeneratingPercentText(object sender, GeneratingPercentTextRoutedEventArgs e)
        {
            var progressBar = (ProgressBar)sender;
            progressBar.Foreground = GetForeground(e.Value / pStrength.Maximum);
            e.Text = $"{e.Value:f2} / {pStrength.Maximum:f0}";
            //if (e.Value <= 20)
            //{
            //    tHearth.Visibility = Visibility.Visible;
            //}
        }

        private Brush? GetForeground(double value)
        {
            if (value >= .6)
            {
                return FindResource("SuccessProgressBarForeground") as Brush;
            }
            else if (value >= .3)
            {
                return FindResource("WarningProgressBarForeground") as Brush;
            }
            else
            {
                return FindResource("DangerProgressBarForeground") as Brush;
            }
        }
        /// <summary>
        /// MenuPanel显示事件
        /// </summary>
        public event Action? EventMenuPanelShow;

        private void MenuPanel_MouseEnter(object sender, MouseEventArgs e)
        {
            BdrPanel.Visibility = Visibility.Visible;
            M_TimeUIHandle(m);
            EventMenuPanelShow?.Invoke();
        }

        private void MenuPanel_MouseLeave(object sender, MouseEventArgs e)
        {
            closePanelTimer.Start();
        }

        public void Dispose()
        {
            CloseTimer.Dispose();
            closePanelTimer.Dispose();
        }

        private void Sleep_Click(object sender, RoutedEventArgs e)
        {
            if (m.State == Main.WorkingState.Sleep)
            {
                if (m.Core.Save!.Mode == IGameSave.ModeType.Ill)
                    return;
                m.State = WorkingState.Nomal;
                m.Display(GraphType.Sleep, AnimatType.C_End, m.DisplayNomal);
            }
            else if (m.State is Main.WorkingState.Nomal or Main.WorkingState.Travel)
                m.DisplaySleep(true);
            else
            {
                m.WorkTimer?.Stop(() => m.DisplaySleep(true), WorkTimer.FinishWorkInfo.StopReason.MenualStop);
            }
        }
    }
}
