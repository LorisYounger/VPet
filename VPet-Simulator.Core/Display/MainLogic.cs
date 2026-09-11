using LinePutScript;
using LinePutScript.Localization.WPF;
using Panuon.WPF.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static VPet_Simulator.Core.GraphHelper;
using static VPet_Simulator.Core.GraphInfo;
using static VPet_Simulator.Core.WorkTimer;
using Timer = System.Timers.Timer;

namespace VPet_Simulator.Core
{
    public partial class Main : IPetStatHost
    {
        // ---- 数值模拟的宿主实现 ----
        // PetStatLogic 是与跨平台侧共享的同一份源码, 通过这个窄接口读写状态.
        // 全部显式实现: 元数据里的名字是 VPet_Simulator.Core.IPetStatHost.XXX,
        // 与同名公开字段零冲突, 现有字段一个都不用动 —— 一旦把公开字段挪到基类
        // 或改成属性, 已编译的 MOD 就会失效.
        IGameSave IPetStatHost.Save => Core.Save!;
        Random IPetStatHost.Rnd => Function.Rnd;
        PetWorkingState IPetStatHost.WorkingState => (PetWorkingState)State;
        IWorkDefinition? IPetStatHost.CurrentWork => NowWork;
        DateTime IPetStatHost.LastInteraction
        {
            get => LastInteractionTime;
            set => LastInteractionTime = value;
        }
        void IPetStatHost.AddWorkCount(double value)
        {
            if (WorkTimer != null)
                WorkTimer.GetCount += value;
        }
        void IPetStatHost.RaiseFunctionSpend() => FunctionSpendHandle?.Invoke();
        void IPetStatHost.PlaySwitchAnimat(IGameSave.ModeType before, IGameSave.ModeType after)
            => PlaySwitchAnimat(before, after);
        void IPetStatHost.StopWorkByStateFail()
            => Dispatcher.Invoke(() => WorkTimer?.Stop(reason: FinishWorkInfo.StopReason.StateFail));

        public const int TreeRND = 5;

        /// <summary>
        /// 处理说话内容
        /// </summary>
        [Obsolete("Use SayProcess instead")]
        public event Action<string>? OnSay;
        /// <summary>
        /// 上次交互时间
        /// </summary>
        public DateTime LastInteractionTime { get; set; } = DateTime.Now;
        /// <summary>
        /// 事件Timer
        /// </summary>
        public Timer EventTimer = new Timer(15000)
        {
            AutoReset = true,
            Enabled = true
        };
        /// <summary>
        /// 说话,使用随机表情
        /// </summary>
        public void SayRnd(string text, bool force = false, string? desc = null)
        {
            Say(text, SayRndFunction(text), force, desc);
        }

        /// <summary>
        /// 处理sayInfo,使用随机表情
        /// </summary>
        /// <param name="sayInfo">SayInfoWithStream Class 用于提供stream基本信息 以及基本方法</param>
        public void SayRnd(SayInfoWithStream sayInfo)
        {
            Task.Run(() =>
            {
                while (!sayInfo.IsFinishGen && Function.ComCheck(sayInfo.CurrentText.ToString()) < 4 && sayInfo.CurrentText.Length < 80)
                {
                    Thread.Sleep(100);
                }
                sayInfo.GraphName = SayRndFunction(sayInfo.CurrentText.ToString());
                if (sayInfo.IsFinishGen)
                    Say(sayInfo.ToNoneStream().Result);
                else
                    Say(sayInfo);
            });
        }
        /// <summary>
        /// 随机表情的方法, 修改这个方法可以使用指定类型的说话表情
        /// </summary>
        public Func<string, string> SayRndFunction;
        /// <summary>
        /// 说话处理 (请不要阻塞该处理)
        /// </summary>
        public List<Action<SayInfo>> SayProcess = new List<Action<SayInfo>>();


