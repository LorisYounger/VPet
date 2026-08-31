namespace VPet_Simulator.Core
{
    /// <summary>
    /// 跨平台工具栏接口
    /// </summary>
    public interface IToolBarBase : IUiModuleBase
    {
        /// <summary>
        /// 加载工作菜单
        /// </summary>
        void LoadWork();

        /// <summary>
        /// 加载自定义菜单
        /// </summary>
        void LoadDIY();

        /// <summary>
        /// 显示工具栏
        /// </summary>
        void Show();
    }
}
