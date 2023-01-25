using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using static VPet_Simulator.Core.GraphCore;

namespace VPet_Simulator.Core
{
    public partial class Main
    {
        /// <summary>
        /// 当前动画类型
        /// </summary>
        public GraphCore.GraphType DisplayType = GraphCore.GraphType.Default;
        /// <summary>
        /// 默认循环次数
        /// </summary>
        public int CountNomal = 0;
        /// <summary>
        /// 显示默认情况
        /// </summary>
        public void DisplayNomal()
        {
            CountNomal++;
            Display(GraphCore.GraphType.Default, DisplayNomal);
        }
        /// <summary>
        /// 显示结束动画
        /// </summary>
        /// <param name="EndAction">结束后接下来</param>
        /// <returns>是否成功结束</returns>
        public bool DisplayStopMove(Action EndAction)
        {
            switch (DisplayType)
            {
                case GraphCore.GraphType.Boring_B_Loop:
                    Display(GraphCore.GraphType.Boring_C_End, EndAction);
                    return true;
                case GraphCore.GraphType.Squat_B_Loop:
                    Display(GraphCore.GraphType.Squat_C_End, EndAction);
                    return true;
                case GraphType.Crawl_Left_B_Loop:
                    Display(GraphCore.GraphType.Crawl_Left_C_End, EndAction);
                    return true;
                case GraphType.Crawl_Right_B_Loop:
                    Display(GraphCore.GraphType.Crawl_Right_C_End, EndAction);
                    return true;
                case GraphType.Fall_Left_B_Loop:
                    Display(GraphCore.GraphType.Fall_Left_C_End, EndAction);
                    return true;
                case GraphType.Fall_Right_B_Loop:
                    Display(GraphCore.GraphType.Fall_Right_C_End, EndAction);
                    return true;
                case GraphType.Walk_Left_B_Loop:
                    Display(GraphCore.GraphType.Walk_Left_C_End, EndAction);
                    return true;
                case GraphType.Walk_Right_B_Loop:
                    Display(GraphCore.GraphType.Walk_Right_C_End, EndAction);
                    return true;
                case GraphType.Sleep_B_Loop:
                    Display(GraphCore.GraphType.Sleep_C_End, EndAction);
                    return true;
                    //case GraphType.Climb_Left:
                    //case GraphType.Climb_Right:
                    //case GraphType.Climb_Top_Left:
                    //case GraphType.Climb_Top_Right:
                    //    DisplayFalled_Left();
                    //    return true;
            }
            return false;
        }

        /// <summary>
        /// 显示摸头情况
        /// </summary>
        public void DisplayTouchHead()
        {
            if (Core.Save.Strength <= DistanceMin)
                return;
            Core.Save.StrengthChange(-1);
            Core.Save.FeelingChange(1);
            if (DisplayType == GraphType.Touch_Head_B_Loop)
                if (PetGrid.Child is IGraph ig && ig.GraphType == GraphCore.GraphType.Touch_Head_B_Loop)
                {
                    ig.IsContinue = true;
                    return;
                }
                else if (PetGrid2.Child is IGraph ig2 && ig2.GraphType == GraphCore.GraphType.Touch_Head_B_Loop)
                {
                    ig2.IsContinue = true;
                    return;
                }
            Core.Graph.RndGraph.Clear();
            Display(GraphCore.GraphType.Touch_Head_A_Start, () =>
               Display(GraphCore.GraphType.Touch_Head_B_Loop, () =>
               Display(GraphCore.GraphType.Touch_Head_C_End, DisplayNomal
            )));
        }

