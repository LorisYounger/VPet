using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Threading;
using System.Drawing;
using LinePutScript;
using static VPet_Simulator.Core.GraphCore;
using static VPet_Simulator.Core.Picture;

namespace VPet_Simulator.Core
{

    /// <summary>
    /// PNGAnimation.xaml 的交互逻辑
    /// </summary>
    public partial class PNGAnimation : IImageRun
    {
        /// <summary>
        /// 所有动画帧
        /// </summary>
        public List<Animation> Animations;
        /// <summary>
        /// 当前动画播放状态
        /// </summary>
        public bool PlayState { get; set; } = false;
        /// <summary>
        /// 当前动画是否执行ENDACTION
        /// </summary>
        private bool DoEndAction = true;
        /// <summary>
        /// 是否循环播放
        /// </summary>
        public bool IsLoop { get; set; }
        /// <summary>
        /// 是否循环播放
        /// </summary>
        public bool IsContinue { get; set; } = false;

        /// <summary>
        /// 动画信息
        /// </summary>
        public GraphInfo GraphInfo { get; private set; }

        /// <summary>
        /// 是否准备完成
        /// </summary>
        public bool IsReady { get; private set; } = false;
        /// <summary>
        /// 动画停止时运行的方法
        /// </summary>
        private Action StopAction;
        int nowid;
        /// <summary>
        /// 图片资源
        /// </summary>
        public string Path;
        private GraphCore GraphCore;
        /// <summary>
        /// 反正一次性生成太多导致闪退
        /// </summary>
        public static int NowLoading = 0;
        /// <summary>
        /// 新建 PNG 动画
        /// </summary>
        /// <param name="path">文件夹位置</param>
        /// <param name="paths">文件内容列表</param>
        /// <param name="isLoop">是否循环</param>
        public PNGAnimation(GraphCore graphCore, string path, FileInfo[] paths, GraphInfo graphinfo, bool isLoop = false)
        {
            Animations = new List<Animation>();
            IsLoop = isLoop;
            //StoreMemory = storemem;
            GraphInfo = graphinfo;
            GraphCore = graphCore;
            if (!GraphCore.CommConfig.ContainsKey("PA_Setup"))
            {
                GraphCore.CommConfig["PA_Setup"] = true;
                GraphCore.CommUIElements["Image1.PNGAnimation"] = new System.Windows.Controls.Image() { Height = 500 };
                GraphCore.CommUIElements["Image2.PNGAnimation"] = new System.Windows.Controls.Image() { Height = 500 };
            }
            Task.Run(() => startup(path, paths));
            //if (storemem)
            //foreach (var file in paths)
            //{
            //    int time = int.Parse(file.Name.Split('.').Reverse().ToArray()[1].Split('_').Last());
            //    var img = new Image()
            //    {
            //        Source = new BitmapImage(new Uri(file.FullName)),
            //        Visibility = Visibility.Hidden
            //    };
            //    MainGrid.Children.Add(img);
            //    Animations.Add(new Animation(this, time, () => img.Visibility = Visibility.Visible, () => img.Visibility = Visibility.Hidden));
            //}
            //else
            //{
            //    Image[] imgs = new Image[3];
            //    imgs[0] = new Image()
            //    {
            //        Visibility = Visibility.Hidden
            //    };
            //    imgs[1] = new Image()
            //    {
            //        Visibility = Visibility.Hidden
            //    };
            //    imgs[2] = new Image()
            //    {
            //        Visibility = Visibility.Hidden
            //    };
            //    int time = int.Parse(paths[0].Name.Split('.').Reverse().ToArray()[1].Split('_').Last());
            //    //第一张图:有专门自己的图层
            //    var img = new Image()
            //    {
            //        Source = new BitmapImage(new Uri(paths[0].FullName)),
            //        Visibility = Visibility.Hidden
            //    };
            //    MainGrid.Children.Add(img);
            //    MainGrid.Children.Add(imgs[0]);
            //    MainGrid.Children.Add(imgs[1]);
            //    MainGrid.Children.Add(imgs[2]);
            //    Animations.Add(new Animation(this, time, () =>
            //    {
            //        img.Visibility = Visibility.Visible;
            //        imgs[1].Source = new BitmapImage(new Uri(paths[1].FullName));
            //    }, () => img.Visibility = Visibility.Hidden));

            //    int last = paths.Count() - 1;
            //    for (int i = 1; i < last; i++)
            //    {
            //        time = int.Parse(paths[i].Name.Split('.').Reverse().ToArray()[1].Split('_').Last());
            //        var im1 = imgs[i % 3];
            //        var im2 = imgs[(i + 1) % 3];
            //        var st3 = paths[i + 1].FullName;
            //        Animations.Add(new Animation(this, time, () =>
            //        {
            //            im1.Visibility = Visibility.Visible;
            //            im2.Source = new BitmapImage(new Uri(st3));
            //        }, () => im1.Visibility = Visibility.Hidden));
            //    }
            //    //最后一张图: 不处理下一张图的imgsSources

            //    time = int.Parse(paths[last].Name.Split('.').Reverse().ToArray()[1].Split('_').Last());
            //    Animations.Add(new Animation(this, time, () => imgs[last % 3].Visibility = Visibility.Visible
            //    , () => imgs[last % 3].Visibility = Visibility.Hidden));
            //}
        }

