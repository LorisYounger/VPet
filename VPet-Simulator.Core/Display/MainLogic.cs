using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        /// 事件Timer
        /// </summary>
        public Timer EventTimer = new Timer(15000)
        {
            AutoReset = true,
            Enabled = true
        };
        /// <summary>
        /// 说话
        /// </summary>
        /// <param name="text">说话内容</param>
        public void Say(string text, GraphCore.Helper.SayType type = GraphCore.Helper.SayType.Shining)
        {
            OnSay?.Invoke(text);
            if (type != GraphCore.Helper.SayType.None && DisplayType == GraphCore.GraphType.Default)
                Display(GraphCore.Helper.Convert(type, GraphCore.Helper.AnimatType.A_Start), () =>
                {
                    Dispatcher.Invoke(() => MsgBar.Show(Core.Save.Name, text, type));
                    Saying(type);
                });
            else
            {
                Dispatcher.Invoke(() => MsgBar.Show(Core.Save.Name, text, type));
            }
        }

        public void Saying(GraphCore.Helper.SayType type)
        {
            Display(GraphCore.Helper.Convert(type, GraphCore.Helper.AnimatType.B_Loop), () => Saying(type));
        }

        private void EventTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Core.Save.CleanChange();
            //所有Handle
            TimeHandle?.Invoke(this);

            if (Core.Controller.EnableFunction)
            {
                //饮食等乱七八糟的消耗
                if (Core.Save.StrengthFood >= 50)
                {
                    Core.Save.StrengthChange(1);
                    if (Core.Save.StrengthFood >= 75)
                        Core.Save.Health += Function.Rnd.Next(0, 1);
                }
                else if (Core.Save.StrengthFood <= 25)
                {
                    Core.Save.Health -= Function.Rnd.Next(0, 1);
                }
                //if (Core.GameSave.Strength <= 40)
                //{
                //    Core.GameSave.Health -= Function.Rnd.Next(0, 1);
                //}
                Core.Save.StrengthChangeFood(-1);
                if (Core.Save.Feeling >= 75)
                {
                    if (Core.Save.Feeling >= 90)
                    {
                        Core.Save.Likability++;
                    }
                    Core.Save.Exp++;
                    Core.Save.Health++;
                }
                else if (Core.Save.Feeling <= 25)
                {
                    Core.Save.Likability--;
                }
                if (Core.Save.StrengthDrink <= 25)
                {
                    Core.Save.Health -= Function.Rnd.Next(0, 1);
                }
                else if (Core.Save.StrengthDrink >= 75)
                    Core.Save.Health += Function.Rnd.Next(0, 1);
                var newmod = Core.Save.CalMode();
                if (Core.Save.Mode != newmod)
                {
                    //TODO:切换逻辑

                    Core.Save.Mode = newmod;
                }
            }
            else
            {
                Core.Save.Mode = GameSave.ModeType.Happy;
                //Core.GameSave.Mode = GameSave.ModeType.Ill;
            }

            //UIHandle
            Dispatcher.Invoke(() => TimeUIHandle.Invoke(this));

            if (DisplayType == GraphCore.GraphType.Default && !isPress)
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
    }
}
