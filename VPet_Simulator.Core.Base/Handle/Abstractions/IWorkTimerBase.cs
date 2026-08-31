using System;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// 跨平台工作计时器接口
    /// </summary>
    public interface IWorkTimerBase : IUiModuleBase
    {
        /// <summary>
        /// 显示模式
        /// </summary>
        int DisplayType { get; set; }

        /// <summary>
        /// 累计获得数值
        /// </summary>
        double GetCount { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        DateTime StartTime { get; set; }

        /// <summary>
        /// 显示时间跨度
        /// </summary>
        void ShowTimeSpan(TimeSpan ts);

        /// <summary>
        /// 停止计时
        /// </summary>
        void Stop();
    }
}
