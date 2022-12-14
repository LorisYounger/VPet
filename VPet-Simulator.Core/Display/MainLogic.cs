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
        /// <summary>
        /// 是否在默认情况(playnoaml)
        /// </summary>
        public bool IsNomal = true;

        public Timer EventTimer = new Timer(15000)
        {
            AutoReset = true,
            Enabled = true
        };

        private void EventTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            //TODO:饮食等乱七八糟的消耗

            if (IsNomal)
                switch (1)//Function.Rnd.Next(10))
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
                    case 10:
                        DisplayClimb_Top_Right();                        
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
    }
}
