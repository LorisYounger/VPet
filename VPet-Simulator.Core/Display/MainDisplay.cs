using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using static VPet_Simulator.Core.GraphCore;
using Panuon.WPF.UI;
using LinePutScript.Localization.WPF;
using static VPet_Simulator.Core.GraphInfo;
using System.Xml.Linq;
using System.Linq;

namespace VPet_Simulator.Core
{
    public partial class Main
    {
        /// <summary>
        /// 当前动画类型
        /// </summary>
        public GraphInfo DisplayType = new GraphInfo("");
        /// <summary>
        /// 默认循环次数
        /// </summary>
        public int CountNomal = 0;
        /// <summary>
        /// 以标准形式显示当前默认状态
        /// </summary>
        public void DisplayToNomal()
        {
            switch (State)
            {
                default:
                case WorkingState.Nomal:
                    DisplayNomal();
                    return;
                case WorkingState.Sleep:
                    DisplaySleep(true);
                    return;
                case WorkingState.Work:
                    Core.Graph.GraphConfig.Works[StateID].Display(this);
                    return;
                case WorkingState.Travel:
                    //TODO
                    return;
            }
        }
        /// <summary>
        /// 显示默认情况
        /// </summary>
        public void DisplayNomal()
        {
            CountNomal++;
            Display(GraphType.Default, AnimatType.Single, DisplayNomal);
        }
        /// <summary>
        /// 显示结束动画
        /// </summary>
        /// <param name="EndAction">结束后接下来</param>
        /// <returns>是否成功结束</returns>
        public bool DisplayStopMove(Action EndAction)
        {
            var graph = Core.Graph.FindGraph(DisplayType.Name, AnimatType.C_End, Core.Save.Mode);
            if (graph != null)
            {
                if(State == WorkingState.Sleep)
                    State = WorkingState.Nomal;
                Display(graph, EndAction);
                return true;
            }
            //switch (DisplayType)
            //{
            //    case GraphType.Idel:
            //        Display(GraphType.Boring_C_End, EndAction);
            //        return true;
            //    case GraphType.Squat_B_Loop:
            //        Display(GraphType.Squat_C_End, EndAction);
            //        return true;
            //    case GraphType.Crawl_Left_B_Loop:
            //        Display(GraphType.Crawl_Left_C_End, EndAction);
            //        return true;
            //    case GraphType.Crawl_Right_B_Loop:
            //        Display(GraphType.Crawl_Right_C_End, EndAction);
            //        return true;
            //    case GraphType.Fall_Left_B_Loop:
            //        Display(GraphType.Fall_Left_C_End,
            //            () => Display(GraphType.Climb_Up_Left, EndAction));
            //        return true;
            //    case GraphType.Fall_Right_B_Loop:
            //        Display(GraphType.Fall_Right_C_End,
            //            () => Display(GraphType.Climb_Up_Right, EndAction));
            //        return true;
            //    case GraphType.Walk_Left_B_Loop:
            //        Display(GraphType.Walk_Left_C_End, EndAction);
            //        return true;
            //    case GraphType.Walk_Right_B_Loop:
            //        Display(GraphType.Walk_Right_C_End, EndAction);
            //        return true;
            //    case GraphType.Sleep_B_Loop:
            //        State = WorkingState.Nomal;
            //        Display(GraphType.Sleep_C_End, EndAction);
            //        return true;
            //    case GraphType.Idel_StateONE_B_Loop:
            //        Display(GraphType.Idel_StateONE_C_End, EndAction);
            //        return true;
            //    case GraphType.Idel_StateTWO_B_Loop:
            //        Display(GraphType.Idel_StateTWO_C_End, () => Display(GraphType.Idel_StateONE_C_End, EndAction));
            //        return true;
            //        //case GraphType.Climb_Left:
            //        //case GraphType.Climb_Right:
            //        //case GraphType.Climb_Top_Left:
            //        //case GraphType.Climb_Top_Right:
            //        //    DisplayFalled_Left();
            //        //    return true;
            //}
            return false;
        }
        /// <summary>
        /// 尝试触发移动
        /// </summary>
        /// <returns></returns>
        public bool DisplayMove()
        {
            var list = Core.Graph.GraphConfig.Moves.ToList();
            for (int i = Function.Rnd.Next(list.Count); 0 != list.Count; i = Function.Rnd.Next(list.Count))
            {
                var move = list[i];
                if (move.Triggered(Core.Controller))
                {
                    move.Display(this);
                    return true;
                }
                else
                {
                    list.RemoveAt(i);
                }
            }
            return false;
        }
        /// <summary>
        /// 显示摸头情况
        /// </summary>
        public void DisplayTouchHead()
        {
            CountNomal = 0;
            if (Core.Controller.EnableFunction && Core.Save.Strength >= 10 && Core.Save.Feeling < 100)
            {
                Core.Save.StrengthChange(-2);
                Core.Save.FeelingChange(1);
                LabelDisplayShowChangeNumber(LocalizeCore.Translate("体力-{0:f0} 心情+{1:f0}"), 2, 1);
            }
            if (DisplayType.Type == GraphType.Touch_Head)
            {
                if (DisplayType.Animat == AnimatType.A_Start)
                    return;
                else if (DisplayType.Animat == AnimatType.B_Loop)
                    if (Dispatcher.Invoke(() => PetGrid.Tag) is IGraph ig && ig.GraphInfo.Type == GraphType.Touch_Head && ig.GraphInfo.Animat == AnimatType.B_Loop)
                    {
                        ig.IsContinue = true;
                        return;
                    }
                    else if (Dispatcher.Invoke(() => PetGrid2.Tag) is IGraph ig2 && ig2.GraphInfo.Type == GraphType.Touch_Head && ig2.GraphInfo.Animat == AnimatType.B_Loop)
                    {
                        ig2.IsContinue = true;
                        return;
                    }
            }
            Display(GraphType.Touch_Head, AnimatType.A_Start, (graphname) =>
               Display(graphname, AnimatType.B_Loop, (graphname) =>
               DisplayCEndtoNomal(graphname)));
        }
        /// <summary>
        /// 显示摸身体情况
        /// </summary>
        public void DisplayTouchBody()
        {
            CountNomal = 0;
            if (Core.Controller.EnableFunction && Core.Save.Strength >= 10 && Core.Save.Feeling < 100)
            {
                Core.Save.StrengthChange(-2);
                Core.Save.FeelingChange(1);
                LabelDisplayShowChangeNumber(LocalizeCore.Translate("体力-{0:f0} 心情+{1:f0}"), 2, 1);
            }
            if (DisplayType.Type == GraphType.Touch_Body)
            {
                if (DisplayType.Animat == AnimatType.A_Start)
                    return;
                else if (DisplayType.Animat == AnimatType.B_Loop)
                    if (Dispatcher.Invoke(() => PetGrid.Tag) is IGraph ig && ig.GraphInfo.Type == GraphType.Touch_Body && ig.GraphInfo.Animat == AnimatType.B_Loop)
                    {
                        ig.IsContinue = true;
                        return;
                    }
                    else if (Dispatcher.Invoke(() => PetGrid2.Tag) is IGraph ig2 && ig2.GraphInfo.Type == GraphType.Touch_Body && ig2.GraphInfo.Animat == AnimatType.B_Loop)
                    {
                        ig2.IsContinue = true;
                        return;
                    }
            }
            Display(GraphType.Touch_Body, AnimatType.A_Start, (graphname) =>
             Display(graphname, AnimatType.B_Loop, (graphname) =>
             DisplayCEndtoNomal(graphname)));
        }
        /// <summary>
        /// 显示待机(模式1)情况
        /// </summary>
        public void DisplayIdel_StateONE()
        {
            looptimes = 0;
            CountNomal = 0;
            var name = Core.Graph.FindName(GraphType.State_ONE);
            var list = Core.Graph.FindGraphs(name, AnimatType.A_Start, Core.Save.Mode)?.FindAll(x => x.GraphInfo.Type == GraphType.State_ONE);
            if (list != null && list.Count > 0)
                Display(list[Function.Rnd.Next(list.Count)], () => DisplayIdel_StateONEing(name));
            else
                Display(GraphType.State_ONE, AnimatType.A_Start, DisplayIdel_StateONEing);
        }
        /// <summary>
        /// 显示待机(模式1)情况
        /// </summary>
        private void DisplayIdel_StateONEing(string graphname)
        {
            if (Function.Rnd.Next(++looptimes) > Core.Graph.GraphConfig.GetDuration(graphname))
                switch (Function.Rnd.Next(2 + CountNomal))
                {
                    case 0:
                        DisplayIdel_StateTWO(graphname);
                        break;
                    default:
                        Display(graphname, AnimatType.C_End, GraphType.State_ONE, DisplayNomal);
                        break;
                }
            else
            {
                Display(graphname, AnimatType.B_Loop, GraphType.State_ONE, DisplayIdel_StateONEing);
            }
        }
        /// <summary>
        /// 显示待机(模式2)情况
        /// </summary>
        public void DisplayIdel_StateTWO(string graphname)
        {
            looptimes = 0;
            CountNomal++;
            Display(graphname, AnimatType.A_Start, GraphType.State_TWO, DisplayIdel_StateTWOing);
        }
        /// <summary>
        /// 显示待机(模式2)情况
        /// </summary>
        private void DisplayIdel_StateTWOing(string graphname)
        {
            if (Function.Rnd.Next(++looptimes) > Core.Graph.GraphConfig.GetDuration(graphname))
            {
                Display(graphname, AnimatType.C_End, GraphType.State_TWO, DisplayIdel_StateONEing);
            }
            else
            {
                Display(graphname, AnimatType.B_Loop, GraphType.State_TWO, DisplayIdel_StateTWOing);
            }
        }

