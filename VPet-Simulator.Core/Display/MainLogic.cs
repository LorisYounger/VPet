using LinePutScript.Localization.WPF;
using Panuon.WPF.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using static System.Net.Mime.MediaTypeNames;
using static VPet_Simulator.Core.GraphHelper;
using static VPet_Simulator.Core.GraphInfo;
using static VPet_Simulator.Core.WorkTimer;

namespace VPet_Simulator.Core
{
    public partial class Main
    {
        public const int TreeRND = 5;

        /// <summary>
        /// 处理说话内容
        /// </summary>
        [Obsolete("Use SayProcess instead")]
        public event Action<string> OnSay;
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
        public void SayRnd(string text, bool force = false, string desc = null)
        {
            Say(text, SayRndFunction(text), force, desc);// 根据现有内容 如果SayRndFunction被自定义 可能会出现问题
        }
        /// <summary>
        /// 处理sayInfo,使用随机表情
        /// </summary>
        /// <param name="sayInfo">SayInfoWithStream Class 用于提供stream基本信息 以及基本方法</param>
        public void SayRnd(SayInfoWithStream sayInfo)
        {
            Task.Run(() =>
            {


            });
        }
        /// <summary>
        /// 随机表情的方法, 修改这个方法可以使用指定类型的说话表情
        /// </summary>
        public Func<string, string> SayRndFunction;
        /// <summary>
        /// 说话处理
        /// </summary>
        public List<Action<SayInfo>> SayProcess = new List<Action<SayInfo>>();


