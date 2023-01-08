using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Timers;
using Timer = System.Timers.Timer;
using Panuon.WPF.UI;
using System.Windows.Threading;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// ToolBar.xaml 的交互逻辑
    /// </summary>
    public partial class ToolBar : UserControl
    {
        Main m;
        Timer closetimer;
        bool onFocus = false;
        DispatcherTimer closePanelTimer;

        public ToolBar(Main m)
        {
            InitializeComponent();
            this.m = m;
            closetimer = new Timer()
            {
                Interval = 4000,
                AutoReset = false,
                Enabled = false
            };
            closetimer.Elapsed += Closetimer_Elapsed;
            closePanelTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromSeconds(0.1),
            };
            closePanelTimer.Tick += ClosePanelTimer_Tick;
            m.TimeUIHandle += M_TimeUIHandle;
        }

        private void M_TimeUIHandle(Main m)
        {
            if (BdrPanel.Visibility == Visibility.Visible)
            {
                Tlv.Text = "Lv " + m.Core.Save.Level.ToString();
                tMoney.Text = "$ " + m.Core.Save.Money.ToString("N2");
                till.Visibility = m.Core.Save.Mode == Save.ModeType.Ill ? Visibility.Visible : Visibility.Collapsed;
                pExp.Maximum = m.Core.Save.LevelUpNeed();
                pExp.Value = m.Core.Save.Exp;
                pStrength.Value = m.Core.Save.Strength;
                pFeeling.Value = m.Core.Save.Feeling;
                pStrengthFood.Value = m.Core.Save.StrengthFood;
                pStrengthDrink.Value = m.Core.Save.StrengthDrink;
            }
        }

        private void ClosePanelTimer_Tick(object sender, EventArgs e)
        {
            if (BdrPanel.IsMouseOver
                || MenuPanel.IsMouseOver)
            {
                closePanelTimer.Stop();
                return;
            }
            BdrPanel.Visibility = Visibility.Collapsed;
        }

        private void Closetimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (onFocus)
            {
                onFocus = false;
                closetimer.Start();
            }
            else
                Dispatcher.Invoke(() => this.Visibility = Visibility.Collapsed);
        }

        public void Show()
        {
            Visibility = Visibility.Visible;
            if (closetimer.Enabled)
                onFocus = true;
            else
                closetimer.Start();
        }

        private void UserControl_MouseEnter(object sender, MouseEventArgs e)
        {
            closetimer.Stop();
        }

        private void UserControl_MouseLeave(object sender, MouseEventArgs e)
        {
            closetimer.Start();
        }

        private void MenuSetting_Click(object sender, RoutedEventArgs e)
        {
            m.Core.Controller.ShowSetting();
        }

        private void MenuPanel_Click(object sender, RoutedEventArgs e)
        {
            m.Core.Controller.ShowPanel();
        }

        public void AddMenuButton(string parentMenu,
            string displayName,
            Action clickCallback)
        {
            var menuItem = new MenuItem()
            {
                Header = displayName,
            };
            menuItem.Click += delegate
            {
                clickCallback?.Invoke();
            };
            switch (parentMenu)
            {
                case "投喂":
                    MenuFeed.Items.Add(menuItem);
                    break;
            }
        }

        private void PgbExperience_GeneratingPercentText(object sender, GeneratingPercentTextRoutedEventArgs e)
        {
            e.Text = $"{e.Value} / {pExp.Maximum}";
        }

        private void PgbStrength_GeneratingPercentText(object sender, GeneratingPercentTextRoutedEventArgs e)
        {
            e.Text = $"{e.Value} / 100";
        }

        private void PgbSpirit_GeneratingPercentText(object sender, GeneratingPercentTextRoutedEventArgs e)
        {
            var progressBar = (ProgressBar)sender;
            progressBar.Foreground = GetForeground(e.Value);
            e.Text = $"{e.Value} / 100";
        }

        private void PgbHunger_GeneratingPercentText(object sender, GeneratingPercentTextRoutedEventArgs e)
        {
            var progressBar = (ProgressBar)sender;
            progressBar.Foreground = GetForeground(e.Value);
            e.Text = $"{e.Value} / 100";
        }

        private void PgbThirsty_GeneratingPercentText(object sender, GeneratingPercentTextRoutedEventArgs e)
        {
            var progressBar = (ProgressBar)sender;
            progressBar.Foreground = GetForeground(e.Value);
            e.Text = $"{e.Value} / 100";
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
    }
}
