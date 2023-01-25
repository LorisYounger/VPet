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
using System.Windows.Shapes;
using static VPet_Simulator.Core.GraphCore;

namespace VPet_Simulator.Windows
{
    /// <summary>
    /// winConsole.xaml 的交互逻辑
    /// </summary>
    public partial class winConsole : Window
    {
        MainWindow mw;
        public winConsole(MainWindow mw)
        {
            InitializeComponent();
            this.mw = mw;
            foreach (string v in Enum.GetNames(typeof(GraphType)))
            {
                GraphListBox.Items.Add(v);
            }
            
        }
        public void DisplayLoop(GraphType graphType)
        {
            mw.Main.Display(graphType, () => DisplayLoop(graphType));
        }
        private void GraphListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (GraphListBox.SelectedItem == null)
                return;
            LabelNowPlay.Content = $"当前正在播放: {GraphListBox.SelectedItem}";
            DisplayLoop((GraphType)Enum.Parse(typeof(GraphType), (string)GraphListBox.SelectedItem));
        }
    }
}
