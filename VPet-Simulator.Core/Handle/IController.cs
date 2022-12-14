using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// 桌宠控制器 需自行实现
    /// </summary>
    public interface IController
    {
        /// <summary>
        /// 移动桌宠位置
        /// </summary>
        /// <param name="X">X轴</param>
        /// <param name="Y">Y轴</param>
        void MoveWindows(double X, double Y);

        /// <summary>
        /// 获取桌宠距离左侧的位置
        /// </summary>
        double GetWindowsDistanceLeft();
        /// <summary>
        /// 获取桌宠距离右侧的位置
        /// </summary>
        double GetWindowsDistanceRight();
        /// <summary>
        /// 获取桌宠距离上方的位置
        /// </summary>
        double GetWindowsDistanceUp();
        /// <summary>
        /// 获取桌宠距离下方的位置
        /// </summary>
        double GetWindowsDistanceDown();
        /// <summary>
        /// 窗体宽度
        /// </summary>
        double WindowsWidth { get; set; }
        /// <summary>
        /// 窗体高度
        /// </summary>
        double WindowsHight { get; set; }
        /// <summary>
        /// 缩放比例 
        /// </summary>
        double ZoomRatio { get; }

    }
}
