using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Timers;
using Timer = System.Timers.Timer;
using Panuon.WPF.UI;
using System.Windows.Threading;
using LinePutScript;
using LinePutScript.Localization.WPF;
using static VPet_Simulator.Core.GraphInfo;
using System.Collections.Generic;
using static VPet_Simulator.Core.GraphHelper;
using System.ComponentModel;
using System.Reflection;
using static VPet_Simulator.Core.Main;
using static System.Net.Mime.MediaTypeNames;

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
        Timer closePanelTimer;

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
            closePanelTimer = new Timer();
            closePanelTimer.Elapsed += ClosePanelTimer_Tick;
            m.TimeUIHandle += M_TimeUIHandle;
            LoadWork();
        }

        public void LoadWork()
        {
            MenuWork.Click -= MenuWork_Click;
            MenuWork.Visibility = Visibility.Visible;
            MenuStudy.Click -= MenuStudy_Click;
            MenuStudy.Visibility = Visibility.Visible;
            MenuPlay.Click -= MenuPlay_Click;


            MenuWork.Items.Clear();
            MenuStudy.Items.Clear();
            MenuPlay.Items.Clear();
            List<Work> ws = new List<Work>();
            List<Work> ss = new List<Work>();
            List<Work> ps = new List<Work>();
            foreach (var w in m.Core.Graph.GraphConfig.Works)
            {
                switch (w.Type)
                {
                    case Work.WorkType.Study:
                        ss.Add(w);
                        break;
                    case Work.WorkType.Work:
                        ws.Add(w);
                        break;
                    case Work.WorkType.Play:
                        ps.Add(w);
                        break;
                }
            }
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

        private void MenuStudy_Click(object sender, RoutedEventArgs e)
        {
            StartWork(wstudy);
        }
        Work wwork;
        Work wstudy;
        Work wplay;
        /// <summary>
        /// 工作检测
        /// </summary>
        public Func<Work, bool> WorkCheck;
        public void StartWork(Work work)
        {
            if (!m.Core.Controller.EnableFunction || m.Core.Save.Mode != IGameSave.ModeType.Ill)
                if (!m.Core.Controller.EnableFunction || m.Core.Save.Level >= work.LevelLimit)
                    if (m.State == Main.WorkingState.Work && m.StateID == m.Core.Graph.GraphConfig.Works.IndexOf(work))
                        m.WorkTimer.Stop();
                    else
                    {
                        if (WorkCheck != null && !WorkCheck.Invoke(work))
                            return;
                        m.WorkTimer.Start(work);
                    }
                else
                    MessageBoxX.Show(LocalizeCore.Translate("您的桌宠等级不足{0}/{2}\n无法进行{1}", m.Core.Save.Level.ToString()
                        , work.NameTrans, work.LevelLimit), LocalizeCore.Translate("{0}取消", work.NameTrans));
            else
                MessageBoxX.Show(LocalizeCore.Translate("您的桌宠 {0} 生病啦,没法进行{1}", m.Core.Save.Name,
                  work.NameTrans), LocalizeCore.Translate("{0}取消", work.NameTrans));
            Visibility = Visibility.Collapsed;
        }
        private void MenuWork_Click(object sender, RoutedEventArgs e)
        {
            StartWork(wwork);
        }
        private void MenuPlay_Click(object sender, RoutedEventArgs e)
        {
            StartWork(wplay);
        }

        private void M_TimeUIHandle(Main m)
        {
            if (BdrPanel.Visibility == Visibility.Visible)
            {
                Tlv.Text = "Lv " + m.Core.Save.Level.ToString();
                tExp.Text = "x" + m.Core.Save.ExpBonus.ToString("f2");
                tMoney.Text = "$ " + m.Core.Save.Money.ToString("N2");
                if (m.Core.Controller.EnableFunction)
                {
                    till.Visibility = m.Core.Save.Mode == IGameSave.ModeType.Ill ? Visibility.Visible : Visibility.Collapsed;
                    tfun.Visibility = Visibility.Collapsed;
                }
                else
                {
                    till.Visibility = Visibility.Collapsed;
                    tfun.Visibility = Visibility.Visible;
                }
                var max = m.Core.Save.LevelUpNeed();
                pExp.Value = 0;
                pExp.Maximum = max;
                if (m.Core.Save.Exp < 0)
                {
                    pExp.Minimum = m.Core.Save.Exp;
                }
                else
                {
                    pExp.Minimum = 0;
                }
                pExp.Value = m.Core.Save.Exp;


                pStrengthFood.Value = 0;
                pStrengthDrink.Value = 0;
                pStrength.Value = 0;
                pFeeling.Value = 0;

                pStrengthFood.Maximum = m.Core.Save.StrengthMax;
                pStrengthDrink.Maximum = m.Core.Save.StrengthMax;
                pStrength.Maximum = m.Core.Save.StrengthMax;
                pFeeling.Maximum = m.Core.Save.FeelingMax;



                pStrength.Value = m.Core.Save.Strength;
                pFeeling.Value = m.Core.Save.Feeling;

                pStrengthFood.Value = m.Core.Save.StrengthFood;
                pStrengthDrink.Value = m.Core.Save.StrengthDrink;
                pStrengthFoodMax.Value = Math.Min(100, (m.Core.Save.StrengthFood + m.Core.Save.StoreStrengthFood) / m.Core.Save.StrengthMax * 100);
                pStrengthDrinkMax.Value = Math.Min(100, (m.Core.Save.StrengthDrink + m.Core.Save.StoreStrengthDrink) / m.Core.Save.StrengthMax * 100);

                if (Math.Abs(m.Core.Save.ChangeStrength) > 1)
                    tStrength.Text = $"{m.Core.Save.ChangeStrength:f1}/t";
                else
                    tStrength.Text = $"{m.Core.Save.ChangeStrength:f2}/t";
                if (Math.Abs(m.Core.Save.ChangeFeeling) > 1)
                    tFeeling.Text = $"{m.Core.Save.ChangeFeeling:f1}/t";
                else
                    tFeeling.Text = $"{m.Core.Save.ChangeFeeling:f2}/t";
                if (Math.Abs(m.Core.Save.ChangeStrengthDrink) > 1)
                    tStrengthDrink.Text = $"{m.Core.Save.ChangeStrengthDrink:f1}/t";
                else
                    tStrengthDrink.Text = $"{m.Core.Save.ChangeStrengthDrink:f2}/t";
                if (Math.Abs(m.Core.Save.ChangeStrengthFood) > 1)
                    tStrengthFood.Text = $"{m.Core.Save.ChangeStrengthFood:f1}/t";
                else
                    tStrengthFood.Text = $"{m.Core.Save.ChangeStrengthFood:f2}/t";

            }
        }

        private void ClosePanelTimer_Tick(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (BdrPanel.IsMouseOver
                    || MenuPanel.IsMouseOver)
                {
                    closePanelTimer.Stop();
                    return;
                }
                BdrPanel.Visibility = Visibility.Collapsed;
            });
        }

        private void Closetimer_Elapsed(object sender, ElapsedEventArgs e)
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
        public event Action EventShow;
        public void Show()
        {
            if (m.UIGrid.Children.IndexOf(this) != m.UIGrid.Children.Count - 1)
            {
                Panel.SetZIndex(this, m.UIGrid.Children.Count);
            }
            Visibility = Visibility.Visible;
            EventShow?.Invoke();
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
            m.Core.Controller.ShowPanel();
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

        private Brush GetForeground(double value)
        {
            if (value >= .8)
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
        public event Action EventMenuPanelShow;

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
            m = null;
            CloseTimer.Dispose();
            closePanelTimer.Dispose();
        }

        private void Sleep_Click(object sender, RoutedEventArgs e)
        {
            this.Visibility = Visibility.Collapsed;
            if (m.Core.Save.Mode != IGameSave.ModeType.Ill)
                if (m.State == Main.WorkingState.Sleep)
                {
                    m.State = WorkingState.Nomal;
                    m.Display(GraphType.Sleep, AnimatType.C_End, m.DisplayNomal);
                }
                else if (m.State == Main.WorkingState.Nomal)
                    m.DisplaySleep(true);
                else
                {
                    m.WorkTimer.Stop(() => m.DisplaySleep(true));
                }
        }
    }
}
