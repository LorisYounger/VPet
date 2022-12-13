using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// 桌宠控制器
    /// </summary>
    public abstract class IController
    {
        /// <summary>
        /// 移动桌宠位置
        /// </summary>
        /// <param name="X">X轴</param>
        /// <param name="Y">Y轴</param>
        public abstract void MoveWindows(double X, double Y);
        
        /// <summary>
        /// 获取桌宠距离左侧的位置
        /// </summary>
        public abstract double GetWindowsDistanceLeft();
        /// <summary>
        /// 获取桌宠距离右侧的位置
        /// </summary>
        public abstract double GetWindowsDistanceRight();
        /// <summary>
        /// 获取桌宠距离上方的位置
        /// </summary>
        public abstract double GetWindowsDistanceUp();
        /// <summary>
        /// 获取桌宠距离下方的位置
        /// </summary>
        public abstract double GetWindowsDistanceDown();
        /// <summary>
        /// 窗体宽度
        /// </summary>
        public abstract double WindowsWidth { get; set; }
        /// <summary>
        /// 窗体高度
        /// </summary>
        public abstract double WindowsHight { get; set; }

    }
}