        public static void LoadGraph(GraphCore graph, FileSystemInfo path, ILine info)
        {
            if (!(path is DirectoryInfo p))
            {
                Picture.LoadGraph(graph, path, info);
                return;
            }
            var paths = p.GetFiles();

            bool isLoop = info[(gbol)"loop"];
            PNGAnimation pa = new PNGAnimation(graph, path.FullName, paths, new GraphInfo(path, info), isLoop);
            graph.AddGraph(pa);
        }

        public double Width;
        /// <summary>
        /// 最大同时加载数
        /// </summary>
        public static int MaxLoadNumber = 40;

        private void startup(string path, FileInfo[] paths)
        {
            while (NowLoading > MaxLoadNumber)
            {
                Thread.Sleep(100);
            }
            Interlocked.Increment(ref NowLoading);
            //新方法:加载大图片
            //生成大文件加载非常慢,先看看有没有缓存能用
            Path = System.IO.Path.Combine(GraphCore.CachePath, $"{GraphCore.Resolution}_{Math.Abs(Sub.GetHashCode(path))}_{paths.Length}.png");
            Width = 500 * (paths.Length + 1);
            if (!File.Exists(Path) && !((List<string>)GraphCore.CommConfig["Cache"]).Contains(path))
            {
                ((List<string>)GraphCore.CommConfig["Cache"]).Add(path);
                int w = 0;
                int h = 0;
                FileInfo firstImage = paths[0];
                var img = System.Drawing.Image.FromFile(firstImage.FullName);
                w = img.Width;
                h = img.Height;
                if (w > GraphCore.Resolution)
                {
                    w = GraphCore.Resolution;
                    h = (int)(h * (GraphCore.Resolution / (double)img.Width));
                }
                if (paths.Length * w >= 60000)
                {//修复大长动画导致过长分辨率导致可能的报错
                    w = 60000 / paths.Length;
                    h = (int)(img.Height * (w / (double)img.Width));
                }

                using (Bitmap joinedBitmap = new Bitmap(w * paths.Length, h))
                using (Graphics graph = Graphics.FromImage(joinedBitmap))
                {
                    using (img)
                        graph.DrawImage(img, 0, 0, w, h);
                    Parallel.For(1, paths.Length, i =>
                    {
                        using (var img = System.Drawing.Image.FromFile(paths[i].FullName))
                        {
                            lock (graph)
                                graph.DrawImage(img, w * i, 0, w, h);
                        }
                    });
                    if (!File.Exists(Path))
                        joinedBitmap.Save(Path);
                }
            }
            for (int i = 0; i < paths.Length; i++)
            {
                var noExtFileName = System.IO.Path.GetFileNameWithoutExtension(paths[i].Name);
                int time = int.Parse(noExtFileName.Substring(noExtFileName.LastIndexOf('_') + 1));
                Animations.Add(new Animation(this, time, -500 * i));
            }
            //stream = new MemoryStream(File.ReadAllBytes(cp));
            IsReady = true;
            Interlocked.Decrement(ref NowLoading);
        }


