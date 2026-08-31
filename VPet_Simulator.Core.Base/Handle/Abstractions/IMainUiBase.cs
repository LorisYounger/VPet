namespace VPet_Simulator.Core
{
    /// <summary>
    /// 跨平台主界面模块接口
    /// </summary>
    public interface IMainUiBase : IUiModuleBase
    {
        /// <summary>
        /// 是否已开始运行
        /// </summary>
        bool IsWorking { get; }

        /// <summary>
        /// 消息栏
        /// </summary>
        IMessageBarBase? MsgBarBase { get; }

        /// <summary>
        /// 工具栏
        /// </summary>
        IToolBarBase? ToolBarBase { get; }

        /// <summary>
        /// 工作计时器
        /// </summary>
        IWorkTimerBase? WorkTimerBase { get; }
    }
}
