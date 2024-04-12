using LinePutScript;
using LinePutScript.Localization.WPF;
using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VPet_Simulator.Core;
using static VPet_Simulator.Core.GraphInfo;

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
            mw.Windows.Add(this);
            InitializeComponent();
            Title = "桌宠管理开发控制台".Translate() + ' ' + mw.PrefixSave;
            this.mw = mw;
            foreach (var v in mw.Core.Graph.GraphsList)
            {
                foreach (AnimatType k in v.Value.Keys)
                {
                    var str = v.Key.ToString() + "++" + k.ToString();
                    GraphListBox.Items.Add(str);
                    GraphListPlayerBox.Items.Add(str);
                }
            }
            if (mw.Core.Graph.GraphsName.TryGetValue(GraphType.Say, out var gl))
                foreach (string v in gl)
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
            var kv = Sub.Split((string)GraphListBox.SelectedItem, "++");
            var graph = mw.Main.Core.Graph.FindGraph(kv[0], (AnimatType)Enum.Parse(typeof(AnimatType), kv[1]), (IGameSave.ModeType)ComboxMode.SelectedIndex);
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
            mw.Main.Say(SayTextBox.Text, CombSay.Text, true);
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
        List<Tuple<string, IGameSave.ModeType>> playlist = new List<Tuple<string, IGameSave.ModeType>>();
        private void GraphListPlayerBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            playlist.Add(new Tuple<string, IGameSave.ModeType>((string)GraphListPlayerBox.SelectedItem,
                (IGameSave.ModeType)Enum.Parse(typeof(IGameSave.ModeType), (string)(((ComboBoxItem)ComboxPlayMode.SelectedItem).Content))));
            GraphListWillPlayBox.Items.Add((string)GraphListPlayerBox.SelectedItem + "_" + (string)((ComboBoxItem)ComboxPlayMode.SelectedItem).Content);
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            DisplayList(new Queue<Tuple<string, IGameSave.ModeType>>(playlist));
        }
        public void DisplayList(Queue<Tuple<string, IGameSave.ModeType>> list)
        {
            if (list.Count == 0)
            {
                mw.Main.DisplayToNomal();
                return;
            }
            var v = list.Dequeue();
            var kv = Sub.Split(v.Item1, "++");
            var graph = mw.Main.Core.Graph.FindGraph(kv[0], (AnimatType)Enum.Parse(typeof(AnimatType), kv[1]), v.Item2);
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
            StringBuilder sb = new StringBuilder();
            foreach (var v in LocalizeCore.StoreTranslationList)
            {
                sb.AppendLine(v.Replace("\n", @"\n").Replace("\r", @"\r"));
            }
            LocalTextBox.Text = sb.ToString();
        }

        private void Button_MoveToLeft_Click(object sender, RoutedEventArgs e)
        {
            mw.Core.Graph.GraphConfig.Moves.Find(x => x.SpeedX < 0 && x.Checked(mw.MWController))?.Display(mw.Main);
        }

        private void Button_MoveToUp_Click(object sender, RoutedEventArgs e)
        {
            mw.Core.Graph.GraphConfig.Moves.Find(x => x.SpeedY < 0 && x.Checked(mw.MWController))?.Display(mw.Main);
        }

        private void Button_MoveToButton_Click(object sender, RoutedEventArgs e)
        {
            mw.Core.Graph.GraphConfig.Moves.Find(x => x.SpeedY > 0 && x.Checked(mw.MWController))?.Display(mw.Main);
        }

        private void Button_MoveToRight_Click(object sender, RoutedEventArgs e)
        {
            mw.Core.Graph.GraphConfig.Moves.Find(x => x.SpeedX > 0 && x.Checked(mw.MWController))?.Display(mw.Main);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            mw.Windows.Remove(this);
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
