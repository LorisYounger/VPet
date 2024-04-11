using System;
using System.Windows.Controls;
using System.Windows.Media;

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
        void Run(Decorator parant, Action EndAction = null);
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
        /// 是否准备完成
        /// </summary>
        bool IsReady { get; }
        /// <summary>
        /// 该动画信息
        /// </summary>
        GraphInfo GraphInfo { get; }
        /// <summary>
        /// 停止动画
        /// </summary>
        /// <param name="StopEndAction">停止动画时是否允许执行停止帧</param>
        void Stop(bool StopEndAction = false);
        /// <summary>
        /// 指示该ImageRun支持
        /// </summary>
        public interface IRunImage : IGraph
        {
            /// <summary>
            /// 从0开始运行该动画
            /// </summary>
            /// <param name="parant">显示位置</param>
            /// <param name="EndAction">结束方法</param>
            /// <param name="image">额外图片</param>
            void Run(Decorator parant, ImageSource image, Action EndAction = null);
        }
    }
}
