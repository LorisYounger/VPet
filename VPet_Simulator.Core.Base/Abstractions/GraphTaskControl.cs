using System;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// 动画控制类 (平台无关)
    /// </summary>
    public class GraphTaskControl
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
        public void Stop(Action? endAction = null) { EndAction = endAction; Type = ControlType.Stop; }

        /// <summary>
        /// 控制类型
        /// </summary>
        public enum ControlType
        {
            Status_Quo,
            Stop,
            Continue,
            Status_Stoped,
        }

        /// <summary>
        /// 结束动作
        /// </summary>
        public Action? EndAction;

        /// <summary>
        /// 控制类型
        /// </summary>
        public ControlType Type = ControlType.Status_Quo;

        public GraphTaskControl(Action? endAction = null)
        {
            EndAction = endAction;
        }
    }
}
