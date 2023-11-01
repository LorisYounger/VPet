﻿using LinePutScript.Localization.WPF;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using VPet_Simulator.Windows.Interface;

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
        public static string[] Args { get; set; }
        /// <summary>
        /// 多存档系统名称
        /// </summary>
        public static List<string> MultiSaves { get; set; } = new List<string>();

        public static List<MainWindow> MainWindows { get; set; } = new List<MainWindow>();

        protected override void OnStartup(StartupEventArgs e)
        {
            Args = e.Args;

            foreach (var mss in new DirectoryInfo(ExtensionValue.BaseDirectory).GetFiles("Setting*.lps"))
            {
                var n = mss.Name.Substring(7).Trim('-');
                MultiSaves.Add(n.Substring(0, n.Length - 4));
            }

            if (MultiSaves.Count == 0)
            {
                MultiSaves.Add("");
            }
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;

            var expt = e.Exception.ToString();
            if (expt.Contains("value") && expt.Contains("Panuon.WPF.UI.Internal.Utils") && expt.Contains("NaN"))
            {
                MessageBox.Show("由于修改游戏数据导致数据溢出,存档可能会出错\n开发者提醒您请不要使用过于超模的MOD".Translate());
                return;
            }
            else if (expt.Contains("System.IO.FileNotFoundException") && expt.Contains("cache"))
            {
                MessageBox.Show("缓存被其他软件删除,游戏无法继续运行\n请重启游戏重新生成缓存".Translate());
                return;
            }
            else if (expt.Contains("0x80070008"))
            {
                MessageBox.Show("游戏内存不足,请修改设置中渲染分辨率以便降低内存使用".Translate());
                return;
            }
            else if (expt.Contains("VPet.Plugin"))
            {
                var exptin = expt.Split('\n').First(x => x.Contains("VPet.Plugin"));
                exptin = exptin.Substring(exptin.IndexOf("VPet.Plugin"));
                MessageBox.Show("游戏发生错误,可能是".Translate() + $"MOD({exptin})" +
                    "导致的\n如有可能请发送 错误信息截图和引发错误之前的操作给相应MOD作者\n感谢您对MOD开发的支持\n".Translate()
                     + expt, "游戏发生错误,可能是".Translate() + exptin);
                return;
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
