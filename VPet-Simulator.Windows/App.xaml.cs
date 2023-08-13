using LinePutScript.Localization.WPF;
using System.Windows;

namespace VPet_Simulator.Windows
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public App() : base()
        {
#if !DEBUG
            base.DispatcherUnhandledException += App_DispatcherUnhandledException;
#endif
            //AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            string errstr = "游戏发生错误,可能是".Translate() + (string.IsNullOrWhiteSpace(CoreMOD.NowLoading) ?
                "游戏或者MOD".Translate() : $"MOD({CoreMOD.NowLoading})") +
                "导致的\n如有可能请发送 错误信息截图和引发错误之前的操作 给开发者:service@exlb.net\n感谢您对游戏开发的支持\n".Translate()
                + e.Exception.ToString();
            if (MainWindow == null)
            {
                MessageBox.Show(errstr, "游戏致命性错误".Translate());
                return;
            }
            else
            {
                new winReport(((MainWindow)MainWindow), errstr).Show();
                return;
            }
        }
    }
}
