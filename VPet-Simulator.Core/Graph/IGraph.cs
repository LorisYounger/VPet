using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// 动画显示接口
    /// </summary>
    public interface IGraph : IEquatable<object>, IDisposable
    {
        /// <summary>
        /// 从0开始运行该动画
        /// </summary>
        /// <param name="EndAction">停止动作</param>
        /// <param name="parant">显示位置</param>
        void Run(Decorator parant, Action EndAction = null);

        /// <summary>
        /// 是否循环播放
        /// </summary>
        bool IsLoop { get; set; }

        /// <summary>
        /// 是否准备完成
        /// </summary>
        bool IsReady { get; }

        /// <summary>
        /// 是否读取失败
        /// </summary>
        bool IsFail { get; }
        /// <summary>
        /// 失败报错信息
        /// </summary>
        string FailMessage { get; }
        /// <summary>
        /// 该动画信息
        /// </summary>
        GraphInfo GraphInfo { get; }

        /// <summary>
        /// 当前动画播放状态和控制
        /// </summary>
        TaskControl Control { get; }

        /// <summary>
        /// 停止动画
        /// </summary>
        /// <param name="StopEndAction">停止动画时是否不运行结束动画</param>
        void Stop(bool StopEndAction)
        {
            if (Control == null)
                return;
            if (StopEndAction)
                Control.EndAction = null;
            Control.Type = TaskControl.ControlType.Stop;
        }
        /// <summary>
        /// 设置为继续播放
        /// </summary>
        void SetContinue()
        {
            Control.Type = TaskControl.ControlType.Continue;
        }
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
        /// <summary>
        /// 动画文件路径, 可能是文件夹或文件
        /// </summary>
        string Path { get; }

        /// <summary>
        /// 动画控制类
        /// </summary>
        public class TaskControl
        {
            /// <summary>
            /// 当前动画播放状态
            /// </summary>
            public bool PlayState => Type != ControlType.Status_Stoped && Type != ControlType.Stop;
            /// <summary>
            /// 设置为继续播放
            /// </summary>
            public void SetContinue() { Type = ControlType.Continue; }
            /// <summary>
            /// 停止播放
            /// </summary>
            public void Stop(Action endAction = null) { EndAction = endAction; Type = ControlType.Stop; }
            /// <summary>
            /// 控制类型
            /// </summary>
            public enum ControlType
            {
                /// <summary>
                /// 维持现状, 不进行任何超控
                /// </summary>
                Status_Quo,
                /// <summary>
                /// 停止当前动画
                /// </summary>
                Stop,
                /// <summary>
                /// 播放完成后继续播放,仅生效一次, 之后将恢复为Status_Quo
                /// </summary>
                Continue,
                /// <summary>
                /// 动画已停止
                /// </summary>
                Status_Stoped,
            }
            /// <summary>
            /// 结束动作
            /// </summary>
            public Action EndAction;
            /// <summary>
            /// 控制类型
            /// </summary>
            public ControlType Type = ControlType.Status_Quo;
            /// <summary>
            /// 为动画控制类提供操作和结束动作
            /// </summary>
            /// <param name="endAction"></param>
            public TaskControl(Action endAction = null)
            {
                EndAction = endAction;
            }
        }
    }
}
