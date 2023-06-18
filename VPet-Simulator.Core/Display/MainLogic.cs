using System;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;

namespace VPet_Simulator.Core
{
    public partial class Main
    {
        public const int DistanceMax = 100;
        public const int DistanceMid = 100;
        public const int DistanceMin = 50;
        public const int LoopProMax = 20;
        public const int LoopMax = 10;
        public const int LoopMid = 7;
        public const int LoopMin = 5;
        public const int TreeRND = 5;

        /// <summary>
        /// 处理说话内容
        /// </summary>
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
        readonly GraphCore.Helper.SayType[] sayTypes = new GraphCore.Helper.SayType[] { GraphCore.Helper.SayType.Serious, GraphCore.Helper.SayType.Shining, GraphCore.Helper.SayType.Self };
        public void SayRnd(string text)
        {
            Say(text, sayTypes[Function.Rnd.Next(sayTypes.Length)]);
        }
        /// <summary>
        /// 说话
        /// </summary>
        /// <param name="text">说话内容</param>
        public void Say(string text, GraphCore.Helper.SayType type = GraphCore.Helper.SayType.Shining, bool force = false)
        {
            Task.Run(() =>
            {
                OnSay?.Invoke(text);
                if (force || type != GraphCore.Helper.SayType.None && DisplayType == GraphCore.GraphType.Default)
                    Display(GraphCore.Helper.Convert(type, GraphCore.Helper.AnimatType.A_Start), () =>
                    {
                        Dispatcher.Invoke(() => MsgBar.Show(Core.Save.Name, text, type));
                        Saying(type);
                    });
                else
                {
                    Dispatcher.Invoke(() => MsgBar.Show(Core.Save.Name, text, type));
                }
            });
        }

        public void Saying(GraphCore.Helper.SayType type)
        {
            Display(GraphCore.Helper.Convert(type, GraphCore.Helper.AnimatType.B_Loop), () => Saying(type));
        }
        int lowstrengthAskCountFood = 1;
        int lowstrengthAskCountDrink = 1;
        private void lowStrengthFood()//未来的Display
        {
            if (Function.Rnd.Next(lowstrengthAskCountFood--) == 0)
            {
                Display(GraphCore.GraphType.Switch_Thirsty, () => Say("肚子饿了,想吃东西", GraphCore.Helper.SayType.Serious, true));//TODO:不同的饥饿说话方式
                lowstrengthAskCountFood = 20;
            }

        }
        private void lowStrengthDrink()//未来的Display
        {
            if (Function.Rnd.Next(lowstrengthAskCountDrink--) == 0)
            {
                Display(GraphCore.GraphType.Switch_Thirsty, () => Say("渴了,想喝东西", GraphCore.Helper.SayType.Serious, true));//TODO:不同的饥饿说话方式
                lowstrengthAskCountDrink = 20;
            }

        }
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
                freedrop = 0.5 * TimePass;
            else
                freedrop = Math.Sqrt(freedrop) * TimePass;
            switch (State)
            {
                case WorkingState.Sleep:
                    //睡觉消耗
                    if (Core.Save.StrengthFood >= 25)
                    {
                        Core.Save.StrengthChange(TimePass * 4);
                        if (Core.Save.StrengthFood >= 75)
                            Core.Save.Health += TimePass * 2;
                    }
                    Core.Save.StrengthChangeFood(-TimePass / 2);
                    Core.Save.StrengthChangeDrink(-TimePass / 2);
                    Core.Save.FeelingChange(-freedrop / 2);
                    break;
                case WorkingState.WorkONE:
                    //工作
                    if (Core.Save.StrengthFood <= 25)
                    {
                        if (Core.Save.Strength >= TimePass)
                        {
                            Core.Save.StrengthChange(-TimePass);
                        }
                        else
                        {
                            Core.Save.Health -= TimePass;
                        }
                        lowStrengthFood();
                        var addmoney = TimePass * 5;
                        Core.Save.Money += addmoney;
                        WorkTimer.GetCount += addmoney;
                    }
                    else
                    {
                        Core.Save.StrengthChangeFood(TimePass);
                        if (Core.Save.StrengthFood >= 75)
                            Core.Save.Health += TimePass;
                        var addmoney = TimePass * (10 + Core.Save.Level / 2);
                        Core.Save.Money += addmoney;
                        WorkTimer.GetCount += addmoney;
                    }
                    Core.Save.StrengthChangeFood(-TimePass * 3.5);
                    Core.Save.StrengthChangeDrink(-TimePass * 2.5);
                    Core.Save.FeelingChange(-freedrop * 1.5);
                    break;
                case WorkingState.WorkTWO:
                    //工作2 更加消耗体力
                    if (Core.Save.StrengthFood <= 25)
                    {
                        if (Core.Save.Strength >= TimePass * 2)
                        {
                            Core.Save.StrengthChange(-TimePass * 2);
                        }
                        else
                        {
                            Core.Save.Health -= TimePass;
                        }
                        lowStrengthFood();
                        var addmoney = TimePass * 10;
                        Core.Save.Money += addmoney;
                        WorkTimer.GetCount += addmoney;
                    }
                    else
                    {
                        if (Core.Save.StrengthFood >= 75)
                            Core.Save.Health += TimePass;
                        var addmoney = TimePass * (20 + Core.Save.Level);
                        Core.Save.Money += addmoney;
                        WorkTimer.GetCount += addmoney;
                    }
                    Core.Save.StrengthChangeFood(-TimePass * 4.5);
                    Core.Save.StrengthChangeDrink(-TimePass * 7.5);
                    Core.Save.FeelingChange(-freedrop * 2.5);
                    break;
                case WorkingState.Study:
                    //学习
                    if (Core.Save.StrengthFood <= 25)
                    {
                        if (Core.Save.Strength >= TimePass)
                        {
                            Core.Save.StrengthChange(-TimePass);
                        }
                        else
                        {
                            Core.Save.Health -= TimePass;
                        }
                        lowStrengthFood();
                        var addmoney = TimePass * (10 + Core.Save.Level);
                        Core.Save.Exp += addmoney;
                        WorkTimer.GetCount += addmoney;
                    }
                    else
                    {
                        Core.Save.StrengthChange(TimePass);
                        if (Core.Save.StrengthFood >= 75)
                            Core.Save.Health += TimePass;
                        var addmoney = TimePass * (30 + Core.Save.Level);
                        Core.Save.Exp += addmoney;
                        WorkTimer.GetCount += addmoney;
                    }
                    Core.Save.StrengthChangeFood(-TimePass * 1.5);
                    Core.Save.StrengthChangeDrink(-TimePass * 2);
                    Core.Save.FeelingChange(-freedrop * 3);
                    goto default;
                default://默认
                    //饮食等乱七八糟的消耗
                    if (Core.Save.StrengthFood >= 50)
                    {
                        Core.Save.StrengthChange(TimePass * 2);
                        if (Core.Save.StrengthFood >= 75)
                            Core.Save.Health += Function.Rnd.Next(0, 2) * TimePass;
                    }
                    else if (Core.Save.StrengthFood <= 25)
                    {
                        Core.Save.Health -= Function.Rnd.Next(0, 1) * TimePass;
                    }
                    Core.Save.StrengthChangeFood(-TimePass * 1.5);
                    Core.Save.StrengthChangeDrink(-TimePass * 1.5);
                    Core.Save.FeelingChange(-freedrop);
                    break;
            }

