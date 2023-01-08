using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VPet_Simulator.Core;
using static VPet_Simulator.Core.GraphCore;

namespace VPet_Simulator.Windows
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public readonly string ModPath = Environment.CurrentDirectory + @"\mod";
        public readonly bool IsSteamUser;
        public Setting Set;
        public List<CorePet> Pets = new List<CorePet>();
        public List<CoreMOD> CoreMODs = new List<CoreMOD>();
        public GameCore Core = new GameCore();
        public MainWindow()
        {
            //判断是不是Steam用户,因为本软件会发布到Steam
            //在 https://store.steampowered.com/app/1920960/VPet
            try
            {
                SteamClient.Init(1920960, true);
                SteamClient.RunCallbacks();
                IsSteamUser = SteamClient.IsValid;
                ////同时看看有没有买dlc,如果有就添加dlc按钮
                //if (Steamworks.SteamApps.IsDlcInstalled(1386450))
                //    dlcToolStripMenuItem.Visible = true;
            }
            catch
            {
                IsSteamUser = false;
            }

            //加载游戏设置
            if (new FileInfo(AppDomain.CurrentDomain.BaseDirectory + @"\Setting.lps").Exists)
            {
                Set = new Setting(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"\Setting.lps"));
            }
            else
                Set = new Setting("Setting#VPET:|\n");

            InitializeComponent();

            //不存在就关掉
            var modpath = new DirectoryInfo(ModPath + @"\0000_core\pet\vup");
            if (!modpath.Exists)
            {
                MessageBox.Show("缺少模组Core,无法启动桌宠", "启动错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }
            Task.Run(GameLoad);
        }
        public void GameLoad()
        {
            //加载所有MOD
            List<DirectoryInfo> Path = new List<DirectoryInfo>();
            Path.AddRange(new DirectoryInfo(ModPath).EnumerateDirectories());
            if (IsSteamUser)//如果是steam用户,尝试加载workshop
            {
                Dispatcher.Invoke(new Action(() => LoadingText.Content = "尝试加载 Steam Workshop"));
                int i = 1;
                while (true)
                {
                    var page = Steamworks.Ugc.Query.ItemsReadyToUse.GetPageAsync(i++).Result;
                    if (page.HasValue && page.Value.ResultCount != 0)
                    {
                        foreach (Steamworks.Ugc.Item entry in page.Value.Entries)
                        {
                            if (entry.Directory != null)
                                Path.Add(new DirectoryInfo(entry.Directory));
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            //加载mod
            foreach (DirectoryInfo di in Path)
            {
                if (!File.Exists(di.FullName + @"\info.lps"))
                    continue;
                Dispatcher.Invoke(new Action(() => LoadingText.Content = $"尝试加载 MOD数据: {di.Name}"));
                CoreMODs.Add(new CoreMOD(di, this));
            }
            Dispatcher.Invoke(new Action(() => LoadingText.Content = "尝试加载游戏内容"));
            //加载游戏内容
            Core.Controller = new MWController(this);
            Dispatcher.Invoke(new Action(() => Core.Graph = Pets[0].Graph));
            Core.Save = new Save();
            Dispatcher.Invoke(new Action(() => LoadingText.Visibility = Visibility.Collapsed));
            Dispatcher.Invoke(new Action(() => DisplayGrid.Child = new Main(Core)));
        }

        //public void DEBUGValue()
        //{
        //    Dispatcher.Invoke(() =>
        //    {
        //        Console.WriteLine("Left: " + mwc.GetWindowsDistanceLeft());
        //        Console.WriteLine("Right: " + mwc.GetWindowsDistanceRight());
        //    });
        //    Thread.Sleep(1000);
        //    DEBUGValue(); 
        //}

    }
}
