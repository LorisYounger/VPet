using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace VPet_Simulator.Core
{
    public partial class Main
    {
        /// <summary>
        /// 显示默认情况
        /// </summary>
        public void DisplayNomal()
        {
            IsNomal = true;
            Display(Core.Graph.FindGraph(GraphCore.GraphType.Default, Core.Save.Mode));
        }
        /// <summary>
        /// 显示摸头情况
        /// </summary>
        public void DisplayTouchHead()
        {
            IsNomal = false;
            if (petgridcrlf)
                if (PetGrid.Child is IGraph ig && ig.GraphType == GraphCore.GraphType.Touch_Head_B_Loop)
                {
                    ((IGraph)PetGrid.Child).IsContinue = true;
                    return;
                }
                else if (PetGrid2.Child is IGraph ig2 && ig2.GraphType == GraphCore.GraphType.Touch_Head_B_Loop)
                {
                    ((IGraph)PetGrid.Child).IsContinue = true;
                    return;
                }
            Display(Core.Graph.FindGraph(GraphCore.GraphType.Touch_Head_A_Start, Core.Save.Mode), () =>
               Display(Core.Graph.FindGraph(GraphCore.GraphType.Touch_Head_B_Loop, Core.Save.Mode), () =>
               Display(Core.Graph.FindGraph(GraphCore.GraphType.Touch_Head_C_End, Core.Save.Mode), DisplayNomal
            )));
        }
        /// <summary>
        /// 显示拖拽情况
        /// </summary>
        public void DisplayRaised()
        {
            IsNomal = false;
            //位置迁移: 254-128           
            MainGrid.MouseMove += MainGrid_MouseMove;
            rasetype = 0;
            DisplayRaising();
            //if (((IGraph)PetGrid.Child).GraphType == GraphCore.GraphType.Touch_Head_B_Loop)
            //{
            //    ((IGraph)PetGrid.Child).IsContinue = true;
            //    return;
            //}
            //Display(Core.Graph.FindGraph(GraphCore.GraphType.Raised_Dynamic, Core.Save.Mode), () =>
            //   Display(Core.Graph.FindGraph(GraphCore.GraphType.Touch_Head_B_Loop, Core.Save.Mode), () =>
            //   Display(Core.Graph.FindGraph(GraphCore.GraphType.Touch_Head_C_End, Core.Save.Mode), () =>
            //   Display(Core.Graph.FindGraph(GraphCore.GraphType.Default, Core.Save.Mode)
            //))));
        }
        int rasetype = -1;
        int walklength = 0;
        /// <summary>
        /// 显示拖拽中
        /// </summary>
        private void DisplayRaising()
        {
            switch (rasetype++)
            {
                case -1:
                    DisplayFalled();
                    rasetype = -2;
                    return;
                case 0:
                case 1:
                case 2:
                    Display(Core.Graph.FindGraph(GraphCore.GraphType.Raised_Dynamic, Core.Save.Mode), DisplayRaising);
                    return;
                case 3:
                    Display(Core.Graph.FindGraph(GraphCore.GraphType.Raised_Static_A_Start, Core.Save.Mode), DisplayRaising);
                    return;
                default:
                    Display(Core.Graph.FindGraph(GraphCore.GraphType.Raised_Static_B_Loop, Core.Save.Mode), DisplayRaising);
                    rasetype = 4;
                    break;
            }
        }
        /// <summary>
        /// 显示掉到地上
        /// </summary>
        public void DisplayFalled()
        {//TODO:爬起
            Display(Core.Graph.FindGraph(GraphCore.GraphType.Fall_B_End, Core.Save.Mode), DisplayNomal);
        }
        /// <summary>
        /// 显示向左走 (有判断)
        /// </summary>
        public void DisplayWalk_Left()
        {
            //看看距离是否满足调节
            if (Core.Controller.GetWindowsDistanceLeft() > 400 * Core.Controller.ZoomRatio)
            {
                walklength = 0;
                IsNomal = false;
                Display(Core.Graph.FindGraph(GraphCore.GraphType.Walk_Left_A_Start, Core.Save.Mode), () =>
                {
                    MoveTimerPoint = new Point(-20, 0);//TODO:锚定设置
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
            if (Core.Controller.GetWindowsDistanceLeft() < 50 * Core.Controller.ZoomRatio)
            {//是,停下恢复默认 or/爬墙
                switch (Function.Rnd.Next(3))
                {
                    case 0:
                        DisplayClimb_Left_UP(() =>
                        {
                            MoveTimer.Stop();
                            Display(Core.Graph.FindGraph(GraphCore.GraphType.Walk_Left_C_End, Core.Save.Mode), DisplayNomal);
                        });
                        return;
                    case 1:
                        DisplayClimb_Left_DOWN(() =>
                        {
                            MoveTimer.Stop();
                            Display(Core.Graph.FindGraph(GraphCore.GraphType.Walk_Left_C_End, Core.Save.Mode), DisplayNomal);
                        });
                        return;
                    default:
                        MoveTimer.Stop();
                        Display(Core.Graph.FindGraph(GraphCore.GraphType.Walk_Left_C_End, Core.Save.Mode), DisplayNomal);
                        return;
                }
            }
            //不是:继续右边走or停下
            if (Function.Rnd.Next(walklength++) < 5)
            {
                Display(Core.Graph.FindGraph(GraphCore.GraphType.Walk_Left_B_Loop, Core.Save.Mode), DisplayWalk_Lefting);
            }
            else
            {//停下来
                MoveTimer.Stop();
                Display(Core.Graph.FindGraph(GraphCore.GraphType.Walk_Left_C_End, Core.Save.Mode), DisplayNomal);
            }
        }
        /// <summary>
        /// 显示向右走 (有判断)
        /// </summary>
        public void DisplayWalk_Right()
        {
            //看看距离是否满足调节
            if (Core.Controller.GetWindowsDistanceRight() > 400 * Core.Controller.ZoomRatio)
            {
                walklength = 0;
                IsNomal = false;
                Display(Core.Graph.FindGraph(GraphCore.GraphType.Walk_Right_A_Start, Core.Save.Mode), () =>
                {
                    MoveTimerPoint = new Point(-20, 0);//TODO:锚定设置
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
            if (Core.Controller.GetWindowsDistanceRight() < 50 * Core.Controller.ZoomRatio)
            {//是,停下恢复默认 or/爬墙
                switch (Function.Rnd.Next(3))
                {
                    case 0:
                        DisplayClimb_Right_UP(() =>
                        {
                            MoveTimer.Stop();
                            Display(Core.Graph.FindGraph(GraphCore.GraphType.Walk_Right_C_End, Core.Save.Mode), DisplayNomal);
                        });
                        return;
                    case 1:
                        DisplayClimb_Right_DOWN(() =>
                        {
                            MoveTimer.Stop();
                            Display(Core.Graph.FindGraph(GraphCore.GraphType.Walk_Right_C_End, Core.Save.Mode), DisplayNomal);
                        });
                        return;
                    default:
                        MoveTimer.Stop();
                        Display(Core.Graph.FindGraph(GraphCore.GraphType.Walk_Right_C_End, Core.Save.Mode), DisplayNomal);
                        return;
                }
            }
            //不是:继续右边走or停下
            if (Function.Rnd.Next(walklength++) < 5)
            {
                Display(Core.Graph.FindGraph(GraphCore.GraphType.Walk_Right_B_Loop, Core.Save.Mode), DisplayWalk_Righting);
            }
            else
            {//停下来
                MoveTimer.Stop();
                Display(Core.Graph.FindGraph(GraphCore.GraphType.Walk_Right_C_End, Core.Save.Mode), DisplayNomal);
            }
        }
        /// <summary>
        /// 显示左墙壁爬行 上
        /// </summary>
        public void DisplayClimb_Left_UP(Action ifNot = null)
        {
            //看看距离是否满足调节
            if (Core.Controller.GetWindowsDistanceLeft() < 100 * Core.Controller.ZoomRatio && Core.Controller.GetWindowsDistanceUp() > 400 * Core.Controller.ZoomRatio)
            {
                walklength = 0;
                IsNomal = false;

                Core.Controller.MoveWindows(-Core.Controller.GetWindowsDistanceLeft() / Core.Controller.ZoomRatio - 145, 0);//TODO:锚定设置
                Display(Core.Graph.FindGraph(GraphCore.GraphType.Walk_Left_A_Start, Core.Save.Mode), () =>
                {
                    MoveTimerPoint = new Point(0, -10);//TODO:锚定设置
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
            if (Core.Controller.GetWindowsDistanceUp() < 100 * Core.Controller.ZoomRatio)
            {//是,停下恢复默认 or/爬上面的墙
                switch (0)//Function.Rnd.Next(3))
                {
                    case 0:
                        DisplayClimb_Top_Right();
                        return;
                    //case 1://TODO:落下
                    //    return;
                    default:
                        MoveTimer.Stop();
                        DisplayNomal();
                        return;
                }
            }
            //不是:继续or停下
            if (Function.Rnd.Next(walklength++) < 5)
            {
                Display(Core.Graph.FindGraph(GraphCore.GraphType.Climb_Left, Core.Save.Mode), DisplayClimb_Lefting_UP);
            }
            else
            {//停下来
                MoveTimer.Stop();
                DisplayNomal();
            }
        }
        /// <summary>
        /// 显示左墙壁爬行 下
        /// </summary>
        public void DisplayClimb_Left_DOWN(Action ifNot = null)
        {
            //看看距离是否满足调节
            if (Core.Controller.GetWindowsDistanceLeft() < 50 * Core.Controller.ZoomRatio && Core.Controller.GetWindowsDistanceDown() > 400 * Core.Controller.ZoomRatio)
            {
                walklength = 0;
                IsNomal = false;

                Core.Controller.MoveWindows(-Core.Controller.GetWindowsDistanceLeft() / Core.Controller.ZoomRatio - 145, 0);//TODO:锚定设置
                Display(Core.Graph.FindGraph(GraphCore.GraphType.Walk_Left_A_Start, Core.Save.Mode), () =>
                {
                    MoveTimerPoint = new System.Windows.Point(0, 10);//TODO:锚定设置
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
            if (Core.Controller.GetWindowsDistanceDown() < 50 * Core.Controller.ZoomRatio)
            {//是,停下恢复默认
                MoveTimer.Stop();
                DisplayNomal();
            }
            //不是:继续or停下
            if (Function.Rnd.Next(walklength++) < 5)
            {
                Display(Core.Graph.FindGraph(GraphCore.GraphType.Climb_Left, Core.Save.Mode), DisplayClimb_Lefting_DOWN);
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
            if (Core.Controller.GetWindowsDistanceRight() < 100 * Core.Controller.ZoomRatio && Core.Controller.GetWindowsDistanceUp() > 400 * Core.Controller.ZoomRatio)
            {
                walklength = 0;
                IsNomal = false;

                Core.Controller.MoveWindows(Core.Controller.GetWindowsDistanceRight() / Core.Controller.ZoomRatio + 185, 0);//TODO:锚定设置
                Display(Core.Graph.FindGraph(GraphCore.GraphType.Walk_Right_A_Start, Core.Save.Mode), () =>
                {
                    MoveTimerPoint = new Point(0, -10);//TODO:锚定设置
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
            if (Core.Controller.GetWindowsDistanceUp() < 100 * Core.Controller.ZoomRatio)
            {//是,停下恢复默认 or/爬上面的墙
                switch (3)//Function.Rnd.Next(3))
                {
                    case 0:
                        DisplayClimb_Top_Left();
                        return;
                    //case 1://TODO:落下
                    //    return;
                    default:
                        MoveTimer.Stop();
                        DisplayNomal();
                        return;
                }
            }
            //不是:继续or停下
            if (Function.Rnd.Next(walklength++) < 5)
            {
                Display(Core.Graph.FindGraph(GraphCore.GraphType.Climb_Right, Core.Save.Mode), DisplayClimb_Righting_UP);
            }
            else
            {//停下来
                MoveTimer.Stop();
                DisplayNomal();
            }
        }
        /// <summary>
        /// 显示右墙壁爬行 下
        /// </summary>
        public void DisplayClimb_Right_DOWN(Action ifNot = null)
        {
            //看看距离是否满足调节
            if (Core.Controller.GetWindowsDistanceRight() < 100 * Core.Controller.ZoomRatio && Core.Controller.GetWindowsDistanceDown() > 400 * Core.Controller.ZoomRatio)
            {
                walklength = 0;
                IsNomal = false;

                Core.Controller.MoveWindows(Core.Controller.GetWindowsDistanceRight() / Core.Controller.ZoomRatio + 185, 0);//TODO:锚定设置
                Display(Core.Graph.FindGraph(GraphCore.GraphType.Walk_Right_A_Start, Core.Save.Mode), () =>
                {
                    MoveTimerPoint = new Point(0, 10);//TODO:锚定设置
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
            if (Core.Controller.GetWindowsDistanceDown() < 50 * Core.Controller.ZoomRatio)
            {//是,停下恢复默认
                MoveTimer.Stop();
                DisplayNomal();
            }
            //不是:继续or停下
            if (Function.Rnd.Next(walklength++) < 5)
            {
                Display(Core.Graph.FindGraph(GraphCore.GraphType.Climb_Right, Core.Save.Mode), DisplayClimb_Righting_DOWN);
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
            if (Core.Controller.GetWindowsDistanceUp() < 50 * Core.Controller.ZoomRatio && Core.Controller.GetWindowsDistanceRight() > 400 * Core.Controller.ZoomRatio)
            {
                walklength = 0;
                IsNomal = false;

                Core.Controller.MoveWindows(0, -Core.Controller.GetWindowsDistanceUp() / Core.Controller.ZoomRatio - 150);//TODO:锚定设置
                MoveTimerPoint = new Point(8, 0);//TODO:锚定设置
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
            if (Core.Controller.GetWindowsDistanceRight() < 50 * Core.Controller.ZoomRatio)
            {//是,停下恢复默认 or向下爬or掉落
                switch (3)//Function.Rnd.Next(3))
                {
                    case 0:
                        DisplayClimb_Right_DOWN();
                        return;
                    //case 1://TODO:落下
                    //    return;
                    default:
                        Core.Controller.MoveWindows(0, -Core.Controller.GetWindowsDistanceUp() / Core.Controller.ZoomRatio);
                        MoveTimer.Stop();
                        DisplayNomal();
                        return;
                }
            }
            //不是:继续or停下
            if (Function.Rnd.Next(walklength++) < 10)
            {
                Display(Core.Graph.FindGraph(GraphCore.GraphType.Climb_Top_Right, Core.Save.Mode), DisplayClimb_Top_Righting);
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
            if (Core.Controller.GetWindowsDistanceUp() < 50 * Core.Controller.ZoomRatio && Core.Controller.GetWindowsDistanceLeft() > 400 * Core.Controller.ZoomRatio)
            {
                walklength = 0;
                IsNomal = false;

                Core.Controller.MoveWindows(0, -Core.Controller.GetWindowsDistanceUp() / Core.Controller.ZoomRatio - 150);//TODO:锚定设置
                MoveTimerPoint = new Point(8, 0);//TODO:锚定设置
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
            if (Core.Controller.GetWindowsDistanceLeft() < 50 * Core.Controller.ZoomRatio)
            {//是,停下恢复默认 or向下爬
                switch (3)//Function.Rnd.Next(3))
                {
                    case 0:
                        DisplayClimb_Left_DOWN();
                        return;
                    //case 1://TODO:落下
                    //    return;
                    default:
                        Core.Controller.MoveWindows(0, -Core.Controller.GetWindowsDistanceUp() / Core.Controller.ZoomRatio);
                        MoveTimer.Stop();
                        DisplayNomal();
                        return;
                }
            }
            //不是:继续or停下
            if (Function.Rnd.Next(walklength++) < 10)
            {
                Display(Core.Graph.FindGraph(GraphCore.GraphType.Climb_Top_Left, Core.Save.Mode), DisplayClimb_Top_Lefting);
            }
            else
            {//停下来
                Core.Controller.MoveWindows(0, -Core.Controller.GetWindowsDistanceUp() / Core.Controller.ZoomRatio);
                MoveTimer.Stop();
                DisplayNomal();
            }
        }








        bool petgridcrlf = true;
        /// <summary>
        /// 显示动画 (自动多层切换)
        /// </summary>
        /// <param name="graph">动画</param>
        /// <param name="EndAction">结束操作</param>
        public void Display(IGraph graph, Action EndAction = null)
        {
            if (PetGrid.Child == graph.This)
            {
                ((IGraph)(PetGrid.Child)).Run(EndAction);
                return;
            }
            else if (PetGrid2.Child == graph.This)
            {
                ((IGraph)(PetGrid2.Child)).Run(EndAction);
                return;
            }
            graph.Run(EndAction);


            if (petgridcrlf)
            {
                ((IGraph)(PetGrid.Child)).Stop(true);
                Dispatcher.Invoke(() => PetGrid2.Child = graph.This);
                Task.Run(() =>
                {
                    Thread.Sleep(25);
                    Dispatcher.Invoke(() => PetGrid.Child = null);
                });
            }
            else
            {
                ((IGraph)(PetGrid2.Child)).Stop(true);
                Dispatcher.Invoke(() => PetGrid.Child = graph.This);
                Task.Run(() =>
                {
                    Thread.Sleep(25);
                    Dispatcher.Invoke(() => PetGrid2.Child = null);
                });
            }
            petgridcrlf = !petgridcrlf;

        }
    }
}