        /// <summary>
        /// 显示无聊情况
        /// </summary>
        public void DisplayBoring()
        {
            looptimes = 0;
            CountNomal = 0;
            Core.Graph.RndGraph.Clear();
            Display(GraphCore.GraphType.Boring_A_Start, DisplayBoringing);
        }
        /// <summary>
        /// 显示无聊情况
        /// </summary>
        private void DisplayBoringing()
        {
            if (Function.Rnd.Next(++looptimes) > LoopProMax)
                Display(GraphCore.GraphType.Boring_C_End, DisplayNomal);
            else
                Display(GraphCore.GraphType.Boring_B_Loop, DisplayBoringing);
        }
        int looptimes;
        /// <summary>
        /// 显示蹲下情况
        /// </summary>
        public void DisplaySquat()
        {
            looptimes = 0;
            CountNomal = 0;
            Core.Graph.RndGraph.Clear();
            Display(GraphCore.GraphType.Squat_A_Start, DisplaySquating);
        }
        /// <summary>
        /// 显示蹲下情况
        /// </summary>
        public void DisplaySquating()
        {
            if (Function.Rnd.Next(++looptimes) > LoopProMax)
                Display(GraphCore.GraphType.Squat_C_End, DisplayNomal);
            else
                Display(GraphCore.GraphType.Squat_B_Loop, DisplaySquating);
        }
        /// <summary>
        /// 显示睡觉情况
        /// </summary>
        public void DisplaySleep(bool force = false)
        {
            looptimes = 0;
            CountNomal = 0;
            Core.Graph.RndGraph.Clear();
            if (force)
                Display(GraphCore.GraphType.Sleep_A_Start, DisplaySleepingForce);
            else
                Display(GraphCore.GraphType.Sleep_A_Start, DisplaySleeping);
        }
        /// <summary>
        /// 显示睡觉情况 (正常)
        /// </summary>
        public void DisplaySleeping()
        {
            if (Function.Rnd.Next(++looptimes) > LoopProMax)
                Display(GraphCore.GraphType.Sleep_C_End, DisplayNomal);
            else
                Display(GraphCore.GraphType.Sleep_B_Loop, DisplaySleeping);
        }
        /// <summary>
        /// 显示睡觉情况 (强制)
        /// </summary>
        public void DisplaySleepingForce()
        {//TODO:如果开启了Function,强制睡觉为永久,否则睡到自然醒+LoopMax
            Display(GraphCore.GraphType.Sleep_B_Loop, DisplaySleepingForce);
        }

