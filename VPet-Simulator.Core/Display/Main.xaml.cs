﻿using Panuon.WPF.UI;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using VPet_Simulator.Core.New;
using static VPet_Simulator.Core.GraphCore;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// Main.xaml 的交互逻辑
    /// </summary>
    public partial class Main : ContentControlX, IDisposable
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
        public Main(GameCore core, bool loadtouchevent = true)
        {
            Console.WriteLine(DateTime.Now.ToString("T:fff"));
            InitializeComponent();
            Core = core;

            ToolBar = new ToolBar(this);
            ToolBar.Visibility = Visibility.Collapsed;
            UIGrid.Children.Add(ToolBar);
            MsgBar = new MessageBar(this);
            MsgBar.Visibility = Visibility.Collapsed;
            UIGrid.Children.Add(MsgBar);

            if (loadtouchevent)
            {
                LoadTouchEvent();
            }

            var ig = Core.Graph.FindGraph(GraphCore.GraphType.StartUP, core.Save.Mode);
            PetGrid.Child = ig.This;
            var ig2 = Core.Graph.FindGraph(GraphCore.GraphType.Default, core.Save.Mode);
            PetGrid2.Child = ig2.This; //用于缓存
            //PetGrid2.Visibility = Visibility.Collapsed;
            ig.WaitForReadyRun(DisplayNomal);


            EventTimer.Elapsed += EventTimer_Elapsed;
            MoveTimer.Elapsed += MoveTimer_Elapsed;
            SmartMoveTimer.Elapsed += SmartMoveTimer_Elapsed;

            AnimationController.Instance.RegistryGraph(animationCanvas);
        }
        /// <summary>
        /// 自动加载触摸事件
        /// </summary>
        public void LoadTouchEvent()
        {
            Core.TouchEvent.Add(new TouchArea(Core.Graph.GraphConfig.TouchHeadLocate, Core.Graph.GraphConfig.TouchHeadSize, DisplayTouchHead));
            Core.TouchEvent.Add(new TouchArea(Core.Graph.GraphConfig.TouchRaisedLocate, Core.Graph.GraphConfig.TouchRaisedSize, DisplayRaised, true));
        }

        private void SmartMoveTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            MoveTimer.AutoReset = false;
        }

        private void MoveTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            string str = DisplayType.ToString();
            Core.Controller.MoveWindows(MoveTimerPoint.X, MoveTimerPoint.Y);
            if (!str.Contains("Left") && !str.Contains("Right"))
            {
                MoveTimer.Enabled = false;
            }
        }
        public Action DefaultClickAction;
        bool isPress = false;
        long presstime;
        private void MainGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isPress = true;
            CountNomal = 0;
            if (DisplayType != GraphCore.GraphType.Default)
            {//不是nomal! 可能会卡timer,所有全部timer清空下
                CleanState();
                if (DisplayStopMove(DisplayNomal))
                    return;
            }
            Task.Run(() =>
            {
                var pth = DateTime.Now.Ticks;
                presstime = pth;
                Thread.Sleep(Core.Controller.PressLength);
                Point mp = default;
                Dispatcher.BeginInvoke(new Action(() => mp = Mouse.GetPosition(MainGrid))).Wait();
                //mp = new Point(mp.X * Core.Controller.ZoomRatio, mp.Y * Core.Controller.ZoomRatio);
                if (isPress && presstime == pth)
                {//历遍长按事件
                    var act = Core.TouchEvent.FirstOrDefault(x => x.IsPress == true && x.Touch(mp));
                    if (act != null)
                        Dispatcher.Invoke(act.DoAction);
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
            if (DisplayType.ToString().StartsWith("Raised"))
            {
                MainGrid.MouseMove -= MainGrid_MouseMove;
                MainGrid.MouseMove += MainGrid_MouseWave;
                rasetype = -1;
                DisplayRaising();
            }
            else
            {
                //if (MsgBar.Visibility == Visibility.Visible)
                //{
                //    MsgBar.ForceClose();
                //}
                if (SmartMove)
                {
                    MoveTimer.AutoReset = true;
                    SmartMoveTimer.Enabled = false;
                    SmartMoveTimer.Start();
                }
            }
            ((UIElement)e.Source).ReleaseMouseCapture();
        }

        private void MainGrid_MouseMove(object sender, MouseEventArgs e)
        {
            ((UIElement)e.Source).CaptureMouse();
            var mp = e.GetPosition(MainGrid);
            var x = mp.X - Core.Graph.GraphConfig.RaisePoint[(int)Core.Save.Mode].X;
            var y = mp.Y - Core.Graph.GraphConfig.RaisePoint[(int)Core.Save.Mode].Y;
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
            MoveTimer.Enabled = false; ;
            EventTimer.Dispose();
            MoveTimer.Dispose();
            MsgBar.Dispose();
            ToolBar.Dispose();
            if (PetGrid.Child is IGraph g)
                g.Stop(true);
            if (PetGrid2.Child is IGraph g2)
                g2.Stop(true);
        }
        /// <summary>
        /// 清理所有状态
        /// </summary>
        public void CleanState()
        {
            MoveTimer.Enabled = false;
            MainGrid.MouseMove -= MainGrid_MouseMove;
            MainGrid.MouseMove += MainGrid_MouseWave;
        }
        private int wavetimes = 0;
        private int switchcount = 0;
        private bool? waveleft = null;
        private bool? wavetop = null;
        private DateTime wavespan;
        private void MainGrid_MouseWave(object sender, MouseEventArgs e)
        {
            if (rasetype >= 0)
                return;

            if ((DateTime.Now - wavespan).TotalSeconds > 2)
            {
                wavetimes = 0;
                switchcount = 0;
                waveleft = null;
                wavetop = null;
            }
            wavespan = DateTime.Now;
            bool active = false;
            var p = e.GetPosition(MainGrid);

            if (p.Y < 200)
            {
                if (wavetop != false)
                    wavetop = true;
                else
                {
                    if (switchcount++ > 150)
                        wavespan = DateTime.MinValue;
                    return;
                }
            }
            else
            {
                if (wavetop != true)
                    wavetop = false;
                else
                {
                    if (switchcount++ > 150)
                        wavespan = DateTime.MinValue;
                    return;
                }
            }

            if (p.X < 200 && waveleft != true)
            {
                waveleft = true;
                active = true;
            }
            if (p.X > 300 && waveleft != false)
            {
                active = true;
                waveleft = false;
            }

            if (active)
            {
                if (wavetimes++ > 4)
                    if (wavetop == true)
                    {
                        if (wavetimes >= 10 || DisplayType == GraphCore.GraphType.Default || DisplayType == GraphType.Touch_Head_B_Loop || DisplayType == GraphType.Touch_Head_C_End)
                            DisplayTouchHead();
                        //Console.WriteLine(wavetimes);
                    }
                    else
                    {
                        if (wavetimes >= 10 || DisplayType == GraphCore.GraphType.Default || DisplayType == GraphType.Touch_Body_B_Loop || DisplayType == GraphType.Touch_Body_C_End)
                            DisplayTouchBody();
                    }
            }
        }
    }
}