            //if (Core.GameSave.Strength <= 40)
            //{
            //    Core.GameSave.Health -= Function.Rnd.Next(0, 1);
            //}
            Core.Save.Exp += TimePass;
            //感受提升好感度
            if (Core.Save.Feeling >= 75)
            {
                if (Core.Save.Feeling >= 90)
                {
                    Core.Save.Likability += TimePass;
                }
                Core.Save.Exp += TimePass * 2;
                Core.Save.Health += TimePass;
            }
            else if (Core.Save.Feeling <= 25)
            {
                Core.Save.Likability -= TimePass;
                Core.Save.Exp -= TimePass;
            }
            if (Core.Save.StrengthDrink <= 25)
            {
                Core.Save.Health -= Function.Rnd.Next(0, 1) * TimePass;
                Core.Save.Exp -= TimePass;
                lowStrengthDrink();
            }
            else if (Core.Save.StrengthDrink >= 75)
                Core.Save.Health += Function.Rnd.Next(0, 1) * TimePass;
            var newmod = Core.Save.CalMode();
            if (Core.Save.Mode != newmod)
            {
                //TODO:切换显示动画
                Core.Save.Mode = newmod;
                //TODO:看情况播放停止工作动画
                if (newmod == GameSave.ModeType.Ill && (State != WorkingState.Nomal || State != WorkingState.Sleep))
                {
                    WorkTimer.Stop();
                }
            }
        }

        private void EventTimer_Elapsed(object sender, ElapsedEventArgs e)
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
            Dispatcher.Invoke(() => TimeUIHandle.Invoke(this));

            if (DisplayType == GraphCore.GraphType.Default && !isPress)
                if (Core.Save.Mode == GameSave.ModeType.Ill)
                {//生病时候的随机

                }
                else//TODO:制作随机列表
                    switch (Function.Rnd.Next(Math.Max(20, Core.Controller.InteractionCycle - CountNomal)))
                    {
                        case 0:
                            //随机向右
                            DisplayWalk_Left();
                            break;
                        case 1:
                            DisplayClimb_Left_UP();
                            break;
                        case 2:
                            DisplayClimb_Left_DOWN();
                            break;
                        case 3:
                            DisplayClimb_Right_UP();
                            break;
                        case 4:
                            DisplayClimb_Right_DOWN();
                            break;
                        case 5:
                            DisplayWalk_Right();
                            break;
                        case 6:
                            DisplayFall_Left();
                            break;
                        case 7:
                            DisplayFall_Right();
                            break;
                        case 8:
                            DisplayClimb_Top_Right();
                            break;
                        case 9:
                            DisplayClimb_Top_Left();
                            break;
                        case 10:
                            DisplayCrawl_Left();
                            break;
                        case 11:
                            DisplayCrawl_Right();
                            break;
                        case 13:
                        case 14:
                            DisplaySleep();
                            break;
                        case 15:
                        case 16:
                            DisplayBoring();
                            break;
                        case 18:
                        case 17:
                            DisplaySquat();
                            break;
                        case 12:
                        case 19:
                        case 20:
                            DisplayIdel_StateONE();
                            break;
                        default:
                            break;
                    }

        }
        /// <summary>
        /// 定点移动位置向量
        /// </summary>
        private Point MoveTimerPoint = new Point(0, 0);
        /// <summary>
        /// 定点移动定时器
        /// </summary>
        private Timer MoveTimer = new Timer(125)
        {
            AutoReset = true,
        };
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
                MoveTimer.AutoReset = true;
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
                MoveTimer.AutoReset = false;
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
            /// 正在干活1
            /// </summary>
            WorkONE,
            /// <summary>
            /// 正在干活1
            /// </summary>
            WorkTWO,
            /// <summary>
            /// 学习中 
            /// </summary>
            Study,
            /// <summary>
            /// 睡觉
            /// </summary>
            Sleep,
            ///// <summary>
            ///// 玩耍中
            ///// </summary>
            //Playing,
        }
    }
}