        /// <summary>
        /// 显示拖拽情况
        /// </summary>
        public void DisplayRaised()
        {
            //位置迁移: 254-128           
            MainGrid.MouseMove += MainGrid_MouseMove;
            rasetype = 0;
            DisplayRaising();
            //if (((IGraph)PetGrid.Child).GraphType == GraphCore.GraphType.Touch_Head_B_Loop)
            //{
            //    ((IGraph)PetGrid.Child).IsContinue = true;
            //    return;
            //}
            //Display(GraphCore.GraphType.Raised_Dynamic, () =>
            //   Display(GraphCore.GraphType.Touch_Head_B_Loop, () =>
            //   Display(GraphCore.GraphType.Touch_Head_C_End, () =>
            //   Display(GraphCore.GraphType.Default
            //))));
        }
        int rasetype = int.MinValue;
        int walklength = 0;
        /// <summary>
        /// 显示拖拽中
        /// </summary>
        private void DisplayRaising()
        {
            switch (rasetype)
            {
                case int.MinValue:
                    break;
                case -1:
                    DisplayFalled_Left();
                    rasetype = int.MinValue;
                    return;
                case 0:
                case 1:
                case 2:
                    rasetype++;
                    Display(GraphCore.GraphType.Raised_Dynamic, DisplayRaising);
                    return;
                case 3:
                    rasetype++;
                    Display(GraphCore.GraphType.Raised_Static_A_Start, DisplayRaising);
                    return;
                default:
                    Display(GraphCore.GraphType.Raised_Static_B_Loop, DisplayRaising);
                    rasetype = 4;
                    break;
            }
        }
        /// <summary>
        /// 显示掉到地上 从左边
        /// </summary>
        public void DisplayFalled_Left()
        {
            Display(GraphCore.GraphType.Fall_Left_C_End,
              () => Display(GraphCore.GraphType.Climb_Up_Left, DisplayNomal));
        }
        /// <summary>
        /// 显示掉到地上 从左边
        /// </summary>
        public void DisplayFalled_Right()
        {
            Display(GraphCore.GraphType.Fall_Right_C_End,
              () => Display(GraphCore.GraphType.Climb_Up_Right, DisplayNomal));
        }
        /// <summary>
        /// 显示向左走 (有判断)
        /// </summary>
        public void DisplayWalk_Left()
        {
            //看看距离是否满足调节
            if (Core.Controller.GetWindowsDistanceLeft() > DistanceMax * Core.Controller.ZoomRatio)
            {
                walklength = 0;
                CountNomal = 0;
                Display(GraphCore.GraphType.Walk_Left_A_Start, () =>
                {
                    MoveTimerPoint = new Point(-Core.Graph.GraphConfig.SpeedWalk, 0);
                    MoveTimer.Start();
                    DisplayWalk_Lefting();
                });
            }
        }
        /// <summary>
        /// 显示向左走
        /// </summary>
        private void DisplayWalk_Lefting()
        {
            //看看距离是不是不足
            if (Core.Controller.GetWindowsDistanceLeft() < DistanceMin * Core.Controller.ZoomRatio)
            {//是,停下恢复默认 or/爬墙
                switch (Function.Rnd.Next(TreeRND))
                {
                    case 0:
                        DisplayFall_Left(() =>
                        {
                            MoveTimer.Stop();
                            Display(GraphCore.GraphType.Walk_Left_C_End, DisplayNomal);
                        });
                        return;
                    case 1:
                        DisplayFall_Right(() =>
                        {
                            MoveTimer.Stop();
                            Display(GraphCore.GraphType.Walk_Left_C_End, DisplayNomal);
                        });
                        return;
                    default:
                        MoveTimer.Stop();
                        Display(GraphCore.GraphType.Walk_Left_C_End, DisplayNomal);
                        return;
                }
            }
            //不是:继续右边走or停下
            if (Function.Rnd.Next(walklength++) < LoopMin)
            {
                Display(GraphCore.GraphType.Walk_Left_B_Loop, DisplayWalk_Lefting);
            }
            else
            {//停下来
                switch (Function.Rnd.Next(TreeRND))
                {

                    case 0:
                        DisplayFall_Left(() =>
                        {
                            MoveTimer.Stop();
                            Display(GraphCore.GraphType.Walk_Left_C_End, DisplayNomal);
                        });
                        break;
                    case 1:
                        DisplayFall_Right(() =>
                        {
                            MoveTimer.Stop();
                            Display(GraphCore.GraphType.Walk_Left_C_End, DisplayNomal);
                        });
                        break;
                    default:
                        MoveTimer.Stop();
                        Display(GraphCore.GraphType.Walk_Left_C_End, DisplayNomal);
                        break;

                }
            }
        }
        /// <summary>
        /// 显示向右走 (有判断)
        /// </summary>
        public void DisplayWalk_Right()
        {
            //看看距离是否满足调节
            if (Core.Controller.GetWindowsDistanceRight() > DistanceMax * Core.Controller.ZoomRatio)
            {
                walklength = 0;
                CountNomal = 0;
                Display(GraphCore.GraphType.Walk_Right_A_Start, () =>
                {
                    MoveTimerPoint = new Point(Core.Graph.GraphConfig.SpeedWalk, 0);
                    MoveTimer.Start();
                    DisplayWalk_Righting();
                });
            }
        }
        /// <summary>
        /// 显示向右走
        /// </summary>
        private void DisplayWalk_Righting()
        {
            //看看距离是不是不足
            if (Core.Controller.GetWindowsDistanceRight() < DistanceMin * Core.Controller.ZoomRatio)
            {//是,停下恢复默认 or/爬墙
                switch (Function.Rnd.Next(TreeRND))
                {
                    case 0:
                        DisplayClimb_Right_UP(() =>
                        {
                            MoveTimer.Stop();
                            Display(GraphCore.GraphType.Walk_Right_C_End, DisplayNomal);
                        });
                        return;
                    case 1:
                        DisplayClimb_Right_DOWN(() =>
                        {
                            MoveTimer.Stop();
                            Display(GraphCore.GraphType.Walk_Right_C_End, DisplayNomal);
                        });
                        return;
                    default:
                        MoveTimer.Stop();
                        Display(GraphCore.GraphType.Walk_Right_C_End, DisplayNomal);
                        return;
                }
            }
            //不是:继续右边走or停下
            if (Function.Rnd.Next(walklength++) < LoopMin)
            {
                Display(GraphCore.GraphType.Walk_Right_B_Loop, DisplayWalk_Righting);
            }
            else
            {//停下来
                switch (Function.Rnd.Next(TreeRND))
                {

                    case 0:
                        DisplayFall_Left(() =>
                        {
                            MoveTimer.Stop();
                            Display(GraphCore.GraphType.Walk_Left_C_End, DisplayNomal);
                        });
                        break;
                    case 1:
                        DisplayFall_Right(() =>
                        {
                            MoveTimer.Stop();
                            Display(GraphCore.GraphType.Walk_Left_C_End, DisplayNomal);
                        });
                        break;
                    default:
                        MoveTimer.Stop();
                        Display(GraphCore.GraphType.Walk_Left_C_End, DisplayNomal);
                        break;

                }
            }
        }
        /// <summary>
        /// 显示向左爬 (有判断)
        /// </summary>
        public void DisplayCrawl_Left()
        {
            //看看距离是否满足调节
            if (Core.Controller.GetWindowsDistanceLeft() > DistanceMax * Core.Controller.ZoomRatio)
            {
                walklength = 0;
                Display(GraphCore.GraphType.Crawl_Left_A_Start, () =>
                {
                    MoveTimerPoint = new Point(-Core.Graph.GraphConfig.SpeedCrawl, 0);
                    MoveTimer.Start();
                    DisplayCrawl_Lefting();
                });
            }
        }
        /// <summary>
        /// 显示向左爬
        /// </summary>
        private void DisplayCrawl_Lefting()
        {
            //看看距离是不是不足
            if (Core.Controller.GetWindowsDistanceLeft() < DistanceMin * Core.Controller.ZoomRatio)
            {//是,停下恢复默认 or/爬墙
                switch (Function.Rnd.Next(TreeRND))
                {
                    case 0:
                        DisplayClimb_Left_UP(() =>
                        {
                            MoveTimer.Stop();
                            Display(GraphCore.GraphType.Crawl_Left_C_End, DisplayNomal);
                        });
                        return;
                    case 1:
                        DisplayClimb_Left_DOWN(() =>
                        {
                            MoveTimer.Stop();
                            Display(GraphCore.GraphType.Crawl_Left_C_End, DisplayNomal);
                        });
                        return;
                    default:
                        MoveTimer.Stop();
                        Display(GraphCore.GraphType.Crawl_Left_C_End, DisplayNomal);
                        return;
                }
            }
            //不是:继续右边走or停下
            if (Function.Rnd.Next(walklength++) < LoopMin)
            {
                Display(GraphCore.GraphType.Crawl_Left_B_Loop, DisplayCrawl_Lefting);
            }
            else
            {//停下来
                MoveTimer.Stop();
                Display(GraphCore.GraphType.Crawl_Left_C_End, DisplayNomal);
            }
        }
        /// <summary>
        /// 显示向右爬 (有判断)
        /// </summary>
        public void DisplayCrawl_Right()
        {
            //看看距离是否满足调节
            if (Core.Controller.GetWindowsDistanceRight() > DistanceMax * Core.Controller.ZoomRatio)
            {
                walklength = 0;
                Display(GraphCore.GraphType.Crawl_Right_A_Start, () =>
                {
                    MoveTimerPoint = new Point(Core.Graph.GraphConfig.SpeedCrawl, 0);
                    MoveTimer.Start();
                    DisplayCrawl_Righting();
                });
            }
        }
        /// <summary>
        /// 显示向右爬
        /// </summary>
        private void DisplayCrawl_Righting()
        {
            //看看距离是不是不足
            if (Core.Controller.GetWindowsDistanceRight() < DistanceMin * Core.Controller.ZoomRatio)
            {//是,停下恢复默认 or/爬墙
                switch (Function.Rnd.Next(TreeRND))
                {
                    case 0:
                        DisplayClimb_Right_UP(() =>
                        {
                            MoveTimer.Stop();
                            Display(GraphCore.GraphType.Crawl_Right_C_End, DisplayNomal);
                        });
                        return;
                    case 1:
                        DisplayClimb_Right_DOWN(() =>
                        {
                            MoveTimer.Stop();
                            Display(GraphCore.GraphType.Crawl_Right_C_End, DisplayNomal);
                        });
                        return;
                    default:
                        MoveTimer.Stop();
                        Display(GraphCore.GraphType.Crawl_Right_C_End, DisplayNomal);
                        return;
                }
            }
            //不是:继续右边走or停下
            if (Function.Rnd.Next(walklength++) < LoopMin)
            {
                Display(GraphCore.GraphType.Crawl_Right_B_Loop, DisplayCrawl_Righting);
            }
            else
            {//停下来
                MoveTimer.Stop();
                Display(GraphCore.GraphType.Crawl_Right_C_End, DisplayNomal);
            }
        }
        /// <summary>
        /// 显示左墙壁爬行 上
        /// </summary>
        public void DisplayClimb_Left_UP(Action ifNot = null)
        {
            //看看距离是否满足调节
            if (Core.Controller.GetWindowsDistanceLeft() < DistanceMid * Core.Controller.ZoomRatio && Core.Controller.GetWindowsDistanceUp() > DistanceMax * Core.Controller.ZoomRatio)
            {
                walklength = 0;
                CountNomal = 0;
                Core.Controller.MoveWindows(-Core.Controller.GetWindowsDistanceLeft() / Core.Controller.ZoomRatio - Core.Graph.GraphConfig.LocateClimbLeft, 0);
                Display(GraphCore.GraphType.Walk_Left_A_Start, () =>
                {
                    MoveTimerPoint = new Point(0, -Core.Graph.GraphConfig.SpeedClimb);
                    MoveTimer.Start();
                    DisplayClimb_Lefting_UP();
                });
            }
            else
                ifNot?.Invoke();
        }
        /// <summary>
        /// 显示左墙壁爬行 上
        /// </summary>
        private void DisplayClimb_Lefting_UP()
        {
            //看看距离是不是不足
            if (Core.Controller.GetWindowsDistanceUp() < DistanceMid * Core.Controller.ZoomRatio)
            {//是,停下恢复默认 or/爬上面的墙
                switch (Function.Rnd.Next(TreeRND))
                {
                    case 0:
                        DisplayClimb_Top_Right();
                        return;
                    case 1:
                        DisplayFall_Right();
                        return;
                    default:
                        MoveTimer.Stop();
                        DisplayNomal();
                        return;
                }
            }
            //不是:继续or停下
            if (Function.Rnd.Next(walklength++) < LoopMid)
            {
                Display(GraphCore.GraphType.Climb_Left, DisplayClimb_Lefting_UP);
            }
            else
            {//停下来
                switch (Function.Rnd.Next(TreeRND))
                {
                    case 1:
                        DisplayFall_Right();
                        break;
                    default:
                        MoveTimer.Stop();
                        DisplayNomal();
                        break;
                }
            }
        }
        /// <summary>
        /// 显示左墙壁爬行 下
        /// </summary>
        public void DisplayClimb_Left_DOWN(Action ifNot = null)
        {
            //看看距离是否满足调节
            if (Core.Controller.GetWindowsDistanceLeft() < DistanceMin * Core.Controller.ZoomRatio && Core.Controller.GetWindowsDistanceDown() > DistanceMax * Core.Controller.ZoomRatio)
            {
                walklength = 0;
                CountNomal = 0;

                Core.Controller.MoveWindows(-Core.Controller.GetWindowsDistanceLeft() / Core.Controller.ZoomRatio - Core.Graph.GraphConfig.LocateClimbLeft, 0);
                Display(GraphCore.GraphType.Walk_Left_A_Start, () =>
                {
                    MoveTimerPoint = new System.Windows.Point(0, Core.Graph.GraphConfig.SpeedClimb);
                    MoveTimer.Start();
                    DisplayClimb_Lefting_DOWN();
                });
            }
            else
                ifNot?.Invoke();
        }
        /// <summary>
        /// 显示左墙壁爬行 下
        /// </summary>
        private void DisplayClimb_Lefting_DOWN()
        {
            //看看距离是不是不足
            if (Core.Controller.GetWindowsDistanceDown() < DistanceMin * Core.Controller.ZoomRatio)
            {//是,停下恢复默认
                MoveTimer.Stop();
                DisplayNomal();
            }
            //不是:继续or停下
            if (Function.Rnd.Next(walklength++) < LoopMin)
            {
                Display(GraphCore.GraphType.Climb_Left, DisplayClimb_Lefting_DOWN);
            }
            else
            {//停下来
                MoveTimer.Stop();
                DisplayNomal();
            }
        }
        /// <summary>
        /// 显示右墙壁爬行 上
        /// </summary>
        public void DisplayClimb_Right_UP(Action ifNot = null)
        {
            //看看距离是否满足调节
            if (Core.Controller.GetWindowsDistanceRight() < DistanceMid * Core.Controller.ZoomRatio && Core.Controller.GetWindowsDistanceUp() > DistanceMax * Core.Controller.ZoomRatio)
            {
                walklength = 0;
                CountNomal = 0;

                Core.Controller.MoveWindows(Core.Controller.GetWindowsDistanceRight() / Core.Controller.ZoomRatio + Core.Graph.GraphConfig.LocateClimbRight, 0);
                Display(GraphCore.GraphType.Walk_Right_A_Start, () =>
                {
                    MoveTimerPoint = new Point(0, -Core.Graph.GraphConfig.SpeedClimb);
                    MoveTimer.Start();
                    DisplayClimb_Righting_UP();
                });
            }
            else
                ifNot?.Invoke();
        }
        /// <summary>
        /// 显示右墙壁爬行 上
        /// </summary>
        private void DisplayClimb_Righting_UP()
        {
            //看看距离是不是不足
            if (Core.Controller.GetWindowsDistanceUp() < DistanceMid * Core.Controller.ZoomRatio)
            {//是,停下恢复默认 or/爬上面的墙
                switch (Function.Rnd.Next(TreeRND))
                {
                    case 0:
                        DisplayClimb_Top_Left();
                        return;
                    case 1:
                        DisplayFall_Left();
                        return;
                    default:
                        MoveTimer.Stop();
                        DisplayNomal();
                        return;
                }
            }
            //不是:继续or停下
            if (Function.Rnd.Next(walklength++) < LoopMin)
            {
                Display(GraphCore.GraphType.Climb_Right, DisplayClimb_Righting_UP);
            }
            else
            {//停下来
                switch (Function.Rnd.Next(TreeRND))
                {
                    case 0:
                        DisplayFall_Left();
                        break;
                    default:
                        MoveTimer.Stop();
                        DisplayNomal();
                        break;
                }
            }
        }
        /// <summary>
        /// 显示右墙壁爬行 下
        /// </summary>
        public void DisplayClimb_Right_DOWN(Action ifNot = null)
        {
            //看看距离是否满足调节
            if (Core.Controller.GetWindowsDistanceRight() < DistanceMid * Core.Controller.ZoomRatio && Core.Controller.GetWindowsDistanceDown() > DistanceMax * Core.Controller.ZoomRatio)
            {
                walklength = 0;
                CountNomal = 0;

                Core.Controller.MoveWindows(Core.Controller.GetWindowsDistanceRight() / Core.Controller.ZoomRatio + Core.Graph.GraphConfig.LocateClimbRight, 0);
                Display(GraphCore.GraphType.Walk_Right_A_Start, () =>
                {
                    MoveTimerPoint = new Point(0, Core.Graph.GraphConfig.SpeedClimb);
                    MoveTimer.Start();
                    DisplayClimb_Righting_DOWN();
                });
            }
            else
                ifNot?.Invoke();
        }
        /// <summary>
        /// 显示右墙壁爬行 下
        /// </summary>
        private void DisplayClimb_Righting_DOWN()
        {
            //看看距离是不是不足
            if (Core.Controller.GetWindowsDistanceDown() < DistanceMin * Core.Controller.ZoomRatio)
            {//是,停下恢复默认
                MoveTimer.Stop();
                DisplayNomal();
            }
            //不是:继续or停下
            if (Function.Rnd.Next(walklength++) < LoopMin)
            {
                Display(GraphCore.GraphType.Climb_Right, DisplayClimb_Righting_DOWN);
            }
            else
            {//停下来
                MoveTimer.Stop();
                DisplayNomal();
            }
        }
        /// <summary>
        /// 显示顶部墙壁爬行向右
        /// </summary>
        public void DisplayClimb_Top_Right()
        {
            //看看距离是否满足调节
            if (Core.Controller.GetWindowsDistanceUp() < DistanceMid * Core.Controller.ZoomRatio && Core.Controller.GetWindowsDistanceRight() > DistanceMax * Core.Controller.ZoomRatio)
            {
                walklength = 0;
                CountNomal = 0;

                Core.Controller.MoveWindows(0, -Core.Controller.GetWindowsDistanceUp() / Core.Controller.ZoomRatio - Core.Graph.GraphConfig.LocateClimbTop);
                MoveTimerPoint = new Point(Core.Graph.GraphConfig.SpeedClimbTop, 0);
                MoveTimer.Start();
                DisplayClimb_Top_Righting();
            }
        }
        /// <summary>
        /// 显示顶部墙壁爬行向左
        /// </summary>
        private void DisplayClimb_Top_Righting()
        {
            //看看距离是不是不足
            if (Core.Controller.GetWindowsDistanceRight() < DistanceMin * Core.Controller.ZoomRatio)
            {//是,停下恢复默认 or向下爬or掉落
                switch (Function.Rnd.Next(TreeRND))
                {
                    case 0:
                        DisplayClimb_Right_DOWN();
                        return;
                    case 1:
                        DisplayFall_Right();
                        return;
                    default:
                        Core.Controller.MoveWindows(0, -Core.Controller.GetWindowsDistanceUp() / Core.Controller.ZoomRatio);
                        MoveTimer.Stop();
                        DisplayNomal();
                        return;
                }
            }
            //不是:继续or停下
            if (Function.Rnd.Next(walklength++) < LoopMax)
            {
                Display(GraphType.Climb_Top_Right, DisplayClimb_Top_Righting);
            }
            else
            {//停下来
                Core.Controller.MoveWindows(0, -Core.Controller.GetWindowsDistanceUp() / Core.Controller.ZoomRatio);
                MoveTimer.Stop();
                DisplayNomal();
            }
        }
        /// <summary>
        /// 显示顶部墙壁爬行向左
        /// </summary>
        public void DisplayClimb_Top_Left()
        {
            //看看距离是否满足调节
            if (Core.Controller.GetWindowsDistanceUp() < DistanceMid * Core.Controller.ZoomRatio && Core.Controller.GetWindowsDistanceLeft() > DistanceMax * Core.Controller.ZoomRatio)
            {
                walklength = 0;
                CountNomal = 0;

                Core.Controller.MoveWindows(0, -Core.Controller.GetWindowsDistanceUp() / Core.Controller.ZoomRatio - Core.Graph.GraphConfig.LocateClimbTop);
                MoveTimerPoint = new Point(-Core.Graph.GraphConfig.SpeedClimbTop, 0);
                MoveTimer.Start();
                DisplayClimb_Top_Lefting();
            }
        }
        /// <summary>
        /// 显示顶部墙壁爬行向左
        /// </summary>
        private void DisplayClimb_Top_Lefting()
        {
            //看看距离是不是不足
            if (Core.Controller.GetWindowsDistanceLeft() < DistanceMin * Core.Controller.ZoomRatio)
            {//是,停下恢复默认 or向下爬
                switch (Function.Rnd.Next(TreeRND))
                {
                    case 0:
                        DisplayClimb_Left_DOWN();
                        return;
                    case 1:
                        DisplayFall_Left();
                        return;
                    default:
                        Core.Controller.MoveWindows(0, -Core.Controller.GetWindowsDistanceUp() / Core.Controller.ZoomRatio);
                        MoveTimer.Stop();
                        DisplayNomal();
                        return;
                }
            }
            //不是:继续or停下
            if (Function.Rnd.Next(walklength++) < LoopMax)
            {
                Display(GraphCore.GraphType.Climb_Top_Left, DisplayClimb_Top_Lefting);
            }
            else
            {//停下来
                Core.Controller.MoveWindows(0, -Core.Controller.GetWindowsDistanceUp() / Core.Controller.ZoomRatio);
                MoveTimer.Stop();
                DisplayNomal();
            }
        }
        /// <summary>
        /// 显示掉落向左
        /// </summary>
        public void DisplayFall_Left(Action ifNot = null)
        {
            //看看距离是否满足调节
            if (Core.Controller.GetWindowsDistanceDown() > DistanceMax * Core.Controller.ZoomRatio && Core.Controller.GetWindowsDistanceLeft() > DistanceMax * Core.Controller.ZoomRatio)
            {
                walklength = 0;
                CountNomal = 0;
                //Core.Controller.MoveWindows(0, -Core.Controller.GetWindowsDistanceUp() / Core.Controller.ZoomRatio - 1DistanceMin);
                MoveTimerPoint = new Point(-Core.Graph.GraphConfig.SpeedFallX, Core.Graph.GraphConfig.SpeedFallY);
                MoveTimer.Start();
                Display(GraphType.Fall_Left_A_Start, DisplayFall_Lefting);
            }
            else
                ifNot?.Invoke();
        }
        /// <summary>
        /// 显示掉落向左
        /// </summary>
        private void DisplayFall_Lefting()
        {
            //看看距离是不是不足
            if (Core.Controller.GetWindowsDistanceLeft() < DistanceMin * Core.Controller.ZoomRatio || Core.Controller.GetWindowsDistanceDown() < DistanceMin * Core.Controller.ZoomRatio)
            {//是,停下恢复默认 or向上爬
                switch (Function.Rnd.Next(TreeRND))
                {
                    case 0:
                        DisplayClimb_Left_UP(() =>
                        {
                            MoveTimer.Stop();
                            DisplayFalled_Left();
                        });
                        return;
                    case 1:
                        DisplayClimb_Left_DOWN(() =>
                        {
                            MoveTimer.Stop();
                            DisplayFalled_Left();
                        });
                        return;
                    default:
                        //Core.Controller.MoveWindows(0, -Core.Controller.GetWindowsDistanceUp() / Core.Controller.ZoomRatio);
                        MoveTimer.Stop();
                        DisplayFalled_Left();
                        return;
                }
            }
            //不是:继续or停下
            if (Function.Rnd.Next(walklength++) < LoopMid)
            {
                Display(GraphCore.GraphType.Fall_Left_B_Loop, DisplayFall_Lefting);
            }
            else
            {//停下来
             //Core.Controller.MoveWindows(0, -Core.Controller.GetWindowsDistanceUp() / Core.Controller.ZoomRatio);
                MoveTimer.Stop();
                DisplayFalled_Left();
                //DisplayNomal();
            }
        }

