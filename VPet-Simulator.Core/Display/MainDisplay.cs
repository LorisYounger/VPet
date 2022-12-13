using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
            if (((IGraph)PetGrid.Child).GraphType == GraphCore.GraphType.Touch_Head_B_Loop)
            {
                ((IGraph)PetGrid.Child).IsContinue = true;
                return;
            }
            Display(Core.Graph.FindGraph(GraphCore.GraphType.Touch_Head_A_Start, Core.Save.Mode), () =>
               Display(Core.Graph.FindGraph(GraphCore.GraphType.Touch_Head_B_Loop, Core.Save.Mode), () =>
               Display(Core.Graph.FindGraph(GraphCore.GraphType.Touch_Head_C_End, Core.Save.Mode), () =>
               Display(Core.Graph.FindGraph(GraphCore.GraphType.Default, Core.Save.Mode)
            ))));
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
        /// <summary>
        /// 显示拖拽中
        /// </summary>
        public void DisplayRaising()
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
