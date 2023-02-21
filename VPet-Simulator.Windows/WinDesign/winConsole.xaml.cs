using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using VPet_Simulator.Core;
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
            DestanceTimer.Elapsed += DestanceTimer_Elapsed;
        }

        private void DestanceTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                RLeft.Text = mw.Core.Controller.GetWindowsDistanceLeft().ToString("f2");
                RRight.Text = mw.Core.Controller.GetWindowsDistanceRight().ToString("f2");
                RTop.Text = mw.Core.Controller.GetWindowsDistanceUp().ToString("f2");
                RDown.Text = mw.Core.Controller.GetWindowsDistanceDown().ToString("f2");
            });
        }

        public void DisplayLoop(IGraph graph)
        {
            mw.Main.Display(graph, () => DisplayLoop(graph));
        }
        private void GraphListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (GraphListBox.SelectedItem == null)
                return;
            var graph = mw.Main.Core.Graph.FindGraph((GraphType)Enum.Parse(typeof(GraphType), (string)GraphListBox.SelectedItem),
                 (Save.ModeType)Enum.Parse(typeof(Save.ModeType), (string)(((ComboBoxItem)ComboxMode.SelectedItem).Content)));
            if (graph == null)
            {
                LabelNowPlay.Content = "未找到对应类型图像资源";
                return;
            }
            LabelNowPlay.Content = $"当前正在播放: {GraphListBox.SelectedItem}";
            DisplayLoop(graph);
        }

        private void DisplayListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DisplayListBox.SelectedItem == null)
                return;
            LabelSuccess.Content = $"当前正在运行: {(string)((ListBoxItem)DisplayListBox.SelectedItem).Content}";
            switch ((string)((ListBoxItem)DisplayListBox.SelectedItem).Content)
            {
                case "DisplayNomal":
                    mw.Main.DisplayNomal();
                    break;
                case "DisplayTouchHead":
                    mw.Main.DisplayTouchHead();
                    break;
                case "DisplayTouchBody":
                    mw.Main.DisplayTouchBody();
                    break;
                case "DisplayBoring":
                    mw.Main.DisplayBoring();
                    break;
                case "DisplaySquat":
                    mw.Main.DisplaySquat();
                    break;
                case "DisplaySleep":
                    mw.Main.DisplaySleep();
                    break;
                case "DisplayRaised":
                    mw.Main.DisplayRaised();
                    break;
                case "DisplayFalled_Left":
                    mw.Main.DisplayFalled_Left();
                    break;
                case "DisplayFalled_Right":
                    mw.Main.DisplayFalled_Right();
                    break;
                case "DisplayWalk_Left":
                    mw.Main.DisplayWalk_Left();
                    break;
                case "DisplayWalk_Right":
                    mw.Main.DisplayWalk_Right();
                    break;
                case "DisplayCrawl_Left":
                    mw.Main.DisplayCrawl_Left();
                    break;
                case "DisplayCrawl_Right":
                    mw.Main.DisplayCrawl_Right();
                    break;
                case "DisplayClimb_Left_UP":
                    mw.Main.DisplayClimb_Left_UP();
                    break;
                case "DisplayClimb_Left_DOWN":
                    mw.Main.DisplayClimb_Left_DOWN();
                    break;
                case "DisplayClimb_Right_UP":
                    mw.Main.DisplayClimb_Right_UP();
                    break;
                case "DisplayClimb_Right_DOWN":
                    mw.Main.DisplayClimb_Right_DOWN();
                    break;
                case "DisplayClimb_Top_Right":
                    mw.Main.DisplayClimb_Top_Right();
                    break;
                case "DisplayClimb_Top_Left":
                    mw.Main.DisplayClimb_Top_Left();
                    break;
                case "DisplayFall_Left":
                    mw.Main.DisplayFall_Left();
                    break;
                case "DisplayFall_Right":
                    mw.Main.DisplayFall_Right();
                    break;
                case "DisplayIdel_StateONE":
                    mw.Main.DisplayIdel_StateONE();
                    break;
                case "DisplayIdel_StateTWO":
                    mw.Main.DisplayIdel_StateTWO();
                    break;
                    
            }
        }

        private void Say_Click(object sender, RoutedEventArgs e)
        {
            mw.Main.Say(SayTextBox.Text);
        }
        Timer DestanceTimer = new Timer()
        {
            AutoReset = true,
            Interval = 100,
        };


        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            DestanceTimer.Start();
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            DestanceTimer.Stop();
        }

        //private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //   switch(((TabControl)sender).SelectedIndex)
        //    {
        //        case 0:
        //        case 1:
        //        case 2:
        //            ComboxMode.Visibility = Visibility.Visible;
        //            break;
        //        default:
        //            ComboxMode.Visibility = Visibility.Collapsed;
        //            break;
        //    }
        //}
    }
}
