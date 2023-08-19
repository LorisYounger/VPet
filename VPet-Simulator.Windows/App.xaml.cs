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

            var expt = e.Exception.ToString();
            if (expt.Contains("value") && expt.Contains("Panuon.WPF.UI.Internal.Utils") && expt.Contains("NaN"))
            {
                MessageBox.Show("由于修改游戏数据导致数据溢出,存档可能会出错\n开发者提醒您请不要使用过于超模的MOD".Translate()); 
            }

            string errstr = "游戏发生错误,可能是".Translate() + (string.IsNullOrWhiteSpace(CoreMOD.NowLoading) ?
                "游戏或者MOD".Translate() : $"MOD({CoreMOD.NowLoading})") +
                "导致的\n如有可能请发送 错误信息截图和引发错误之前的操作 给开发者:service@exlb.net\n感谢您对游戏开发的支持\n".Translate()
                + expt;
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
