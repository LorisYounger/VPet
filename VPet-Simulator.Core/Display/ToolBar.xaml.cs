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
        }

        private void M_TimeUIHandle(Main m)
        {
            if (BdrPanel.Visibility == Visibility.Visible)
            {
                Tlv.Text = "Lv " + m.Core.Save.Level.ToString();
                tMoney.Text = "$ " + m.Core.Save.Money.ToString("N2");
                if (m.Core.Controller.EnableFunction)
                {
                    till.Visibility = m.Core.Save.Mode == GameSave.ModeType.Ill ? Visibility.Visible : Visibility.Collapsed;
                    tfun.Visibility = Visibility.Collapsed;
                }
                else
                {
                    till.Visibility = Visibility.Collapsed;
                    tfun.Visibility = Visibility.Visible;
                }
                pExp.Maximum = m.Core.Save.LevelUpNeed();
                pExp.Value = m.Core.Save.Exp;
                pStrength.Value = m.Core.Save.Strength;
                pFeeling.Value = m.Core.Save.Feeling;
                pStrengthFood.Value = m.Core.Save.StrengthFood;
                pStrengthDrink.Value = m.Core.Save.StrengthDrink;
                if (m.Core.Save.ChangeStrength < 1)
                    tStrength.Text = $"{m.Core.Save.ChangeStrength:f1}/t";
                else
                    tStrength.Text = $"{m.Core.Save.ChangeStrength:f2}/t";
                if (m.Core.Save.ChangeFeeling < 1)
                    tFeeling.Text = $"{m.Core.Save.ChangeFeeling:f1}/t";
                else
                    tFeeling.Text = $"{m.Core.Save.ChangeFeeling:f2}/t";
                if (m.Core.Save.ChangeStrengthDrink < 1)
                    tStrengthDrink.Text = $"{m.Core.Save.ChangeStrengthDrink:f1}/t";
                else
                    tStrengthDrink.Text = $"{m.Core.Save.ChangeStrengthDrink:f2}/t";
                if (m.Core.Save.ChangeStrengthFood < 1)
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

        public void Show()
        {
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
            e.Text = $"{e.Value:f2} / 100";
        }

        private void PgbSpirit_GeneratingPercentText(object sender, GeneratingPercentTextRoutedEventArgs e)
        {
            var progressBar = (ProgressBar)sender;
            progressBar.Foreground = GetForeground(e.Value);
            e.Text = $"{e.Value:f2} / 100";
        }

        private void PgbHunger_GeneratingPercentText(object sender, GeneratingPercentTextRoutedEventArgs e)
        {
            var progressBar = (ProgressBar)sender;
            progressBar.Foreground = GetForeground(e.Value);
            e.Text = $"{e.Value:f2} / 100";
        }

        private void PgbThirsty_GeneratingPercentText(object sender, GeneratingPercentTextRoutedEventArgs e)
        {
            var progressBar = (ProgressBar)sender;
            progressBar.Foreground = GetForeground(e.Value);
            e.Text = $"{e.Value:f2} / 100";
            //if (e.Value <= 20)
            //{
            //    tHearth.Visibility = Visibility.Visible;
            //}
        }

        private Brush GetForeground(double value)
        {
            if (value >= 80)
            {
                return FindResource("SuccessProgressBarForeground") as Brush;
            }
            else if (value >= 50)
            {
                return FindResource("WarningProgressBarForeground") as Brush;
            }
            else
            {
                return FindResource("DangerProgressBarForeground") as Brush;
            }
        }

        private void MenuPanel_MouseEnter(object sender, MouseEventArgs e)
        {
            BdrPanel.Visibility = Visibility.Visible;
            M_TimeUIHandle(m);
        }

        private void MenuPanel_MouseLeave(object sender, MouseEventArgs e)
        {
            closePanelTimer.Start();
        }

        private void MenuDIY_Click(object sender, RoutedEventArgs e)
        {
            //m.Say("您好,我是萝莉斯, 让我来帮您熟悉并掌握使用vos系统,成为永世流传的虚拟主播.");
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
            if (m.Core.Save.Mode != GameSave.ModeType.Ill)
                if (m.State == Main.WorkingState.Sleep)
                    m.Display(GraphCore.GraphType.Sleep_C_End, m.DisplayNomal);
                else if (m.State == Main.WorkingState.Nomal)
                    m.DisplaySleep(true);
                else
                {
                    m.WorkTimer.Stop(() => m.DisplaySleep(true));
                }
        }

        private void Study_Click(object sender, RoutedEventArgs e)
        {
            this.Visibility = Visibility.Collapsed;
            if (!m.Core.Controller.EnableFunction || m.Core.Save.Mode != GameSave.ModeType.Ill)
                if (m.State == Main.WorkingState.Study)
                    m.WorkTimer.Stop();
                else m.WorkTimer.Start(Main.WorkingState.Study);
            else
                MessageBoxX.Show($"您的桌宠 {m.Core.Save.Name} 生病啦,没法进行学习", "工作取消");
        }

        private void Work1_Click(object sender, RoutedEventArgs e)
        {
            this.Visibility = Visibility.Collapsed;
            if (!m.Core.Controller.EnableFunction || m.Core.Save.Mode != GameSave.ModeType.Ill)
                if (m.State == Main.WorkingState.WorkONE)
                    m.WorkTimer.Stop();
                else m.WorkTimer.Start(Main.WorkingState.WorkONE);
            else
                MessageBoxX.Show($"您的桌宠 {m.Core.Save.Name} 生病啦,没法进行工作{m.Core.Graph.GraphConfig.Str[(gstr)"work1"]}", "工作取消");
        }

        private void Work2_Click(object sender, RoutedEventArgs e)
        {
            this.Visibility = Visibility.Collapsed;

            if (!m.Core.Controller.EnableFunction || m.Core.Save.Mode != GameSave.ModeType.Ill)
                if (!m.Core.Controller.EnableFunction || m.Core.Save.Level >= 10)
                    if (m.State == Main.WorkingState.WorkTWO)
                        m.WorkTimer.Stop();
                    else m.WorkTimer.Start(Main.WorkingState.WorkTWO);
                else
                    MessageBoxX.Show($"您的桌宠等级不足{m.Core.Save.Level}/10\n无法进行工作{m.Core.Graph.GraphConfig.Str[(gstr)"work2"]}", "工作取消");
            else
                MessageBoxX.Show($"您的桌宠 {m.Core.Save.Name} 生病啦,没法进行工作{m.Core.Graph.GraphConfig.Str[(gstr)"work2"]}", "工作取消");
        }
    }
}