        /// <summary>
        /// 单帧动画
        /// </summary>
        public class Animation
        {
            private PNGAnimation parent;
            public int MarginWIX;
            ///// <summary>
            ///// 显示
            ///// </summary>
            //public Action Visible;
            ///// <summary>
            ///// 隐藏
            ///// </summary>
            //public Action Hidden;
            /// <summary>
            /// 帧时间
            /// </summary>
            public int Time;
            public Animation(PNGAnimation parent, int time, int wxi)//, Action hidden)
            {
                this.parent = parent;
                Time = time;
                //Visible = visible;
                //Hidden = hidden;
                MarginWIX = wxi;
            }
            /// <summary>
            /// 运行该图层
            /// </summary>
            public void Run(FrameworkElement This, Action EndAction = null)
            {
                //先显示该图层
                This.Dispatcher.Invoke(() => This.Margin = new Thickness(MarginWIX, 0, 0, 0));
                //然后等待帧时间毫秒
                Thread.Sleep(Time);
                //判断是否要下一步
                if (parent.PlayState)
                {
                    if (++parent.nowid >= parent.Animations.Count)
                        if (parent.IsLoop)
                            parent.nowid = 0;
                        else if (parent.IsContinue)
                        {
                            parent.IsContinue = false;
                            parent.nowid = 0;
                        }
                        else
                        {
                            //parent.endwilldo = () => parent.Dispatcher.Invoke(Hidden);
                            //parent.Dispatcher.Invoke(Hidden);
                            parent.PlayState = false;
                            if (parent.DoEndAction)
                                EndAction?.Invoke();//运行结束动画时事件
                            parent.StopAction?.Invoke();
                            parent.StopAction = null;
                            ////延时隐藏
                            //Task.Run(() =>
                            //{
                            //    Thread.Sleep(25);
                            //    parent.Dispatcher.Invoke(Hidden);
                            //});
                            return;
                        }
                    //要下一步,现在就隐藏图层
                    //隐藏该图层
                    //parent.Dispatcher.Invoke(Hidden);
                    parent.Animations[parent.nowid].Run(This, EndAction);
                    return;
                }
                else
                {
                    parent.IsContinue = false;
                    //parent.Dispatcher.Invoke(Hidden);
                    if (parent.DoEndAction)
                        EndAction?.Invoke();//运行结束动画时事件
                    parent.StopAction?.Invoke();
                    parent.StopAction = null;
                    //Task.Run(() =>
                    //{
                    //    Thread.Sleep(25);
                    //    parent.Dispatcher.Invoke(Hidden);
                    //});
                }
            }
        }
        /// <summary>
        /// 从0开始运行该动画
        /// </summary>
        public void Run(Border parant, Action EndAction = null)
        {
            //if(endwilldo != null && nowid != Animations.Count)
            //{
            //    endwilldo.Invoke();
            //    endwilldo = null;
            //}
            if (!IsReady)
            {
                EndAction?.Invoke();
                return;
            }
            if (PlayState)
            {//如果当前正在运行,重置状态
             //IsResetPlay = true;
                Stop();
                StopAction = () => Run(parant, EndAction);
                return;
            }
            nowid = 0;
            PlayState = true;
            DoEndAction = true;
            parant.Dispatcher.Invoke(() =>
            {
                if (parant.Tag == this)
                {
                    Task.Run(() => Animations[0].Run((System.Windows.Controls.Image)parant.Child, EndAction));
                    return;
                }
                System.Windows.Controls.Image img;

                if (parant.Child == GraphCore.CommUIElements["Image1.PNGAnimation"])
                {
                    img = (System.Windows.Controls.Image)GraphCore.CommUIElements["Image1.PNGAnimation"];
                }
                else
                {
                    img = (System.Windows.Controls.Image)GraphCore.CommUIElements["Image2.PNGAnimation"];
                    if (parant.Child != GraphCore.CommUIElements["Image2.PNGAnimation"])
                    {
                        if (img.Parent == null)
                        {
                            parant.Child = img;
                        }
                        else
                        {
                            img = (System.Windows.Controls.Image)GraphCore.CommUIElements["Image1.PNGAnimation"];
                            if (img.Parent != null)
                                ((Decorator)img.Parent).Child = null;
                            parant.Child = img;
                        }
                    }
                }
                parant.Tag = this;
                //var bitmap = new BitmapImage();
                //bitmap.BeginInit();
                //stream.Seek(0, SeekOrigin.Begin);
                //bitmap.StreamSource = stream;
                //bitmap.CacheOption = BitmapCacheOption.OnLoad;
                //bitmap.EndInit();
                img.Source = new BitmapImage(new Uri(Path));

                img.Width = Width;
                Task.Run(() => Animations[0].Run((System.Windows.Controls.Image)parant.Child, EndAction));
            });
        }
        /// <summary>
        /// 指定图像图像控件准备运行该动画
        /// </summary>
        /// <param name="img">用于显示的Image</param>
        /// <param name="EndAction">结束动画</param>
        /// <returns>准备好的线程</returns>
        public Task Run(System.Windows.Controls.Image img, Action EndAction = null)
        {
            nowid = 0;
            PlayState = true;
            DoEndAction = true;
            return img.Dispatcher.Invoke(() =>
              {
                  if (img.Tag == this)
                  {
                      return new Task(() => Animations[0].Run(img, EndAction));
                  }
                  img.Tag = this;
                  img.Source = new BitmapImage(new Uri(Path));
                  img.Width = Width;
                  return new Task(() => Animations[0].Run(img, EndAction));
              });
        }
        public void Stop(bool StopEndAction = false)
        {
            DoEndAction = !StopEndAction;
            PlayState = false;
            //IsResetPlay = false;
        }
    }
}