        int looptimes;
        /// <summary>
        /// 显示待机情况 (只有符合条件的才会显示)
        /// </summary>
        public bool DisplayIdel()
        {
            if (Core.Graph.GraphsName.TryGetValue(GraphType.Idel, out var gl))
            {
                var list = gl.ToList();
                for (int i = Function.Rnd.Next(list.Count); 0 != list.Count; i = Function.Rnd.Next(list.Count))
                {
                    var idelname = list[i];
                    var ig = Core.Graph.FindGraphs(idelname, AnimatType.A_Start, Core.Save.Mode);
                    if (ig != null)
                    {
                        looptimes = 0;
                        CountNomal = 0;
                        DisplayBLoopingToNomal(idelname, Core.Graph.GraphConfig.GetDuration(idelname));
                        return true;
                    }
                    else
                    {
                        list.RemoveAt(i);
                    }
                }
                return false;
            }
            else
                return false;
        }
        /// <summary>
        /// 显示B循环+C循环+ToNomal
        /// </summary>
        public Action<string> DisplayBLoopingToNomal(int looplength) => (gn) => DisplayBLoopingToNomal(gn, looplength);
        /// <summary>
        /// 显示B循环+C循环+ToNomal
        /// </summary>
        public void DisplayBLoopingToNomal(string graphname, int loopLength)
        {
            if (Function.Rnd.Next(++looptimes) > loopLength)
                DisplayCEndtoNomal(graphname);
            else
                Display(graphname, AnimatType.B_Loop, DisplayBLoopingToNomal(loopLength));
        }