        /// <summary>
        /// 流式传输的说话
        /// </summary>
        /// <param name="sayInfoWithStream">说话信息</param>
        public void Say(SayInfoWithStream sayInfoWithStream)
        {
            Task.Run(() =>
            {
                sayInfoWithStream.Event_Finish += (text) => OnSay?.Invoke(text);

                if (sayInfoWithStream.IsFinishGen)
                {
                    OnSay?.Invoke(sayInfoWithStream.CurrentText.ToString());
                }

                SayProcess.ForEach(a => a.Invoke(sayInfoWithStream));

                if (sayInfoWithStream.Force || !string.IsNullOrWhiteSpace(sayInfoWithStream.GraphName) && DisplayType.Type == GraphType.Default)//这里不使用idle是因为idle包括学习等
                    Display(sayInfoWithStream.GraphName, AnimatType.A_Start, () =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            MsgBar?.Show(Core.Save!.Name, sayInfoWithStream);
                        });
                        DisplayBLoopingForce(sayInfoWithStream.GraphName!);
                    });
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        MsgBar?.Show(Core.Save!.Name, sayInfoWithStream);
                    });
                }
            });
        }
        /// <summary>
        /// 普通说话
        /// </summary>
        /// <param name="sayinfo">说话信息</param>
        public void Say(SayInfoWithOutStream sayinfo)
        {
            Task.Run(() =>
            {
                OnSay?.Invoke(sayinfo.Text);

                SayProcess.ForEach(a => a.Invoke(sayinfo));

                if (sayinfo.Force || !string.IsNullOrWhiteSpace(sayinfo.GraphName) && DisplayType.Type == GraphType.Default)//这里不使用idle是因为idle包括学习等
                    Display(sayinfo.GraphName, AnimatType.A_Start, () =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            MsgBar?.Show(Core.Save!.Name, sayinfo.Text, sayinfo.GraphName, sayinfo.MsgContent ?? (string.IsNullOrWhiteSpace(sayinfo.Desc) ? null :
                                new TextBlock() { Text = sayinfo.Desc, FontSize = 20, ToolTip = sayinfo.Desc, HorizontalAlignment = HorizontalAlignment.Right }));
                        });
                        DisplayBLoopingForce(sayinfo.GraphName!);
                    });
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        MsgBar?.Show(Core.Save!.Name, sayinfo.Text, sayinfo.GraphName, msgContent: sayinfo.MsgContent ?? (string.IsNullOrWhiteSpace(sayinfo.Desc) ? null :
                            new TextBlock() { Text = sayinfo.Desc, FontSize = 20, ToolTip = sayinfo.Desc, HorizontalAlignment = HorizontalAlignment.Right }));
                    });
                }
            });
        }
        /// <summary>
        /// 说话
        /// </summary>
        /// <param name="text">说话内容</param>
        /// <param name="graphname">图像名</param>
        /// <param name="desc">描述</param>
        /// <param name="force">强制显示图像</param>
        public void Say(string text, string? graphname = null, bool force = false, string? desc = null) => Say(new SayInfoWithOutStream()
        {
            Text = text,
            GraphName = graphname,
            Desc = desc,
            Force = force,
            MsgContent = null
        });
        /// <summary>
        /// 说话
        /// </summary>
        /// <param name="text">说话内容</param>
        /// <param name="graphname">图像名</param>
        /// <param name="msgcontent">消息内容</param>
        /// <param name="force">强制显示图像</param>
        public void Say(string text, UIElement msgcontent, string? graphname = null, bool force = false) => Say(new SayInfoWithOutStream()
        {
            Text = text,
            GraphName = graphname,
            Desc = null,
            Force = force,
            MsgContent = msgcontent
        });

        int labeldisplaycount = 100;
        int labeldisplayhash = 0;
        Timer labeldisplaytimer = new Timer(10)
        {
            AutoReset = true,
        };
        double labeldisplaychangenum1 = 0;
        double labeldisplaychangenum2 = 0;
        /// <summary>
        /// 显示消息弹窗Label
        /// </summary>
        /// <param name="text">文本</param>
        /// <param name="time">持续时间</param>
        public void LabelDisplayShow(string text, int time = 2000)
        {
            labeldisplayhash = text.GetHashCode();
            Dispatcher.Invoke(() =>
            {
                LabelDisplayText.Text = text;
                LabelDisplay.Opacity = 1;
                LabelDisplay.Visibility = Visibility.Visible;
                labeldisplaycount = time / 10;
                labeldisplaytimer.Start();
            });
        }
        /// <summary>
        /// 显示消息弹窗Lable,自动统计数值变化
        /// </summary>
        /// <param name="text">文本, 使用{0:f2}</param>
        /// <param name="changenum1">变化值1</param>
        /// <param name="changenum2">变化值2</param>
        /// <param name="time">持续时间</param>
        public void LabelDisplayShowChangeNumber(string text, double changenum1, double changenum2 = 0, int time = 2000)
        {
            if (labeldisplayhash == text.GetHashCode())
            {
                labeldisplaychangenum1 += changenum1;
                labeldisplaychangenum2 += changenum2;
            }
            else
            {
                labeldisplaychangenum1 = changenum1;
                labeldisplaychangenum2 = changenum2;
                labeldisplayhash = text.GetHashCode();
            }
            Dispatcher.Invoke(() =>
            {
                LabelDisplayText.Text = string.Format(text, labeldisplaychangenum1, labeldisplaychangenum2);
                LabelDisplay.Opacity = 1;
                LabelDisplay.Visibility = Visibility.Visible;
                labeldisplaycount = time / 10;
                labeldisplaytimer.Start();
            });
        }
        public Work? NowWork;
        /// <summary>
        /// 根据消耗计算相关数据
        /// </summary>
        /// <param name="TimePass">过去时间倍率</param>
        public void FunctionSpend(double TimePass)
        {
            // 实现在平台无关的 PetStatLogic 里, 与跨平台版编译同一份源码.
            // 这段数值演化决定了整个游戏的平衡性, 两个平台一旦分叉就是"手感不一样"
            // 这种没有报错、极难回溯的问题.
            PetStatLogic.FunctionSpend(this, TimePass);
        }
        /// <summary>
        /// 播放切换动画
        /// </summary>
        /// <param name="before">切换前状态</param>
        /// <param name="after">切换后状态</param>
        public void PlaySwitchAnimat(IGameSave.ModeType before, IGameSave.ModeType after)
        {
            if (!(DisplayType.Type == GraphType.Default || DisplayType.Type == GraphType.Switch_Down || DisplayType.Type == GraphType.Switch_Up))
            {
                return;
            }
            if (before == after)
            {
                DisplayToNomal();
                return;
            }
            if (before < after)
            {
                Display(Core.Graph!.FindGraph(Core.Graph!.FindName(GraphType.Switch_Down), AnimatType.Single, before),
                    () => PlaySwitchAnimat((IGameSave.ModeType)(((int)before) + 1), after));
            }
            else
            {
                Display(Core.Graph!.FindGraph(Core.Graph!.FindName(GraphType.Switch_Up), AnimatType.Single, before),
                    () => PlaySwitchAnimat((IGameSave.ModeType)(((int)before) - 1), after));
            }
        }
        /// <summary>
        /// 状态计算Handle
        /// </summary>
        public event Action? FunctionSpendHandle;
        /// <summary>
        /// 想要随机显示的接口 (return:是否成功)
        /// </summary>
        public List<Func<bool>> RandomInteractionAction = new List<Func<bool>>();
        /// <summary>
        /// 判断是否是闲置状态
        /// </summary>
        public bool IsIdel => (DisplayType.Type == GraphType.Default || DisplayType.Type == GraphType.Work) && !isPress;

        /// <summary>
        /// 每隔指定时间自动触发计算 可以关闭EventTimer后手动计算
        /// </summary>
        public void EventTimer_Elapsed()
        {
            //所有Handle
            TimeHandle?.Invoke(this);
            if (Core.Controller!.EnableFunction)
            {
                FunctionSpend(0.05);
            }
            else
            {
                //Core.Save!.Mode = GameSave.ModeType.Happy;
                //Core.GameSave.Mode = GameSave.ModeType.Ill;
                Core.Save!.Mode = NoFunctionMOD;
            }

            //UIHandle
            Dispatcher.Invoke(() => TimeUIHandle?.Invoke(this));

            if (IsIdel)
            {
                int rnddisplay = Math.Max(20, Core.Controller!.InteractionCycle - CountNomal);
                if (DisplayType.Type == GraphType.Work)
                    rnddisplay = 2 * rnddisplay + 20;
                switch (Function.Rnd.Next(rnddisplay))
                {
                    case 0:
                    case 1:
                    case 2:
                        //显示移动
                        DisplayMove();
                        break;
                    case 3:
                    case 4:
                    case 5:
                        //显示待机
                        DisplayIdel();
                        break;
                    case 6:
                        DisplayIdel_StateONE();
                        break;
                    case 7:
                        DisplaySleep();
                        break;
                    case 8:
                    case 9:
                    case 10:
                        //给其他显示留个机会
                        var list = RandomInteractionAction.ToList();
                        for (int i = Function.Rnd.Next(list.Count); 0 != list.Count; i = Function.Rnd.Next(list.Count))
                        {
                            var act = list[i];
                            if (act.Invoke())
                            {
                                break;
                            }
                            else
                            {
                                list.RemoveAt(i);
                            }
                        }
                        break;
                }
            }
        }
        /// <summary>
        /// 边缘检查和回正检测, 如果有靠边就进入侧边隐藏模式, 没有就回正
        /// </summary>
        /// <returns>是否成功进入侧边隐藏模式</returns>
        private bool MoveSideHideCheck()
        {
            var result = Core.Controller!.IfInActivateScreen();
            if (result == false && Core.Controller!.AutoChangeWindow == true)
            {
                Core.Controller!.SetNowScreenActivate();
            }
            //判断是否靠边,如果靠边就进入侧边隐藏模式
            if (Core.Controller!.GetWindowsDistanceLeft() < -50 * Core.Controller!.ZoomRatio)
            {
                //检查下是否有SideLoad
                if (Core.Graph!.FindName(GraphType.SideHide_Left_Main) != null)
                {
                    Core.Controller!.MoveWindows(-Core.Controller!.GetWindowsDistanceLeft() / Core.Controller!.ZoomRatio - Core.Graph!.GraphConfig.Data["side"][(gdbe)"left"], 0);
                    if (Core.Controller!.GetWindowsDistanceDown() < 0) Core.Controller!.MoveWindows(0, Core.Controller!.GetWindowsDistanceDown() / Core.Controller!.ZoomRatio - 100);
                    else if (Core.Controller!.GetWindowsDistanceUp() < 0) Core.Controller!.MoveWindows(0, -Core.Controller!.GetWindowsDistanceUp() / Core.Controller!.ZoomRatio);
                    Display(GraphType.SideHide_Left_Main, AnimatType.A_Start, DisplayBLoopingForce);
                    return true;
                }
                else if (Core.Controller!.RePositionActive)
                {//没有就回正
                    Core.Controller!.MoveWindows(-Core.Controller!.GetWindowsDistanceLeft() / Core.Controller!.ZoomRatio, 0);
                }
            }
            else if (Core.Controller!.GetWindowsDistanceRight() < -50 * Core.Controller!.ZoomRatio)
            {
                if (Core.Graph!.FindName(GraphType.SideHide_Right_Main) != null)
                {
                    Core.Controller!.MoveWindows(Core.Controller!.GetWindowsDistanceRight() / Core.Controller!.ZoomRatio + 500 - Core.Graph!.GraphConfig.Data["side"][(gdbe)"right"], 0);
                    if (Core.Controller!.GetWindowsDistanceDown() < 0) Core.Controller!.MoveWindows(0, Core.Controller!.GetWindowsDistanceDown() / Core.Controller!.ZoomRatio - 100);
                    else if (Core.Controller!.GetWindowsDistanceUp() < 0) Core.Controller!.MoveWindows(0, -Core.Controller!.GetWindowsDistanceUp() / Core.Controller!.ZoomRatio);
                    Display(GraphType.SideHide_Right_Main, AnimatType.A_Start, DisplayBLoopingForce);
                    return true;
                }
                else if (Core.Controller!.RePositionActive)
                {
                    Core.Controller!.MoveWindows(Core.Controller!.GetWindowsDistanceRight() / Core.Controller!.ZoomRatio, 0);
                }
            }
            return false;
        }

        /// <summary>
        /// 定点移动位置向量
        /// </summary>
        public Point MoveTimerPoint = new Point(0, 0);
        /// <summary>
        /// 定点移动定时器
        /// </summary>
        public Timer MoveTimer = new Timer();
        /// <summary>
        /// 设置计算间隔
        /// </summary>
        /// <param name="Interval">计算间隔</param>
        public void SetLogicInterval(int Interval)
        {
            EventTimer.Interval = Interval;
        }
        private Timer SmartMoveTimer = new Timer(20 * 60)
        {
            AutoReset = true,
        };
        /// <summary>
        /// 是否启用智能移动
        /// </summary>
        private bool SmartMove;
        /// <summary>
        /// 设置移动模式
        /// </summary>
        /// <param name="AllowMove">允许移动</param>
        /// <param name="smartMove">启用智能移动</param>
        /// <param name="SmartMoveInterval">智能移动周期</param>
        public void SetMoveMode(bool AllowMove, bool smartMove, int SmartMoveInterval)
        {
            MoveTimer.Enabled = false;
            if (AllowMove)
            {
                MoveTimerSmartMove = true;
                if (smartMove)
                {
                    SmartMoveTimer.Interval = SmartMoveInterval;
                    SmartMoveTimer.Start();
                    SmartMove = true;
                }
                else
                {
                    SmartMoveTimer.Enabled = false;
                    SmartMove = false;
                }
            }
            else
            {
                MoveTimerSmartMove = false;
            }
        }
        /// <summary>
        /// 当前状态
        /// </summary>
        public WorkingState State = WorkingState.Nomal;

        /// <summary>
        /// 当前正在的状态
        /// </summary>
        public enum WorkingState
        {
            /// <summary>
            /// 默认:啥都没干
            /// </summary>
            Nomal,
            /// <summary>
            /// 正在干活/学习中
            /// </summary>
            Work,
            /// <summary>
            /// 睡觉
            /// </summary>
            Sleep,
            /// <summary>
            /// 旅游中
            /// </summary>
            Travel,
            /// <summary>
            /// 其他状态,给开发者留个空位计算
            /// </summary>
            Empty,
        }
        /// <summary>
        /// 获得工作列表分类
        /// </summary>
        /// <param name="ws">所有工作</param>
        /// <param name="ss">所有学习</param>
        /// <param name="ps">所有娱乐</param>
        public void WorkList(out List<Work> ws, out List<Work> ss, out List<Work> ps)
        {
            ws = new List<Work>();
            ss = new List<Work>();
            ps = new List<Work>();
            foreach (var w in Core.Graph!.GraphConfig.Works)
            {
                switch (w.Type)
                {
                    case Work.WorkType.Study:
                        ss.Add(w);
                        break;
                    case Work.WorkType.Work:
                        ws.Add(w);
                        break;
                    case Work.WorkType.Play:
                        ps.Add(w);
                        break;
                }
            }
        }
        /// <summary>
        /// 工作检测
        /// </summary>
        public Func<Work, bool>? WorkCheck;
        /// <summary>
        /// 开始工作
        /// </summary>
        /// <param name="work">工作内容</param>
        public bool StartWork(Work? work)
        {
            if (work == null)
                return false;
            if (!Core.Controller!.EnableFunction || Core.Save!.Mode != IGameSave.ModeType.Ill)
                if (!Core.Controller!.EnableFunction || Core.Save!.Level >= work.LevelLimit)
                    if (State == Main.WorkingState.Work && NowWork?.Name == work.Name)
                        WorkTimer?.Stop(reason: FinishWorkInfo.StopReason.MenualStop);
                    else
                    {
                        if (WorkCheck != null && !WorkCheck.Invoke(work))
                            return false;
                        WorkTimer?.Start(work);
                        return true;
                    }
                else
                    MessageBoxX.Show(LocalizeCore.Translate("您的桌宠等级不足{0}/{2}\n无法进行{1}", Core.Save!.Level.ToString()
                        , work.NameTrans, work.LevelLimit), LocalizeCore.Translate("{0}取消", work.NameTrans));
            else
                MessageBoxX.Show(LocalizeCore.Translate("您的桌宠 {0} 生病啦,没法进行{1}", Core.Save!.Name,
                  work.NameTrans), LocalizeCore.Translate("{0}取消", work.NameTrans));
            return false;
        }
        /// <summary>
        /// 任务开始时调用该参数
        /// </summary>
        public event Action<Work>? Event_WorkStart;
        internal void Event_WorkStartInvoke(Work work)
        {
            Event_WorkStart?.Invoke(work);
        }
        /// <summary>
        /// 任务完成时调用该参数 (重定向至WorkTimer.E_FinishWork)
        /// </summary>
        public event Action<FinishWorkInfo> Event_WorkEnd
        {
            add
            {
                if (WorkTimer != null)
                    WorkTimer.E_FinishWork += value;
            }
            remove
            {
                if (WorkTimer != null)
                    WorkTimer.E_FinishWork -= value;
            }
        }
        /// <summary>
        /// 移动开始前(未播放动画)调用该参数
        /// </summary>
        public event Action<Move>? Event_MoveStart;
        /// <summary>
        /// 移动结束后(播放完动画)调用该参数
        /// </summary>
        public event Action<Move>? Event_MoveEnd;
        internal void Event_MoveStartInvoke(Move move)
        {
            Event_MoveStart?.Invoke(move);
        }
        internal void Event_MoveEndInvoke(Move move)
        {
            Event_MoveEnd?.Invoke(move);
        }
    }
}
