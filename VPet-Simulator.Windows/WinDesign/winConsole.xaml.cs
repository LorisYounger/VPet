using LinePutScript.Localization.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
                GraphListPlayerBox.Items.Add(v);
            }
            foreach (string v in Enum.GetNames(typeof(GraphCore.Helper.SayType)))
            {
                CombSay.Items.Add(v);
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
                 (GameSave.ModeType)Enum.Parse(typeof(GameSave.ModeType), (string)(((ComboBoxItem)ComboxMode.SelectedItem).Content)));
            if (graph == null)
            {
                LabelNowPlay.Content = "未找到对应类型图像资源".Translate();
                return;
            }
            LabelNowPlay.Content = "当前正在播放".Translate() + ": " + GraphListBox.SelectedItem;
            DisplayLoop(graph);
        }

        private void DisplayListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DisplayListBox.SelectedItem == null)
                return;
            LabelSuccess.Content = "当前正在运行".Translate() + ": " + (string)((ListBoxItem)DisplayListBox.SelectedItem).Content;
            mw.RunAction((string)((ListBoxItem)DisplayListBox.SelectedItem).Content);
        }

        private void Say_Click(object sender, RoutedEventArgs e)
        {
            mw.Main.Say(SayTextBox.Text, (Helper.SayType)Enum.Parse(typeof(Helper.SayType), CombSay.Text));
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
        List<Tuple<GraphType, GameSave.ModeType>> playlist = new List<Tuple<GraphType, GameSave.ModeType>>();
        private void GraphListPlayerBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            playlist.Add(new Tuple<GraphType, GameSave.ModeType>((GraphType)Enum.Parse(typeof(GraphType), (string)GraphListPlayerBox.SelectedItem),
                (GameSave.ModeType)Enum.Parse(typeof(GameSave.ModeType), (string)(((ComboBoxItem)ComboxPlayMode.SelectedItem).Content))));
            GraphListWillPlayBox.Items.Add((string)GraphListPlayerBox.SelectedItem + "_" + (string)((ComboBoxItem)ComboxPlayMode.SelectedItem).Content);
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            DisplayList(new Queue<Tuple<GraphType, GameSave.ModeType>>(playlist));
        }
        public void DisplayList(Queue<Tuple<GraphType, GameSave.ModeType>> list)
        {
            if (list.Count == 0)
            {
                mw.Main.DisplayToNomal();
                return;
            }
            var v = list.Dequeue();
            var graph = mw.Main.Core.Graph.FindGraph(v.Item1, v.Item2);
            if (graph != null)
            {
                mw.Main.Display(graph, () => DisplayList(list));
            }
            else
            {
                DisplayList(list);
            }
        }

        private void GraphListWillPlayBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            playlist.RemoveAt(GraphListWillPlayBox.SelectedIndex);
            GraphListWillPlayBox.Items.RemoveAt(GraphListWillPlayBox.SelectedIndex);
        }

        private void PlayADD_Click(object sender, RoutedEventArgs e) => GraphListPlayerBox_MouseDoubleClick(sender, null);

        private void Local_SelectAll_Click(object sender, MouseButtonEventArgs e)
        {
            LocalTextBox.SelectAll();
        }

        private void Output_No_Local(object sender, RoutedEventArgs e)
        {
            LocalTextBox.Text = string.Join("\n", LocalizeCore.StoreTranslationList);
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