        /// <summary>
        /// 显示睡觉情况
        /// </summary>
        public void DisplaySleep(bool force = false)
        {
            looptimes = 0;
            CountNomal = 0;
            if (force)
            {
                State = WorkingState.Sleep;
                Display(GraphType.Sleep, AnimatType.A_Start, DisplayBLoopingForce);
            }
            else
                Display(GraphType.Sleep, AnimatType.A_Start, (x) => DisplayBLoopingToNomal(Core.Graph.GraphConfig.GetDuration(x)));
        }
        /// <summary>
        /// 显示B循环 (强制)
        /// </summary>
        public void DisplayBLoopingForce(string graphname)
        {
            Display(graphname, AnimatType.B_Loop, DisplayBLoopingForce);
        }

        //显示工作现在直接由显示调用,没有DisplayWork, 学习同理

        /// <summary>
        /// 显示拖拽情况
        /// </summary>
        public void DisplayRaised()
        {
            //位置迁移: 254-128           
            MainGrid.MouseMove += MainGrid_MouseMove;
            MainGrid.MouseMove -= MainGrid_MouseWave;
            rasetype = 0;
            DisplayRaising();
        }
        int rasetype = int.MinValue;
        /// <summary>
        /// 显示拖拽中
        /// </summary>
        private void DisplayRaising(string name = null)
        {
            Console.WriteLine(rasetype);
            switch (rasetype)
            {
                case int.MinValue:
                    break;
                case -1:
                    rasetype = int.MinValue;
                    if (string.IsNullOrEmpty(name))
                        Display(GraphType.Raised_Static, AnimatType.C_End, DisplayToNomal);
                    else
                        Display(name, AnimatType.C_End, GraphType.Raised_Static, DisplayToNomal);
                    return;
                case 0:
                case 1:
                case 2:
                    rasetype++;
                    if (string.IsNullOrEmpty(name))
                        Display(GraphType.Raised_Dynamic, AnimatType.Single, DisplayRaising);
                    else
                        Display(name, AnimatType.Single, GraphType.Raised_Dynamic, DisplayRaising);
                    return;
                case 3:
                    rasetype++;
                    if (string.IsNullOrEmpty(name))
                        Display(name, AnimatType.A_Start, DisplayRaising);
                    else
                        Display(name, AnimatType.A_Start, GraphType.Raised_Static, DisplayRaising);
                    return;
                default:
                    rasetype = 4;
                    if (string.IsNullOrEmpty(name))
                        Display(name, AnimatType.B_Loop, DisplayRaising);
                    else
                        Display(name, AnimatType.B_Loop, GraphType.Raised_Static, DisplayRaising);
                    return;
            }
        }

