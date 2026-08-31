using System;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// 跨平台 UI 模块基础接口
    /// </summary>
    public interface IUiModuleBase : IDisposable
    {
        /// <summary>
        /// 是否可见
        /// </summary>
        bool IsVisible { get; set; }

        /// <summary>
        /// 平台具体视图对象
        /// </summary>
        object View { get; }
    }
}