        /// <summary>
        /// 流式传输的说话
        /// </summary>
        /// <param name="sayInfoWithStream">对话更新的注册函数</param>
        public void Say(SayInfoWithStream sayInfoWithStream)
        {
            Task.Run(() =>
            {
                sayInfoWithStream.Event_Finish += (text) => OnSay?.Invoke(text);

                if (sayInfoWithStream.FinishGen)
                {
                    OnSay?.Invoke(sayInfoWithStream.CurrentText.ToString());
                }

                SayProcess.ForEach(a => a.Invoke(sayInfoWithStream));

                if (sayInfoWithStream.Force || !string.IsNullOrWhiteSpace(sayInfoWithStream.GraphName) && DisplayType.Type == GraphType.Default)//这里不使用idle是因为idle包括学习等
                    Display(sayInfoWithStream.GraphName, AnimatType.A_Start, () =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            MsgBar.Show(Core.Save.Name, sayInfoWithStream);
                        });
                        DisplayBLoopingForce(sayInfoWithStream.GraphName);
                    });
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        MsgBar.Show(Core.Save.Name, sayInfoWithStream);
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
        public void Say(string text, string graphname = null, bool force = false, string desc = null)
        {
            Task.Run(() =>
            {
                OnSay?.Invoke(text);
                var sayinfo = new SayInfoWithOutStream()
                {
                    Text = text,
                    GraphName = graphname,
                    Desc = desc,
                    Force = force,
                    MsgContent = null
                };
                SayProcess.ForEach(a => a.Invoke(sayinfo));

                if (sayinfo.Force || !string.IsNullOrWhiteSpace(sayinfo.GraphName) && DisplayType.Type == GraphType.Default)//这里不使用idle是因为idle包括学习等
                    Display(sayinfo.GraphName, AnimatType.A_Start, () =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            MsgBar.Show(Core.Save.Name, sayinfo.Text, sayinfo.GraphName, sayinfo.MsgContent ?? (string.IsNullOrWhiteSpace(sayinfo.Desc) ? null :
                                new TextBlock() { Text = sayinfo.Desc, FontSize = 20, ToolTip = sayinfo.Desc, HorizontalAlignment = HorizontalAlignment.Right }));
                        });
                        DisplayBLoopingForce(sayinfo.GraphName);
                    });
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        MsgBar.Show(Core.Save.Name, sayinfo.Text, sayinfo.GraphName, msgContent: sayinfo.MsgContent ?? (string.IsNullOrWhiteSpace(sayinfo.Desc) ? null :
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
        /// <param name="msgcontent">消息内容</param>
        /// <param name="force">强制显示图像</param>
        public void Say(string text, UIElement msgcontent, string graphname = null, bool force = false)
        {
            Task.Run(() =>
            {
                OnSay?.Invoke(text);
                var sayinfo = new SayInfoWithOutStream()
                {
                    Text = text,
                    GraphName = graphname,
                    Desc = null,
                    Force = force,
                    MsgContent = msgcontent
                };
                SayProcess.ForEach(a => a.Invoke(sayinfo));
                if (sayinfo.Force || !string.IsNullOrWhiteSpace(sayinfo.GraphName) && DisplayType.Type == GraphType.Default)//这里不使用idle是因为idle包括学习等
                    Display(sayinfo.GraphName, AnimatType.A_Start, () =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            MsgBar.Show(Core.Save.Name, sayinfo.Text, sayinfo.GraphName, sayinfo.MsgContent ?? (string.IsNullOrWhiteSpace(sayinfo.Desc) ? null :
                                new TextBlock() { Text = sayinfo.Desc, FontSize = 20, ToolTip = sayinfo.Desc, HorizontalAlignment = HorizontalAlignment.Right }));
                        });
                        DisplayBLoopingForce(sayinfo.GraphName);
                    });
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        MsgBar.Show(Core.Save.Name, sayinfo.Text, sayinfo.GraphName, msgContent: sayinfo.MsgContent ?? (string.IsNullOrWhiteSpace(sayinfo.Desc) ? null :
                            new TextBlock() { Text = sayinfo.Desc, FontSize = 20, ToolTip = sayinfo.Desc, HorizontalAlignment = HorizontalAlignment.Right }));
                    });
                }
            });
        }

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
        public Work NowWork;
        /// <summary>
        /// 根据消耗计算相关数据
        /// </summary>
        /// <param name="TimePass">过去时间倍率</param>
        public void FunctionSpend(double TimePass)
        {
            Core.Save.CleanChange();
            Core.Save.StoreTake();
            double freedrop = (DateTime.Now - LastInteractionTime).TotalMinutes;
            if (freedrop < 1)
                freedrop = 0;
            else
                freedrop = Math.Min(Math.Sqrt(freedrop) * TimePass / 4, Core.Save.FeelingMax / 800);
            double sm25 = Core.Save.StrengthMax * 0.25;
            double sm50 = Core.Save.StrengthMax * 0.5;
            double sm60 = Core.Save.StrengthMax * 0.6;
            double sm75 = Core.Save.StrengthMax * 0.75;

            switch (State)
            {
                case WorkingState.Empty:
                    break;
                case WorkingState.Sleep:
                    //睡觉 缓慢恢复所有(除了心情,但是心情不会下降)
                    Core.Save.StrengthChange(TimePass * 2);
                    Core.Save.StrengthChangeFood(TimePass);
                    if (Core.Save.StrengthFood <= sm25)
                    {//低状态2倍恢复速度
                        Core.Save.StrengthChangeFood(TimePass);
                    }
                    else if (Core.Save.StrengthFood >= sm75)
                        Core.Save.Health += TimePass * 2;
                    Core.Save.StrengthChangeDrink(TimePass);
                    if (Core.Save.StrengthDrink >= sm25)
                    {
                        Core.Save.StrengthChangeDrink(TimePass);
                    }
                    else if (Core.Save.StrengthDrink >= sm75)
                        Core.Save.Health += TimePass * 2;
                    LastInteractionTime = DateTime.Now;
                    break;
                case WorkingState.Work:
                    if (NowWork == null)
                        break;
                    var needfood = TimePass * NowWork.StrengthFood;
                    var needdrink = TimePass * NowWork.StrengthDrink;

                    double efficiency = 0;
                    int addhealth = -2;


                    var nsfood = needfood * .3;
                    var nsdrink = needdrink * .3;
                    if (Core.Save.Strength > sm25 + nsfood + nsdrink)
                    {//可以用体力减少一些消耗,并且增加效率
                        Core.Save.StrengthChange(-nsfood - nsdrink);
                        efficiency += 0.1;
                        needfood -= nsfood;
                        needdrink -= nsdrink;
                    }

                    if (Core.Save.StrengthFood <= sm25)
                    {//低状态低效率
                        Core.Save.StrengthChangeFood(-needfood / 2);
                        efficiency += 0.2;
                        if (Core.Save.Strength >= needfood)
                        {
                            Core.Save.StrengthChange(-needfood);
                            efficiency += 0.1;
                        }
                        addhealth -= 2;
                    }
                    else
                    {
                        Core.Save.StrengthChangeFood(-needfood);
                        efficiency += 0.4;
                        if (Core.Save.StrengthFood >= sm60)
                        {
                            addhealth += Function.Rnd.Next(1, 3);
                            efficiency += 0.1;
                        }
                    }
                    if (Core.Save.StrengthDrink <= sm25)
                    {//低状态低效率
                        Core.Save.StrengthChangeDrink(-needdrink / 2);
                        efficiency += 0.2;
                        if (Core.Save.Strength >= needdrink)
                        {
                            Core.Save.StrengthChange(-needdrink);
                            efficiency += 0.1;
                        }
                        addhealth -= 2;
                    }
                    else
                    {
                        Core.Save.StrengthChangeDrink(-needdrink);
                        efficiency += 0.4;
                        if (Core.Save.StrengthDrink >= sm60)
                        {
                            addhealth += Function.Rnd.Next(1, 3);
                            efficiency += 0.1;
                        }
                    }
                    if (addhealth > 0)
                        Core.Save.Health += addhealth * TimePass;
                    var addmoney = Math.Max(0, TimePass * NowWork.MoneyBase * (2 * efficiency - 0.5));
                    if (NowWork.Type == Work.WorkType.Work)
                        Core.Save.Money += addmoney;
                    else
                        Core.Save.Exp += addmoney;
                    WorkTimer.GetCount += addmoney;
                    if (NowWork.Type == Work.WorkType.Play)
                    {
                        LastInteractionTime = DateTime.Now;
                        Core.Save.FeelingChange(-NowWork.Feeling * TimePass);
                    }
                    else
                        Core.Save.FeelingChange(-freedrop * (0.5 + NowWork.Feeling / 2));
                    break;
                default://默认
                    //饮食等乱七八糟的消耗
                    addhealth = -2;
                    if (Core.Save.StrengthFood >= sm50)
                    {
                        Core.Save.StrengthChangeFood(-TimePass);
                        Core.Save.StrengthChange(TimePass);
                        if (Core.Save.StrengthFood >= sm75)
                            addhealth += Function.Rnd.Next(1, 3);
                    }
                    else if (Core.Save.StrengthFood <= sm25)
                    {
                        Core.Save.Health -= Function.Rnd.NextDouble() * TimePass;
                        addhealth -= 2;
                    }
                    if (Core.Save.StrengthDrink >= sm50)
                    {
                        Core.Save.StrengthChangeDrink(-TimePass);
                        Core.Save.StrengthChange(TimePass);
                        if (Core.Save.StrengthDrink >= sm75)
                            addhealth += Function.Rnd.Next(1, 3);
                    }
                    else if (Core.Save.StrengthDrink <= sm25)
                    {
                        Core.Save.Health -= Function.Rnd.NextDouble() * TimePass;
                        addhealth -= 2;
                    }
                    if (addhealth > 0)
                        Core.Save.Health += addhealth * TimePass;
                    Core.Save.StrengthChangeFood(-TimePass);
                    Core.Save.StrengthChangeDrink(-TimePass);
                    Core.Save.FeelingChange(-freedrop);
                    break;
            }

            //if (Core.GameSave.Strength <= 40)
            //{
            //    Core.GameSave.Health -= Function.Rnd.Next(0, 1);
            //}
            Core.Save.Exp += TimePass;
            //感受提升好感度
            if (Core.Save.Feeling >= Core.Save.FeelingMax * 0.75)
            {
                if (Core.Save.Feeling >= Core.Save.FeelingMax * 0.90)
                {
                    Core.Save.Likability += TimePass;
                }
                Core.Save.Exp += TimePass * 2;
                Core.Save.Health += TimePass;
            }
            else if (Core.Save.Feeling <= 25) //这个就不乘倍率了, 给上限高一些好处
            {
                Core.Save.Likability -= TimePass;
                Core.Save.Exp -= TimePass;
            }
            if (Core.Save.StrengthDrink <= sm25)
            {
                Core.Save.Health -= Function.Rnd.Next(0, 1) * TimePass;
                Core.Save.Exp -= TimePass;
            }
            else if (Core.Save.StrengthDrink >= sm75)
                Core.Save.Health += Function.Rnd.Next(0, 1) * TimePass;

            FunctionSpendHandle?.Invoke();
            var newmod = Core.Save.CalMode();
            if (Core.Save.Mode != newmod)
            {
                //切换显示动画
                PlaySwitchAnimat(Core.Save.Mode, newmod);

                Core.Save.Mode = newmod;
            }
            //看情况播放停止工作动画
            if (Core.Save.Mode == IGameSave.ModeType.Ill && State == WorkingState.Work)
            {
                Dispatcher.Invoke(() => WorkTimer.Stop(reason: FinishWorkInfo.StopReason.StateFail));
            }
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
                Display(Core.Graph.FindGraph(Core.Graph.FindName(GraphType.Switch_Down), AnimatType.Single, before),
                    () => PlaySwitchAnimat((IGameSave.ModeType)(((int)before) + 1), after));
            }
            else
            {
                Display(Core.Graph.FindGraph(Core.Graph.FindName(GraphType.Switch_Up), AnimatType.Single, before),
                    () => PlaySwitchAnimat((IGameSave.ModeType)(((int)before) - 1), after));
            }
        }
        /// <summary>
        /// 状态计算Handle
        /// </summary>
        public event Action FunctionSpendHandle;
        /// <summary>
        /// 想要随机显示的接口 (return:是否成功)
        /// </summary>
        public List<Func<bool>> RandomInteractionAction = new List<Func<bool>>();

        public bool IsIdel => (DisplayType.Type == GraphType.Default || DisplayType.Type == GraphType.Work) && !isPress;

        /// <summary>
        /// 每隔指定时间自动触发计算 可以关闭EventTimer后手动计算
        /// </summary>
        public void EventTimer_Elapsed()
        {
            //所有Handle
            TimeHandle?.Invoke(this);
            if (Core.Controller.EnableFunction)
            {
                FunctionSpend(0.05);
            }
            else
            {
                //Core.Save.Mode = GameSave.ModeType.Happy;
                //Core.GameSave.Mode = GameSave.ModeType.Ill;
                Core.Save.Mode = NoFunctionMOD;
            }

            //UIHandle
            Dispatcher.Invoke(() => TimeUIHandle?.Invoke(this));
            if (IsIdel)
            {
                int rnddisplay = Math.Max(20, Core.Controller.InteractionCycle - CountNomal);
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
            foreach (var w in Core.Graph.GraphConfig.Works)
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
        public Func<Work, bool> WorkCheck;
        /// <summary>
        /// 开始工作
        /// </summary>
        /// <param name="work">工作内容</param>
        public bool StartWork(Work work)
        {
            if (!Core.Controller.EnableFunction || Core.Save.Mode != IGameSave.ModeType.Ill)
                if (!Core.Controller.EnableFunction || Core.Save.Level >= work.LevelLimit)
                    if (State == Main.WorkingState.Work && NowWork.Name == work.Name)
                        WorkTimer.Stop(reason: FinishWorkInfo.StopReason.MenualStop);
                    else
                    {
                        if (WorkCheck != null && !WorkCheck.Invoke(work))
                            return false;
                        WorkTimer.Start(work);
                        return true;
                    }
                else
                    MessageBoxX.Show(LocalizeCore.Translate("您的桌宠等级不足{0}/{2}\n无法进行{1}", Core.Save.Level.ToString()
                        , work.NameTrans, work.LevelLimit), LocalizeCore.Translate("{0}取消", work.NameTrans));
            else
                MessageBoxX.Show(LocalizeCore.Translate("您的桌宠 {0} 生病啦,没法进行{1}", Core.Save.Name,
                  work.NameTrans), LocalizeCore.Translate("{0}取消", work.NameTrans));
            return false;
        }
        /// <summary>
        /// 任务开始时调用该参数
        /// </summary>
        public event Action<Work> Event_WorkStart;
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
                WorkTimer.E_FinishWork += value;
            }
            remove
            {
                WorkTimer.E_FinishWork -= value;
            }
        }
    }
}