        /// <summary>
        /// 显示结束动画到正常动画 (DisplayToNomal)
        /// </summary>
        public void DisplayCEndtoNomal(string graphname)
        {
            Display(graphname, AnimatType.C_End, DisplayToNomal);
        }




        /// <summary>
        /// 显示动画 (自动查找和匹配)
        /// </summary>
        /// <param name="Type">动画类型</param>
        /// <param name="EndAction">动画结束后操作(附带名字)</param>
        ///// <param name="storernd">是否储存随机数字典</param>
        /// <param name="animat">动画的动作 Start Loop End</param>
        public void Display(GraphType Type, AnimatType animat, Action<string> EndAction = null)
        {
            var name = Core.Graph.FindName(Type);
            Display(name, animat, EndAction);
        }
        /// <summary>
        /// 显示动画 根据名字播放
        /// </summary>
        /// <param name="name">动画名称</param>
        /// <param name="EndAction">动画结束后操作(附带名字)</param>
        /// <param name="animat">动画的动作 Start Loop End</param>
        public void Display(string name, AnimatType animat, Action<string> EndAction = null)
        {
            Display(Core.Graph.FindGraph(name, animat, Core.Save.Mode), new Action(() => EndAction.Invoke(name)));
        }
        /// <summary>
        /// 显示动画 根据名字和类型查找运行,若无则查找类型
        /// </summary>
        /// <param name="Type">动画类型</param>
        /// <param name="name">动画名称</param>
        /// <param name="EndAction">动画结束后操作(附带名字)</param>
        /// <param name="animat">动画的动作 Start Loop End</param>
        public void Display(string name, AnimatType animat, GraphType Type, Action<string> EndAction = null)
        {
            var list = Core.Graph.FindGraphs(name, animat, Core.Save.Mode)?.FindAll(x => x.GraphInfo.Type == Type);
            if (list != null && list.Count > 0)
                Display(list[Function.Rnd.Next(list.Count)], () => EndAction(name));
            else
                Display(Type, animat, EndAction);
        }
        /// <summary>
        /// 显示动画 根据名字和类型查找运行,若无则查找类型
        /// </summary>
        /// <param name="Type">动画类型</param>
        /// <param name="name">动画名称</param>
        /// <param name="EndAction">动画结束后操作</param>
        /// <param name="animat">动画的动作 Start Loop End</param>
        public void Display(string name, AnimatType animat, GraphType Type, Action EndAction = null)
        {
            var list = Core.Graph.FindGraphs(name, animat, Core.Save.Mode)?.FindAll(x => x.GraphInfo.Type == Type);
            if (list.Count > 0)
                Display(list[Function.Rnd.Next(list.Count)], EndAction);
            else
                Display(Type, animat, EndAction);
        }
        /// <summary>
        /// 显示动画 (自动查找和匹配)
        /// </summary>
        /// <param name="Type">动画类型</param>
        /// <param name="EndAction">动画结束后操作</param>
        ///// <param name="storernd">是否储存随机数字典</param>
        /// <param name="animat">动画的动作 Start Loop End</param>
        public void Display(GraphType Type, AnimatType animat, Action EndAction = null)
        {
            var name = Core.Graph.FindName(Type);
            Display(name, animat, EndAction);
        }
        /// <summary>
        /// 显示动画 根据名字播放
        /// </summary>
        /// <param name="name">动画名称</param>
        /// <param name="EndAction">动画结束后操作</param>
        /// <param name="animat">动画的动作 Start Loop End</param>
        public void Display(string name, AnimatType animat, Action EndAction = null)
        {
            Display(Core.Graph.FindGraph(name, animat, Core.Save.Mode), EndAction);
        }
        bool petgridcrlf = true;
        /// <summary>
        /// 显示动画 (自动多层切换)
        /// </summary>
        /// <param name="graph">动画</param>
        /// <param name="EndAction">结束操作</param>
        public void Display(IGraph graph, Action EndAction = null)
        {
            if (graph == null)
            {
                EndAction?.Invoke();
                return;
            }
            //if(graph.GraphType == GraphType.Climb_Up_Left)
            //{
            //    Dispatcher.Invoke(() => Say(graph.GraphType.ToString()));
            //}
            DisplayType = graph.GraphInfo;
            var PetGridTag = Dispatcher.Invoke(() => PetGrid.Tag);
            var PetGrid2Tag = Dispatcher.Invoke(() => PetGrid2.Tag);
            if (PetGridTag == graph)
            {
                petgridcrlf = true;
                ((IGraph)(PetGrid2Tag)).Stop(true);
                Dispatcher.Invoke(() =>
                {
                    PetGrid.Visibility = Visibility.Visible;
                    PetGrid2.Visibility = Visibility.Hidden;
                });
                graph.Run(PetGrid, EndAction);//(x) => PetGrid.Child = x
                return;
            }
            else if (PetGrid2Tag == graph)
            {
                petgridcrlf = false;
                ((IGraph)(PetGridTag)).Stop(true);
                Dispatcher.Invoke(() =>
                {
                    PetGrid2.Visibility = Visibility.Visible;
                    PetGrid.Visibility = Visibility.Hidden;
                });
                graph.Run(PetGrid2, EndAction);
                return;
            }

            if (petgridcrlf)
            {
                graph.Run(PetGrid2, EndAction);
                ((IGraph)(PetGridTag)).Stop(true);
                Dispatcher.Invoke(() =>
                {
                    PetGrid.Visibility = Visibility.Hidden;
                    PetGrid2.Visibility = Visibility.Visible;
                    //PetGrid2.Tag = graph;
                });
            }
            else
            {
                graph.Run(PetGrid, EndAction);
                ((IGraph)(PetGrid2Tag)).Stop(true);
                Dispatcher.Invoke(() =>
                {
                    PetGrid2.Visibility = Visibility.Hidden;
                    PetGrid.Visibility = Visibility.Visible;
                    //PetGrid.Tag = graph;
                });
            }
            petgridcrlf = !petgridcrlf;
            GC.Collect();
        }
        /// <summary>
        /// 查找可用与显示的Border (自动多层切换)
        /// </summary>
        /// <param name="graph">动画</param>
        public Border FindDisplayBorder(IGraph graph)
        {
            DisplayType = graph.GraphInfo;
            var PetGridTag = Dispatcher.Invoke(() => PetGrid.Tag);
            var PetGrid2Tag = Dispatcher.Invoke(() => PetGrid2.Tag);
            if (PetGridTag == graph)
            {
                petgridcrlf = true;
                ((IGraph)(PetGrid2Tag)).Stop(true);
                Dispatcher.Invoke(() =>
                {
                    PetGrid.Visibility = Visibility.Visible;
                    PetGrid2.Visibility = Visibility.Hidden;
                });
                return PetGrid;
            }
            else if (PetGrid2Tag == graph)
            {
                petgridcrlf = false;
                ((IGraph)(PetGridTag)).Stop(true);
                Dispatcher.Invoke(() =>
                {
                    PetGrid2.Visibility = Visibility.Visible;
                    PetGrid.Visibility = Visibility.Hidden;
                });
                return PetGrid2;
            }

            if (petgridcrlf)
            {
                ((IGraph)(PetGridTag)).Stop(true);
                Dispatcher.Invoke(() =>
                {
                    PetGrid.Visibility = Visibility.Hidden;
                    PetGrid2.Visibility = Visibility.Visible;
                    //PetGrid2.Tag = graph;
                });
                petgridcrlf = !petgridcrlf;
                GC.Collect();
                return PetGrid2;
            }
            else
            {
                ((IGraph)(PetGrid2Tag)).Stop(true);
                Dispatcher.Invoke(() =>
                {
                    PetGrid2.Visibility = Visibility.Hidden;
                    PetGrid.Visibility = Visibility.Visible;
                    //PetGrid.Tag = graph;
                });
                petgridcrlf = !petgridcrlf;
                GC.Collect();
                return PetGrid;
            }

        }
    }
}
