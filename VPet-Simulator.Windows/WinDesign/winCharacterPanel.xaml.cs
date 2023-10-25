using LinePutScript.Localization.WPF;
using Panuon.WPF.UI;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace VPet_Simulator.Windows
{
    /// <summary>
    /// winCharacterPanel.xaml 的交互逻辑
    /// </summary>
    public partial class winCharacterPanel : WindowX
    {
        MainWindow mw;
        public winCharacterPanel(MainWindow mw)
        {
            this.mw = mw;
            InitializeComponent();

            foreach (var v in mw.GameSavesData.Statistics.Data)
            {
                Statlists.Add(new Statlist(v.Key, v.Value));
            }
            DataGridStatic.DataContext = Statlists;
        }
        private List<Statlist> Statlists { get; set; } = new List<Statlist>();
        private class Statlist
        {
            public Statlist(string statid, string statcount)
            {
                this.statid = statid;
                this.statcount = statcount;
                if (statid.StartsWith("buy_"))
                {
                    statname = "购买次数".Translate() + statid.Substring(3);
                }
                else
                {
                    statname = statid.Translate();
                }
            }

            /// <summary>
            /// 统计ID
            /// </summary>
            public string statid { get; set; }
            /// <summary>
            /// 统计显示名称
            /// </summary>
            public string statname { get; set; }
            /// <summary>
            /// 统计内容
            /// </summary>
            public string statcount { get; set; }
        }

        private void PgbExperience_GeneratingPercentText(object sender, GeneratingPercentTextRoutedEventArgs e)
        {
            e.Text = $"{e.Value * 10} / {100 * 10}";
        }

        private void PgbStrength_GeneratingPercentText(object sender, GeneratingPercentTextRoutedEventArgs e)
        {
            e.Text = $"{e.Value} / 100";
        }

        private void PgbSpirit_GeneratingPercentText(object sender, GeneratingPercentTextRoutedEventArgs e)
        {
            var progressBar = (ProgressBar)sender;
            progressBar.Foreground = GetForeground(e.Value);
            progressBar.BorderBrush = GetForeground(e.Value);
            e.Text = $"{e.Value} / 100";
        }

        private void PgbHunger_GeneratingPercentText(object sender, GeneratingPercentTextRoutedEventArgs e)
        {
            var progressBar = (ProgressBar)sender;
            progressBar.Foreground = GetForeground(e.Value);
            progressBar.BorderBrush = GetForeground(e.Value);
            e.Text = $"{e.Value} / 100";
        }

        private void PgbThirsty_GeneratingPercentText(object sender, GeneratingPercentTextRoutedEventArgs e)
        {
            var progressBar = (ProgressBar)sender;
            progressBar.Foreground = GetForeground(e.Value);
            progressBar.BorderBrush = GetForeground(e.Value);
            e.Text = $"{e.Value} / 100";
            if (e.Value <= 20)
            {
                txtHearth.Visibility = Visibility.Visible;
                stkHearth.Visibility = Visibility.Visible;
            }
        }

        private void PgbHearth_GeneratingPercentText(object sender, GeneratingPercentTextRoutedEventArgs e)
        {
            e.Text = $"{e.Value} / 100";
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

    }
}
