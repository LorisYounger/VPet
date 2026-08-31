using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// 动画显示接口 (WPF)
    /// </summary>
    public interface IGraph : IGraphBase
    {
        /// <summary>
        /// 从0开始运行该动画
        /// </summary>
        /// <param name="EndAction">停止动作</param>
        /// <param name="parant">显示位置</param>
        void Run(Decorator parant, Action? EndAction = null);

        /// <summary>
        /// 当前动画播放状态和控制
        /// </summary>
        new TaskControl? Control { get; }

        object? IGraphBase.Control => Control;

        /// <summary>
        /// 停止动画
        /// </summary>
        /// <param name="stopEndAction">停止动画时是否不运行结束动画</param>
        void Stop(bool stopEndAction)
        {
            if (Control == null)
                return;
            if (stopEndAction)
                Control.EndAction = null;
            Control.Type = TaskControl.ControlType.Stop;
        }

        /// <summary>
        /// 设置为继续播放
        /// </summary>
        void SetContinue()
        {
            if (Control != null)
                Control.Type = TaskControl.ControlType.Continue;
        }

        /// <summary>
        /// 停止动画
        /// </summary>
        /// <param name="stopEndAction">停止动画时是否不运行结束动画</param>
        void IGraphBase.Stop(bool stopEndAction)
        {
            Stop(stopEndAction);
        }

        /// <summary>
        /// 设置为继续播放
        /// </summary>
        void IGraphBase.SetContinue()
        {
            SetContinue();
        }

        /// <summary>
        /// 上次使用时间戳, 用于判断是否需要释放资源
        /// </summary>
        long IGraphBase.LastUseTimeTicks => 0;

        /// <summary>
        /// 修改最后使用时间为当前时间，以便在清理空闲缓存时判断是否需要清理
        /// </summary>
        void IGraphBase.Touch() { }

        /// <summary>
        /// 清理空闲缓存, 如果该动画长时间未使用, 则释放资源
        /// </summary>
        /// <param name="nowTicks">当前时间</param>
        void IGraphBase.CleanupIdleCache(long nowTicks) { }

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
            void Run(Decorator parant, ImageSource image, Action? EndAction = null);
        }

        /// <summary>
        /// 动画控制类 (兼容旧代码)
        /// </summary>
        public class TaskControl : GraphTaskControl
        {
            public TaskControl(Action? endAction = null) : base(endAction)
            {
            }
        }
    }
}
