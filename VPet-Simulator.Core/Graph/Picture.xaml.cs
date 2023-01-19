using System;
using System.Collections.Generic;
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

namespace VPet_Simulator.Core
{
    /// <summary>
    /// Picture.xaml 的交互逻辑
    /// </summary>
    public partial class Picture : Image, IGraph
    {
        /// <summary>
        /// 新建新静态图像
        /// </summary>
        /// <param name="path">图片路径</param>
        public Picture(string path, Save.ModeType modetype, GraphCore.GraphType graphType, int length = 1000,  bool isloop = false)
        {
            InitializeComponent();
            ModeType = modetype;
            IsLoop = isloop;
            Length = length;
            Source = new BitmapImage(new Uri(path));
            GraphType = graphType;
        }
        public Save.ModeType ModeType { get; private set; }

        public bool PlayState { get; set; }
        public bool IsLoop { get; set; }
        public int Length { get; set; }
        public bool StoreMemory => true;

        public UIElement This => this;

        public bool IsContinue { get; set; }

        public GraphCore.GraphType GraphType { get; set; }

        public void Run(Action EndAction = null)
        {
            PlayState = true;
            StopEndAction = false;
            Task.Run(() =>
            {
                Thread.Sleep(Length);
                if (IsLoop && PlayState)
                {
                    Run(EndAction);
                }
                else
                {
                    PlayState = false;
                    if (!StopEndAction)
                        EndAction?.Invoke();//运行结束动画时事件
                }
            });
        }
        bool StopEndAction = false;
        public void Stop(bool StopEndAction = false)
        {
            PlayState = false;
            this.StopEndAction = StopEndAction;
        }
    }
}
