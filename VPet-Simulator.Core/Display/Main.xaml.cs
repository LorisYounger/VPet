using LinePutScript;
using Panuon.WPF.UI;
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
        /// 消息栏
        /// </summary>
        public MessageBar MsgBar;
        /// <summary>
        /// 工作显示栏
        /// </summary>
        public WorkTimer WorkTimer;
        /// <summary>
        /// 刷新时间时会调用该方法
        /// </summary>
        public event Action<Main> TimeHandle;
        /// <summary>
        /// 刷新时间时会调用该方法,在所有任务处理完之后
        /// </summary>
        public event Action<Main> TimeUIHandle;
        /// <summary>
        /// 是否开始运行
        /// </summary>
        public bool IsWorking { get; private set; } = false;
        public Main(GameCore core, bool loadtouchevent = true)
        {
            Console.WriteLine(DateTime.Now.ToString("T:fff"));
            InitializeComponent();
            Core = core;
            WorkTimer = new WorkTimer(this);
            WorkTimer.Visibility = Visibility.Collapsed;
            UIGrid.Children.Add(WorkTimer);
            ToolBar = new ToolBar(this);
            ToolBar.Visibility = Visibility.Collapsed;
            ToolBar.MenuWork1.Header = core.Graph.GraphConfig.Str[(gstr)"work1"];
            ToolBar.MenuWork2.Header = core.Graph.GraphConfig.Str[(gstr)"work2"];
            UIGrid.Children.Add(ToolBar);
            MsgBar = new MessageBar(this);
            MsgBar.Visibility = Visibility.Collapsed;
            UIGrid.Children.Add(MsgBar);




            if (loadtouchevent)
            {
                LoadTouchEvent();
            }

            var ig = Core.Graph.FindGraph(GraphCore.GraphType.StartUP, core.Save.Mode);
            //var ig2 = Core.Graph.FindGraph(GraphCore.GraphType.Default, core.GameSave.Mode);
            PetGrid2.Visibility = Visibility.Collapsed;

            ig.WaitForReadyRun(PetGrid, () =>
            {
                IsWorking = true;
                Dispatcher.Invoke(() =>
                {
                    PetGrid.Tag = ig;
                    PetGrid2.Tag = ig;
                });
                DisplayNomal();
            });



            EventTimer.Elapsed += EventTimer_Elapsed;
            MoveTimer.Elapsed += MoveTimer_Elapsed;
            SmartMoveTimer.Elapsed += SmartMoveTimer_Elapsed;
        }
        /// <summary>
        /// 自动加载触摸事件
        /// </summary>
        public void LoadTouchEvent()
        {
            Core.TouchEvent.Add(new TouchArea(Core.Graph.GraphConfig.TouchHeadLocate, Core.Graph.GraphConfig.TouchHeadSize, DisplayTouchHead));
            Core.TouchEvent.Add(new TouchArea(Core.Graph.GraphConfig.TouchRaisedLocate, Core.Graph.GraphConfig.TouchRaisedSize, DisplayRaised, true));
        }
        /// <summary>
        /// 播放语音 语音播放时不会停止播放说话表情
        /// </summary>
        /// <param name="VoicePath">语音位置</param>
        public void PlayVoice(Uri VoicePath)//, TimeSpan timediff = TimeSpan.Zero) TODO
        {
            PlayingVoice = true;
            Dispatcher.Invoke(() =>
            {
                VoicePlayer.Clock = new MediaTimeline(VoicePath).CreateClock();
                VoicePlayer.Clock.Completed += Clock_Completed;
                VoicePlayer.Play();
                //Task.Run(() =>
                //{
                //    Thread.Sleep(1000);
                //    Dispatcher.Invoke(() =>
                //    {
                //        if (VoicePlayer?.Clock?.NaturalDuration.HasTimeSpan == true)
                //            PlayEndTime += VoicePlayer.Clock.NaturalDuration.TimeSpan - TimeSpan.FromSeconds(0.8);
                //    });
                //});
            });
        }
        /// <summary>
        /// 声音音量
        /// </summary>
        public double PlayVoiceVolume
        {
            get => Dispatcher.Invoke(() => VoicePlayer.Volume);
            set => Dispatcher.Invoke(() => VoicePlayer.Volume = value);
        }
        /// <summary>
        /// 当前是否正在播放
        /// </summary>
        public bool PlayingVoice = false;
        private void Clock_Completed(object sender, EventArgs e)
        {
            PlayingVoice = false;
            VoicePlayer.Clock = null;
        }

        private void SmartMoveTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            MoveTimer.AutoReset = false;
        }

        private void MoveTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            string str = DisplayType.ToString();
            if (MoveTimer.Enabled == false || (!str.Contains("Left") && !str.Contains("Right")))
            {
                MoveTimer.Enabled = false;
                return;
            }
            Core.Controller.MoveWindows(MoveTimerPoint.X, MoveTimerPoint.Y);
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
                if (DisplayStopMove(DisplayToNomal))
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
            MoveTimer.Enabled = false;
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
            if (rasetype >= 0 || State != WorkingState.Nomal)
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
