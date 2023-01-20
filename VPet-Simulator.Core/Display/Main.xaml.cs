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
using System.Windows.Shapes;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// Main.xaml 的交互逻辑
    /// </summary>
    public partial class Main : UserControl, IDisposable
    {
        /// <summary>
        /// 游戏核心
        /// </summary>
        public GameCore Core;
        /// <summary>
        /// 菜单栏
        /// </summary>
        public ToolBar ToolBar;
        /// <summary>
        /// 菜单栏
        /// </summary>
        public MessageBar MsgBar;
        /// <summary>
        /// 刷新时间时会调用该方法
        /// </summary>
        public event Action<Main> TimeHandle;
        /// <summary>
        /// 刷新时间时会调用该方法,在所有任务处理完之后
        /// </summary>
        public event Action<Main> TimeUIHandle;
        public Main(GameCore core)
        {
            InitializeComponent();
            Core = core;

            ToolBar = new ToolBar(this);
            ToolBar.Visibility = Visibility.Collapsed;
            UIGrid.Children.Add(ToolBar);
            MsgBar = new MessageBar();
            MsgBar.Visibility = Visibility.Collapsed;
            UIGrid.Children.Add(MsgBar);
            Core.TouchEvent.Add(new TouchArea(Core.Graph.GraphConfig.TouchHeadLocate, Core.Graph.GraphConfig.TouchHeadSize, DisplayTouchHead));
            Core.TouchEvent.Add(new TouchArea(Core.Graph.GraphConfig.TouchRaisedLocate, Core.Graph.GraphConfig.TouchRaisedSize, DisplayRaised, true));
            var ig = Core.Graph.FindGraph(GraphCore.GraphType.Default, Core.Save.Mode);
            PetGrid.Child = ig.This;
            ig.Run(DisplayNomal);


            EventTimer.Elapsed += EventTimer_Elapsed;
            MoveTimer.Elapsed += MoveTimer_Elapsed;
        }
        public void Say(string text)
        {
            MsgBar.Show(Core.Save.Name, text);
        }
        private void MoveTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Core.Controller.MoveWindows(MoveTimerPoint.X, MoveTimerPoint.Y);
        }
        public Action DefaultClickAction;
        bool isPress = false;
        private void MainGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isPress = true;
            if (DisplayType != GraphCore.GraphType.Default)
            {//不是nomal! 可能会卡timer,所有全部timer清空下
                MoveTimer.Stop();
                MainGrid.MouseMove -= MainGrid_MouseMove;
                if (DisplayStopMove(DisplayNomal))
                    return;
            }
            Task.Run(() =>
            {
                Thread.Sleep(Core.Controller.PressLength);
                Point mp = default;
                Dispatcher.BeginInvoke(new Action(() => mp = Mouse.GetPosition(MainGrid))).Wait();
                if (isPress)
                {//历遍长按事件
                    var act = Core.TouchEvent.FirstOrDefault(x => x.IsPress == true && x.Touch(mp));
                    if (act != null)
                        Dispatcher.BeginInvoke(act.DoAction);
                }
                else
                {//历遍点击事件
                    var act = Core.TouchEvent.FirstOrDefault(x => x.IsPress == false && x.Touch(mp));
                    if (act != null)
                        Dispatcher.Invoke(act.DoAction);
                    else
                        DefaultClickAction?.Invoke();
                }
            });
        }

        private void MainGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isPress = false;
            if (rasetype != int.MinValue)
            {
                MainGrid.MouseMove -= MainGrid_MouseMove;
                rasetype = -1;
                DisplayRaising();
            }
        }

        private void MainGrid_MouseMove(object sender, MouseEventArgs e)
        {
            var mp = e.GetPosition(MainGrid);
            var x = mp.X - Core.Graph.GraphConfig.RaisePoint.X;
            var y = mp.Y - Core.Graph.GraphConfig.RaisePoint.Y;
            Core.Controller.MoveWindows(x, y);
            if (Math.Abs(x) + Math.Abs(y) > 10)
                rasetype = 0;
        }

        private void MainGrid_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            ToolBar.Show();
        }

        public void Dispose()
        {
            EventTimer.Stop();
            MoveTimer.Stop();
            EventTimer.Dispose();
            MoveTimer.Dispose();
            if (PetGrid.Child is IGraph g)
                g.Stop();
            if (PetGrid2.Child is IGraph g2)
                g2.Stop();
        }
    }
}
