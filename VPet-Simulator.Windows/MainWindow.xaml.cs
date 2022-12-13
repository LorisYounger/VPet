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
        public MainWindow()
        {
            InitializeComponent();

            //不存在就关掉
            var modpath = new DirectoryInfo(ModPath + @"\0000_core\pet\vup");
            if (!modpath.Exists)
            {
                MessageBox.Show("缺少模组Core,无法启动桌宠", "启动错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            var core = new GameCore();
            core.Setting = new Setting();
            core.Controller = new MWController(this);
            core.Graph = new GraphCore();
            core.Save = new Save();
            LoadGraph(core.Graph, modpath, "");
            DisplayGrid.Children.Add(new Main(core));            
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
        public static void LoadGraph(GraphCore graph, DirectoryInfo di, string path_name)
        {
            var list = di.EnumerateDirectories();
            if (list.Count() == 0)
            {
                if (File.Exists(di.FullName + @"\info.lps"))
                {//如果自带描述信息,则手动加载

                }
                else
                {//自动加载 PNG ANMIN
                    path_name = path_name.Trim('_').ToLower();
                    for (int i = 0; i < GraphTypeValue.Length; i++)
                    {
                        if (path_name.StartsWith(GraphTypeValue[i][0]))
                        {
                            if (path_name.Contains("happy"))
                            {
                                graph.AddGraph(new PNGAnimation(di.FullName, Save.ModeType.Happy, (GraphType)i, GraphTypeValue[i][1], GraphTypeValue[i][2]), (GraphType)i);
                            }
                            if (path_name.Contains("nomal"))
                            {
                                graph.AddGraph(new PNGAnimation(di.FullName, Save.ModeType.Nomal, (GraphType)i, GraphTypeValue[i][1], GraphTypeValue[i][2]), (GraphType)i);
                            }
                            if (path_name.Contains("poorcondition"))
                            {
                                graph.AddGraph(new PNGAnimation(di.FullName, Save.ModeType.PoorCondition, (GraphType)i, GraphTypeValue[i][1], GraphTypeValue[i][2]), (GraphType)i);
                            }
                            if (path_name.Contains("ill"))
                            {
                                graph.AddGraph(new PNGAnimation(di.FullName, Save.ModeType.Ill, (GraphType)i, GraphTypeValue[i][1], GraphTypeValue[i][2]), (GraphType)i);
                            }
                            return;
                        }
                    }
#if DEBUG
                    throw new Exception("未知的图像类型: " + path_name);
#endif
                }
            }
            else
                foreach (var p in list)
                {
                    LoadGraph(graph, p, path_name + "_" + p.Name);
                }
        }
    }
}
