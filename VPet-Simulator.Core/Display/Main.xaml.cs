using LinePutScript.Localization.WPF;
using Panuon.WPF.UI;
using System;
using System.Collections.Generic;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using static VPet_Simulator.Core.GraphInfo;

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
        public IMassageBar MsgBar;
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
        /// 如果不开启功能模式,默认状态设置
        /// </summary>
        public IGameSave.ModeType NoFunctionMOD = IGameSave.ModeType.Happy;
        /// <summary>
        /// 是否开始运行
        /// </summary>
        public bool IsWorking { get; private set; } = false;
        public SoundPlayer soundPlayer = new SoundPlayer();
        public bool windowMediaPlayerAvailable = true;

        public void Load_0_BaseConsole()
        {
            Dispatcher.Invoke(() =>
            {
                WorkTimer = new WorkTimer(this);
                WorkTimer.Visibility = Visibility.Collapsed;
                UIGrid.Children.Add(WorkTimer);
                ToolBar = new ToolBar(this);
                ToolBar.Visibility = Visibility.Collapsed;
                UIGrid.Children.Add(ToolBar);
                MsgBar = new MessageBar(this);
                MsgBar.Visibility = Visibility.Collapsed;
                UIGrid.Children.Add(MsgBar.This);
            });
        }
        public void Load_3_BindingTimer()
        {
            EventTimer.Elapsed += (s, e) => EventTimer_Elapsed();
            MoveTimer.Elapsed += MoveTimer_Elapsed;
            SmartMoveTimer.Elapsed += SmartMoveTimer_Elapsed;
        }
        public List<string> ErrorMessage = new List<string>();
        /// <summary>
        /// 支持在加载等待的时候显示等待计数器
        /// </summary>
        public async Task Load_2_WaitGraph()
        {
            //新功能:等待所有图像加载完成再跑
            foreach (var igs in Core.Graph.GraphsList.Values)
            {
                foreach (var ig2 in igs.Values)
                {
                    for (int i = 0; i < ig2.Count; i++)
                    {
                        IGraph ig3 = ig2[i];
                        while (!ig3.IsReady)
                        {
                            if (ig3.IsFail)
                            {
                                ErrorMessage.Add(ig3.FailMessage);
                                ig2.Remove(ig3);
                                break;
                            }
                            else
                                await Task.Delay(100);
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 支持在加载等待的时候显示等待计数器
        /// </summary>
        /// <param name="WaitCountAction">当前已等待图像个数</param>
        public async Task Load_2_WaitGraph(Action<int> WaitCountAction)
        {
            if (WaitCountAction == null)
            {
                await Load_2_WaitGraph();
                return;
            }
            int count = 0;
            DateTime start = DateTime.Now.AddSeconds(2);
            // 新功能: 等待所有图像加载完成再跑
            var tasks = new List<Task>();

            foreach (var igs in Core.Graph.GraphsList.Values)
            {
                foreach (var ig2 in igs.Values)
                {
                    for (int i = 0; i < ig2.Count; i++)
                    {
                        IGraph ig3 = ig2[i];
                        tasks.Add(Task.Run(async () =>
                        {
                            while (!ig3.IsReady)
                            {
                                if (ig3.IsFail)
                                {
                                    lock (ErrorMessage) // 确保线程安全
                                    {
                                        ErrorMessage.Add(ig3.FailMessage);
                                        ig2.Remove(ig3);
                                    }
                                    break;
                                }
                                else
                                {
                                    await Task.Delay(100);
                                }
                            }
                            Interlocked.Increment(ref count);
                            if (start < DateTime.Now)
                            {
                                start = DateTime.Now.AddSeconds(2);
                                WaitCountAction.Invoke(count);
                            }
                        }));
                    }

                }
            }

            // 等待所有任务完成
            await Task.WhenAll(tasks);

        }
        /// <summary>
        /// 开始运行
        /// </summary>
        /// <param name="startUPGraph">开始运行初始动画</param>
        public void Load_4_Start(IGraph startUPGraph = null)
        {
            IGraph ig = startUPGraph ?? Core.Graph.FindGraph(Core.Graph.FindName(GraphType.StartUP), AnimatType.Single, Core.Save.Mode);
            ig ??= Core.Graph.FindGraph(Core.Graph.FindName(GraphType.Default), AnimatType.Single, Core.Save.Mode);
            Task.Run(() =>
            {
                ig.Run(PetGrid, () =>
                {
                    IsWorking = true;
                    DisplayNomal();
                });
            });
        }
        /// <summary>
        /// 等待图像加载和开始
        /// </summary>
        public void Load_24_WaitAndStart()
        {
            Load_2_WaitGraph().Wait();
            Load_4_Start();
        }
        /// <summary>
        /// 等待图像加载和开始
        /// </summary>
        /// <param name="WaitCountAction">当前已等待图像个数</param>
        /// <param name="startUPGraph">开始运行初始动画</param>
        public void Load_24_WaitAndStart(Action<int> WaitCountAction, IGraph startUPGraph = null)
        {
            Load_2_WaitGraph(WaitCountAction).Wait();
            Load_4_Start(startUPGraph);
        }

        public Main(GameCore core)
        {
            InitializeComponent();
            Core = core;

            labeldisplaytimer.Elapsed += Labledisplaytimer_Elapsed;

            DisplayNomal = DisplayDefault;

            if (!core.Controller.EnableFunction)
                Core.Save.Mode = NoFunctionMOD;
        }
        /// <summary>
        /// 加载所有步骤并开始
        /// </summary>
        /// <param name="WaitCountAction">当前已等待图像个数</param>
        /// <param name="startUPGraph">开始运行初始动画</param>
        public void LoadALL(Action<int> WaitCountAction = null, IGraph startUPGraph = null)
        {
            Load_0_BaseConsole();
            Load_2_TouchEvent();
            Load_2_WaitGraph(WaitCountAction).Wait();
            Load_3_BindingTimer();
            Load_4_Start(startUPGraph);
        }

        private void Labledisplaytimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (--labeldisplaycount <= 0)
            {
                labeldisplaytimer.Enabled = false;
                labeldisplaychangenum1 = 0;
                labeldisplaychangenum2 = 0;
                Dispatcher.Invoke(() =>
                {
                    LabelDisplay.Visibility = Visibility.Collapsed;
                });
            }
            else if (labeldisplaycount < 50)
            {
                Dispatcher.Invoke(() =>
                {
                    LabelDisplay.Opacity = labeldisplaycount / 50;
                });
            }
        }

        /// <summary>
        /// 自动加载触摸事件
        /// </summary>
        public void Load_2_TouchEvent()
        {
            Core.TouchEvent.Add(new TouchArea(Core.Graph.GraphConfig.TouchHeadLocate, Core.Graph.GraphConfig.TouchHeadSize, () => { DisplayTouchHead(); return true; }));
            Core.TouchEvent.Add(new TouchArea(Core.Graph.GraphConfig.TouchBodyLocate, Core.Graph.GraphConfig.TouchBodySize, () => { DisplayTouchBody(); return true; }));
            for (int i = 0; i < 4; i++)
            {
                IGameSave.ModeType m = (IGameSave.ModeType)i;
                Core.TouchEvent.Add(new TouchArea(Core.Graph.GraphConfig.TouchRaisedLocate[i], Core.Graph.GraphConfig.TouchRaisedSize[i],
                    () =>
                    {
                        if (Core.Save.Mode == m)
                        {
                            DisplayRaised();
                            return true;
                        }
                        else
                            return false;

                    }, true));
            }
        }
        /// <summary>
        /// 播放语音 语音播放时不会停止播放说话表情
        /// </summary>
        /// <param name="VoicePath">语音位置</param>
        public void PlayVoice(Uri VoicePath)//, TimeSpan timediff = TimeSpan.Zero) TODO
        {
            PlayingVoice = true;
            if (windowMediaPlayerAvailable)
            {
                Dispatcher.Invoke(() =>
                {
                    VoicePlayer.Clock = new MediaTimeline(VoicePath).CreateClock();
                    VoicePlayer.Clock.Completed += Clock_Completed;
                    VoicePlayer.MediaFailed += MediaPlayer_MediaFailed;
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
            else
            {
                soundPlayer.SoundLocation = VoicePath.LocalPath;
                soundPlayer.LoadAsync();
            }
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
        private void MediaPlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            windowMediaPlayerAvailable = false;
            PlayingVoice = false;
            MessageBoxX.Show("音频播放失败,已尝试自动切换到备用播放器. 如果问题持续,请检查是否已安装WindowsMediaPlayer".Translate(), "音频错误".Translate(), MessageBoxIcon.Warning);
        }
        private void Clock_Completed(object sender, EventArgs e)
        {
            PlayingVoice = false;
            VoicePlayer.Clock = null;
        }
        public bool MoveTimerSmartMove = false;
        private void SmartMoveTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            MoveTimerSmartMove = false;
        }

        private void MoveTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (DisplayType.Type != GraphType.Move || !MoveTimerSmartMove)
            {
                MoveTimer.Stop();
                return;
            }
            Core.Controller.MoveWindows(MoveTimerPoint.X, MoveTimerPoint.Y);

            //if (Core.Controller.GetWindowsDistanceLeft() < -500)
            //{
            //    MessageBox.Show("当前动画移动设计错误: 已到达边界 左侧\n动画名称: {0}\n距离: {1}".Translate(DisplayType.Name, Core.Controller.GetWindowsDistanceLeft()), "MOD移动设计错误".Translate());
            //}
            //else if (Core.Controller.GetWindowsDistanceRight() < -500)
            //{
            //    MessageBox.Show("当前动画移动设计错误: 已到达边界 右侧\n动画名称: {0}\n距离: {1}".Translate(DisplayType.Name, Core.Controller.GetWindowsDistanceRight()), "MOD移动设计错误".Translate());
            //}
            //else if (Core.Controller.GetWindowsDistanceUp() < -500)
            //{
            //    MessageBox.Show("当前动画移动设计错误: 已到达边界 上侧\n动画名称: {0}\n距离: {1}".Translate(DisplayType.Name, Core.Controller.GetWindowsDistanceUp()), "MOD移动设计错误".Translate());
            //}
            //else if (Core.Controller.GetWindowsDistanceDown() < -500)
            //{
            //    MessageBox.Show("当前动画移动设计错误: 已到达边界 下侧\n动画名称: {0}\n距离: {1}".Translate(DisplayType.Name, Core.Controller.GetWindowsDistanceDown()), "MOD移动设计错误".Translate());
            //}
            MoveTimer.Start();
        }
        /// <summary>
        /// 默认点击事件
        /// </summary>
        public Action DefaultClickAction;
        /// <summary>
        /// 默认长按事件
        /// </summary>
        public Action DefaultPressAction;
        public bool isPress = false;
        long presstime;
        private void MainGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isPress = true;
            CountNomal = 0;
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
                    LastInteractionTime = DateTime.Now;
                    foreach (var x in Core.TouchEvent)
                    {
                        if (x.IsPress == true && x.Touch(mp) && x.DoAction())
                            return;
                    }
                    DefaultPressAction?.Invoke();
                }
                else
                {//历遍点击事件
                    LastInteractionTime = DateTime.Now;
                    foreach (var x in Core.TouchEvent)
                    {
                        if (x.IsPress == false && x.Touch(mp) && x.DoAction())
                            return;
                    }
                    //普通点击验证
                    if (DisplayType.Type != GraphType.Default)
                    {//不是nomal! 可能会卡timer,所有全部timer清空下
                        CleanState();
                        if (!IsIdel && State != WorkingState.Sleep && DisplayStop(DisplayToNomal))
                            return;
                    }
                    DefaultClickAction?.Invoke();
                }
            });
        }

        private void MainGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isPress = false;
            if (DisplayType.Type.ToString().StartsWith("Raised"))
            {
                MainGrid.MouseMove -= MainGrid_MouseWave;
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
                    MoveTimerSmartMove = true;
                    SmartMoveTimer.Enabled = false;
                    SmartMoveTimer.Start();
                }
            }
            ((UIElement)e.Source).ReleaseMouseCapture();
        }

        private void MainGrid_MouseMove(object sender, MouseEventArgs e)
        {
            if (!((UIElement)e.Source).CaptureMouse() || !isPress)
            {
                MainGrid.MouseMove -= MainGrid_MouseWave;
                MainGrid.MouseMove -= MainGrid_MouseMove;
                MainGrid.MouseMove += MainGrid_MouseWave;
                rasetype = -1;
                DisplayRaising();
                return;
            }
            var mp = e.GetPosition(MainGrid);
            var x = mp.X - Core.Graph.GraphConfig.RaisePoint[(int)Core.Save.Mode].X;
            var y = mp.Y - Core.Graph.GraphConfig.RaisePoint[(int)Core.Save.Mode].Y;
            if (Math.Abs(x) < 1)
                x = 0;
            if (Math.Abs(y) < 1)
                y = 0;
            Core.Controller.MoveWindows(x, y);
            if (Math.Abs(x) + Math.Abs(y) > 20 && rasetype >= 1)
                rasetype = 0;
        }

        private void MainGrid_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ToolBar.Visibility == Visibility.Visible)
            {
                ToolBar.CloseTimer.Enabled = false;
                ToolBar.Visibility = Visibility.Collapsed;
            }
            else
                ToolBar.Show();
        }

        public void Dispose()
        {
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
            MainGrid.MouseMove -= MainGrid_MouseWave;
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
            if (e.LeftButton == MouseButtonState.Pressed)
                return;
            isPress = false;
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
                        if (wavetimes >= 10 || IsIdel || DisplayType.Type == GraphType.Touch_Head)
                            DisplayTouchHead();
                        //Console.WriteLine(wavetimes);
                        LastInteractionTime = DateTime.Now;
                    }
                    else
                    {
                        if (wavetimes >= 10 || IsIdel || DisplayType.Type == GraphType.Touch_Body)
                            DisplayTouchBody();
                        LastInteractionTime = DateTime.Now;
                    }
            }
        }
    }
}