        /// <summary>
        /// 显示掉落向右
        /// </summary>
        public void DisplayFall_Right(Action ifNot = null)
        {
            //看看距离是否满足调节
            if (Core.Controller.GetWindowsDistanceDown() > DistanceMax * Core.Controller.ZoomRatio && Core.Controller.GetWindowsDistanceRight() > DistanceMax * Core.Controller.ZoomRatio)
            {
                walklength = 0;
                CountNomal = 0;
                //Core.Controller.MoveWindows(0, -Core.Controller.GetWindowsDistanceUp() / Core.Controller.ZoomRatio - 1DistanceMin);
                MoveTimerPoint = new Point(Core.Graph.GraphConfig.SpeedFallX, Core.Graph.GraphConfig.SpeedFallY);
                MoveTimer.Start();
                Display(GraphType.Fall_Right_A_Start, DisplayFall_Righting);
            }
            else
                ifNot?.Invoke();
        }
        /// <summary>
        /// 显示掉落向右
        /// </summary>
        private void DisplayFall_Righting()
        {
            //看看距离是不是不足
            if (Core.Controller.GetWindowsDistanceRight() < DistanceMin * Core.Controller.ZoomRatio || Core.Controller.GetWindowsDistanceDown() < DistanceMin * Core.Controller.ZoomRatio)
            {//是,停下恢复默认 or向上爬
                switch (Function.Rnd.Next(TreeRND))
                {
                    case 0:
                        DisplayClimb_Right_UP(() =>
                        {
                            MoveTimer.Stop();
                            DisplayFalled_Right();
                        });
                        return;
                    case 1:
                        DisplayClimb_Right_DOWN(() =>
                        {
                            MoveTimer.Stop();
                            DisplayFalled_Right();
                        });
                        return;
                    default:
                        //Core.Controller.MoveWindows(0, -Core.Controller.GetWindowsDistanceUp() / Core.Controller.ZoomRatio);
                        MoveTimer.Stop();
                        DisplayFalled_Right();
                        return;
                }
            }
            //不是:继续or停下
            if (Function.Rnd.Next(walklength++) < LoopMid)
            {
                Display(GraphCore.GraphType.Fall_Right_B_Loop, DisplayFall_Righting);
            }
            else
            {//停下来
             //Core.Controller.MoveWindows(0, -Core.Controller.GetWindowsDistanceUp() / Core.Controller.ZoomRatio);
                MoveTimer.Stop();
                DisplayFalled_Right();
                //DisplayNomal();
            }
        }



