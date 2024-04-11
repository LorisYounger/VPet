using LinePutScript;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using static VPet_Simulator.Core.Picture;


namespace VPet_Simulator.Core
{
    /// <summary>
    /// Picture.xaml 的交互逻辑
    /// </summary>
    public class Picture : IImageRun
    {
        /// <summary>
        /// 新建新静态图像
        /// </summary>
        /// <param name="path">图片路径</param>
        public Picture(GraphCore graphCore, string path, GraphInfo graphinfo, int length = 1000, bool isloop = false)
        {
            GraphInfo = graphinfo;
            IsLoop = isloop;
            Length = length;
            GraphCore = graphCore;
            Path = path;
            if (!GraphCore.CommConfig.ContainsKey("PIC_Setup"))
            {
                GraphCore.CommConfig["PIC_Setup"] = true;
                GraphCore.CommUIElements["Image1.Picture"] = new Image() { Width = 500, Height = 500 };
                GraphCore.CommUIElements["Image2.Picture"] = new Image() { Width = 500, Height = 500 };
            }
        }
        public static void LoadGraph(GraphCore graph, FileSystemInfo path, ILine info)
        {
            if (!(path is FileInfo))
            {
                PNGAnimation.LoadGraph(graph, path, info);
                return;
            }
            int length = info.GetInt("length");
            if (length == 0)
            {
                if (!int.TryParse(path.Name.Split('.').Reverse().ToArray()[1].Split('_').Last(), out length))
                    length = 1000;
            }
            bool isLoop = info[(gbol)"loop"];
            Picture pa = new Picture(graph, path.FullName, new GraphInfo(path, info), length, isLoop);
            graph.AddGraph(pa);
        }
        /// <summary>
        /// 图片资源
        /// </summary>
        public string Path;
        private GraphCore GraphCore;
        public bool PlayState { get; set; }
        public bool IsLoop { get; set; }
        public int Length { get; set; }
        //public bool StoreMemory => true;//经过测试,储存到内存好处多多,不储存也要占用很多内存,干脆存了吧
        public bool IsContinue { get; set; }

        /// <summary>
        /// 动画信息
        /// </summary>
        public GraphInfo GraphInfo { get; private set; }

        public bool IsReady => true;

        public void Run(Decorator parant, Action EndAction = null)
        {
            if (PlayState)
            {
                IsContinue = true;
                return;
            }
            PlayState = true;
            DoEndAction = true;
            parant.Dispatcher.Invoke(() =>
            {
                if (parant.Tag != this)
                {
                    Image img;
                    if (parant.Child == GraphCore.CommUIElements["Image1.Picture"])
                    {
                        img = (Image)GraphCore.CommUIElements["Image1.Picture"];
                    }
                    else
                    {
                        img = (Image)GraphCore.CommUIElements["Image2.Picture"];
                        if (parant.Child != img)
                        {
                            if (img.Parent == null)
                            {
                                parant.Child = null;
                                parant.Child = img;
                            }
                            else
                            {
                                img = (Image)GraphCore.CommUIElements["Image1.Picture"];
                                if (img.Parent != null)
                                    ((Decorator)img.Parent).Child = null;
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
                    img.Width = 500;
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
                        if (DoEndAction)
                            EndAction?.Invoke();//运行结束动画时事件
                    }
                });
            });
        }
        bool DoEndAction = true;
        public void Stop(bool StopEndAction = false)
        {
            PlayState = false;
            this.DoEndAction = !StopEndAction;
        }
        public Task Run(System.Windows.Controls.Image img, Action EndAction = null)
        {
            PlayState = true;
            DoEndAction = true;
            return img.Dispatcher.Invoke(() =>
            {
                if (img.Tag == this)
                {
                    return new Task(() =>
                    {
                        Thread.Sleep(Length);
                        if (IsLoop && PlayState)
                        {
                            Run(img, EndAction);
                        }
                        else
                        {
                            PlayState = false;
                            if (DoEndAction)
                                EndAction?.Invoke();//运行结束动画时事件
                        }
                    });
                }
                img.Tag = this;
                img.Source = new BitmapImage(new Uri(Path));
                img.Width = 500;
                return new Task(() =>
                {
                    Thread.Sleep(Length);
                    if (IsLoop && PlayState)
                    {
                        Run(img, EndAction);
                    }
                    else
                    {
                        PlayState = false;
                        if (DoEndAction)
                            EndAction?.Invoke();//运行结束动画时事件
                    }
                });
            });
        }
        public interface IImageRun : IGraph
        {
            /// <summary>
            /// 指定图像图像控件准备运行该动画
            /// </summary>
            /// <param name="img">用于显示的Image</param>
            /// <param name="EndAction">结束动画</param>
            /// <returns>准备好的线程</returns>
            Task Run(System.Windows.Controls.Image img, Action EndAction = null);
        }
    }


}
