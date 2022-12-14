using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static VPet_Simulator.Core.GraphCore;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// 动画显示接口
    /// </summary>
    public interface IGraph
    {
        /// <summary>
        /// 从0开始运行该动画
        /// </summary>
        void Run(Action EndAction = null);
        /// <summary>
        /// 当前动画播放状态
        /// </summary>
        bool PlayState { get; set; }
        /// <summary>
        /// 是否循环播放
        /// </summary>
        bool IsLoop { get; set; }
        /// <summary>
        /// 是否继续播放
        /// </summary>
        bool IsContinue { get; set; }
        /// <summary>
        /// 是否储存到内存以支持快速显示
        /// </summary>
        bool StoreMemory { get; }
        /// <summary>
        /// 该动画UI状态
        /// </summary>
        Save.ModeType ModeType { get;}
        /// <summary>
        /// 该动画UI状态
        /// </summary>
        GraphType GraphType { get; }
        /// <summary>
        /// 当前UI
        /// </summary>
        UIElement This { get; }
        /// <summary>
        /// 停止动画
        /// </summary>
        /// <param name="StopEndAction">停止动画时是否允许执行停止帧</param>
        void Stop(bool StopEndAction = false);
    }
}