        /// <summary>
        /// 显示动画 
        /// </summary>
        /// <param name="Type">动画类型</param>
        /// <param name="EndAction">动画结束后操作</param>
        public void Display(GraphType Type, Action EndAction = null)//, bool StoreRnd = false)
        {
            Display(Core.Graph.FindGraph(Type, Core.Save.Mode), EndAction);
        }
        bool petgridcrlf = true;
        /// <summary>
        /// 显示动画 (自动多层切换)
        /// </summary>
        /// <param name="graph">动画</param>
        /// <param name="EndAction">结束操作</param>
        public void Display(IGraph graph, Action EndAction = null)
        {
            //if(graph.GraphType == GraphType.Climb_Up_Left)
            //{
            //    Dispatcher.Invoke(() => Say(graph.GraphType.ToString()));
            //}
            DisplayType = graph.GraphType;
            if (PetGrid.Child == graph.This)
            {
                ((IGraph)(PetGrid2.Child)).Stop(true);
                Dispatcher.Invoke(() =>
                {
                    PetGrid.Visibility = Visibility.Visible;
                    PetGrid2.Visibility = Visibility.Hidden;
                });
                graph.Run(EndAction);
                return;
            }
            else if (PetGrid2.Child == graph.This)
            {
                ((IGraph)(PetGrid.Child)).Stop(true);
                Dispatcher.Invoke(() =>
                {
                    PetGrid2.Visibility = Visibility.Visible;
                    PetGrid.Visibility = Visibility.Hidden;
                });
                graph.Run(EndAction);
                return;
            }
            graph.Run(EndAction);


            if (petgridcrlf)
            {
                ((IGraph)(PetGrid.Child)).Stop(true);
                Dispatcher.Invoke(() =>
                {
                    PetGrid2.Child = graph.This;
                    PetGrid.Visibility = Visibility.Hidden;
                    PetGrid2.Visibility = Visibility.Visible;
                });
                //Task.Run(() =>
                //{
                //    Thread.Sleep(25);
                //    Dispatcher.Invoke(() => PetGrid.Child = null);
                //});
            }
            else
            {
                ((IGraph)(PetGrid2.Child)).Stop(true);
                Dispatcher.Invoke(() =>
                {
                    PetGrid.Child = graph.This;
                    PetGrid2.Visibility = Visibility.Hidden;
                    PetGrid.Visibility = Visibility.Visible;
                });
                //Task.Run(() =>
                //{
                //    Thread.Sleep(25);
                //    Dispatcher.Invoke(() => PetGrid2.Child = null);
                //});
            }
            petgridcrlf = !petgridcrlf;

        }
    }
}
