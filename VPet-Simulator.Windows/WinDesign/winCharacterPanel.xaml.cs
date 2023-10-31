using LinePutScript.Localization.WPF;
using Panuon.WPF.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
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
            Title = "面板".Translate() + ' ' + mw.PrefixSave;
            mw.Windows.Add(this);
            foreach (var v in mw.GameSavesData.Statistics.Data)
            {
                StatList.Add(new StatInfo(v.Key, v.Value.GetDouble()));
            }
            DataGridStatic.ItemsSource = StatList;
            mw.GameSavesData.Statistics.StatisticChanged += Statistics_StatisticChanged;
        }

        private void Statistics_StatisticChanged(Interface.Statistics sender, string name, LinePutScript.SetObject value)
        {
            var v = StatList.FirstOrDefault(x => x.StatId == name);
            if (v != null)
            {
                v.StatCount = value.GetDouble();
            }
            else
            {
                StatList.Add(new StatInfo(name, value.GetDouble()));
            }
        }

        private ObservableCollection<StatInfo> StatList { get; set; } = new();

        private class StatInfo : INotifyPropertyChanged
        {
            public StatInfo(string statId, double statCount)
            {
                StatId = statId;
                StatCount = statCount;
                if (statId.StartsWith("buy_"))
                {
                    StatName = "购买次数".Translate() + '_' + statId.Substring(4).Translate();
                }
                else if (statId.StartsWith("stat_"))
                {
                    StatName = "统计".Translate() + '_' + statId.Substring(5).Translate();
                }
                else
                {
                    StatName = statId.Translate();
                }
            }

            /// <summary>
            /// 统计ID
            /// </summary>
            public string StatId { get; set; }

            /// <summary>
            /// 统计显示名称
            /// </summary>
            public string StatName { get; set; }

            private double _statCount;
            /// <summary>
            /// 统计内容
            /// </summary>
            public double StatCount
            {
                get { return _statCount; }
                set
                {
                    if (_statCount != value)
                    {
                        _statCount = value;
                        OnPropertyChanged(nameof(StatCount));
                    }
                }
            }
            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            public event PropertyChangedEventHandler PropertyChanged;
        }

        private void PgbExperience_GeneratingPercentText(
            object sender,
            GeneratingPercentTextRoutedEventArgs e
        )
        {
            e.Text = $"{e.Value * 10} / {100 * 10}";
        }

        private void PgbStrength_GeneratingPercentText(
            object sender,
            GeneratingPercentTextRoutedEventArgs e
        )
        {
            e.Text = $"{e.Value} / 100";
        }

        private void PgbSpirit_GeneratingPercentText(
            object sender,
            GeneratingPercentTextRoutedEventArgs e
        )
        {
            var progressBar = (ProgressBar)sender;
            progressBar.Foreground = GetForeground(e.Value);
            progressBar.BorderBrush = GetForeground(e.Value);
            e.Text = $"{e.Value} / 100";
        }

        private void PgbHunger_GeneratingPercentText(
            object sender,
            GeneratingPercentTextRoutedEventArgs e
        )
        {
            var progressBar = (ProgressBar)sender;
            progressBar.Foreground = GetForeground(e.Value);
            progressBar.BorderBrush = GetForeground(e.Value);
            e.Text = $"{e.Value} / 100";
        }

        private void PgbThirsty_GeneratingPercentText(
            object sender,
            GeneratingPercentTextRoutedEventArgs e
        )
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

        private void PgbHearth_GeneratingPercentText(
            object sender,
            GeneratingPercentTextRoutedEventArgs e
        )
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

        private void TextBox_Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox textBox)
                return;
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                DataGridStatic.ItemsSource = StatList;
            }
            else
            {
                DataGridStatic.ItemsSource = StatList.Where(
                    i =>
                        i.StatName.IndexOf(
                            textBox.Text,
                            StringComparison.InvariantCultureIgnoreCase
                        ) >= 0 || i.StatId.IndexOf(
                            textBox.Text,
                            StringComparison.InvariantCultureIgnoreCase
                        ) >= 0
                );
            }
        }

        private void WindowX_Closed(object sender, EventArgs e)
        {
            mw.GameSavesData.Statistics.StatisticChanged -= Statistics_StatisticChanged;
            mw.Windows.Remove(this);
        }
    }
}
