using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows;
using System.IO;
using System.Windows.Controls;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// Picture.xaml 的交互逻辑
    /// </summary>
    public partial class Picture : IGraph
    {
        /// <summary>
        /// 新建新静态图像
        /// </summary>
        /// <param name="path">图片路径</param>
        public Picture(GraphCore graphCore, string path, Save.ModeType modetype, GraphCore.GraphType graphType, int length = 1000, bool isloop = false)
        {
            ModeType = modetype;
            IsLoop = isloop;
            Length = length;
            GraphCore = graphCore;
            Path = path;
            GraphType = graphType;
            if (!GraphCore.CommConfig.ContainsKey("PIC_Setup"))
            {
                GraphCore.CommConfig["PIC_Setup"] = true;
                GraphCore.CommUIElements["Image1.Picture"] = new System.Windows.Controls.Image() { Width = 500, Height = 500 };
                GraphCore.CommUIElements["Image2.Picture"] = new System.Windows.Controls.Image() { Width = 500, Height = 500 };
                GraphCore.CommUIElements["Image3.Picture"] = new System.Windows.Controls.Image() { Width = 500, Height = 500 };
            }
        }
        /// <summary>
        /// 图片资源
        /// </summary>
        public string Path;
        public Save.ModeType ModeType { get; private set; }
        private GraphCore GraphCore;
        public bool PlayState { get; set; }
        public bool IsLoop { get; set; }
        public int Length { get; set; }
        //public bool StoreMemory => true;//经过测试,储存到内存好处多多,不储存也要占用很多内存,干脆存了吧
        public bool IsContinue { get; set; }

        public GraphCore.GraphType GraphType { get; set; }

        public void Run(Border parant, Action EndAction = null)
        {
            PlayState = true;
            StopEndAction = false;
            if (parant.Tag != this)
            {
                System.Windows.Controls.Image img;
                if (parant.Child == GraphCore.CommUIElements["Image1.Picture"])
                {
                    img = (System.Windows.Controls.Image)GraphCore.CommUIElements["Image1.Picture"];
                }
                else
                {
                    img = (System.Windows.Controls.Image)GraphCore.CommUIElements["Image2.Picture"];
                    if (parant.Child != GraphCore.CommUIElements["Image2.Picture"])
                    {
                        if (img.Parent == null)
                        {
                            parant.Child = img;
                        }
                        else
                        {
                            img = (System.Windows.Controls.Image)GraphCore.CommUIElements["Image3.Picture"];
                            parant.Child = img;
                        }
                    }
                }
                //var bitmap = new BitmapImage();
                //bitmap.BeginInit();
                //stream.Seek(0, SeekOrigin.Begin);
                //bitmap.StreamSource = stream;
                //bitmap.CacheOption = BitmapCacheOption.OnLoad;
                //bitmap.EndInit();
                img.Source = new BitmapImage(new Uri(Path));
                parant.Tag = this;
            }
            Task.Run(() =>
            {
                Thread.Sleep(Length);
                if (IsLoop && PlayState)
                {
                    Run(parant, EndAction);
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

        public void WaitForReadyRun(Border parant, Action EndAction = null) => Run(parant, EndAction);
    }
}
