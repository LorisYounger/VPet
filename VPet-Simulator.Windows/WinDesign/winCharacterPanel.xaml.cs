using Panuon.WPF.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace VPet_Simulator.Windows.WinDesign
{
    /// <summary>
    /// winCharacterPanel.xaml 的交互逻辑
    /// </summary>
    public partial class winCharacterPanel : WindowX
    {
        public winCharacterPanel()
        {
            InitializeComponent();
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
            if(e.Value <= 20)
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
            if(value >= 80)
            {
                return FindResource("SuccessProgressBarForeground") as Brush;
            }
            else if(value >= 50)
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
