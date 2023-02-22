using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows;
using System.Windows.Threading;
using System.Threading;
using System.Drawing;
using LinePutScript;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TreeView;
using Panuon.WPF.UI;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// PNGAnimation.xaml 的交互逻辑
    /// </summary>
    public partial class PNGAnimation : System.Windows.Controls.Image, IGraph
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
        ///// <summary>
        ///// 是否重置状态从0开始播放
        ///// </summary>
        //public bool IsResetPlay { get; set; } = false;
        ///// <summary>//经过测试,储存到内存好处多多,不储存也要占用很多内存,干脆存了吧
        ///// 是否储存到内存以支持快速显示
        ///// </summary>
        //public bool StoreMemory { get; private set; }
        public UIElement This => this;

        public Save.ModeType ModeType { get; private set; }

        public GraphCore.GraphType GraphType { get; private set; }
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
        /// 新建 PNG 动画
        /// </summary>
        /// <param name="path">文件夹位置</param>
        /// <param name="paths">文件内容列表</param>
        /// <param name="isLoop">是否循环</param>
        public PNGAnimation(string path, FileInfo[] paths, Save.ModeType modetype, GraphCore.GraphType graphtype, bool isLoop = false)
        {
            InitializeComponent();
            Animations = new List<Animation>();
            IsLoop = isLoop;
            //StoreMemory = storemem;
            GraphType = graphtype;
            ModeType = modetype;
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
        private void startup(string path, FileInfo[] paths)
        {
            //新方法:加载大图片
            //生成大文件加载非常慢,先看看有没有缓存能用
            string cp = GraphCore.CachePath + $"\\{Sub.GetHashCode(path)}_{paths.Length}.png";
            Dispatcher.Invoke(() => Width = 500 * (paths.Length + 1));
            if (File.Exists(cp))
            {
                for (int i = 0; i < paths.Length; i++)
                {
                    FileInfo file = paths[i];
                    int time = int.Parse(file.Name.Split('.').Reverse().ToArray()[1].Split('_').Last());
                    int wxi = -500 * i;
                    Animations.Add(new Animation(this, time, () => Margin = new Thickness(wxi, 0, 0, 0)));
                }
                Dispatcher.Invoke(() => Source = new BitmapImage(new Uri(cp)));
                IsReady = true;
            }
            else
            {
                List<System.Drawing.Image> imgs = new List<System.Drawing.Image>();
                foreach (var file in paths)
                    imgs.Add(System.Drawing.Image.FromFile(file.FullName));
                int w = imgs[0].Width;
                int h = imgs[0].Height;
                Bitmap joinedBitmap = new Bitmap(w * paths.Length, h);
                var graph = Graphics.FromImage(joinedBitmap);
                for (int i = 0; i < paths.Length; i++)
                {
                    FileInfo file = paths[i];
                    int time = int.Parse(file.Name.Split('.').Reverse().ToArray()[1].Split('_').Last());
                    graph.DrawImage(imgs[i], w * i, 0, w, h);
                    int wxi = -500 * i;
                    Animations.Add(new Animation(this, time, () => Margin = new Thickness(wxi, 0, 0, 0)));
                }
                joinedBitmap.Save(cp);
                graph.Dispose();
                joinedBitmap.Dispose();
                imgs.ForEach(x => x.Dispose());
                Dispatcher.Invoke(() => Source = new BitmapImage(new Uri(cp)));
                IsReady = true;
            }
        }

        /// <summary>
        /// 单帧动画
        /// </summary>
        public class Animation
        {
            private PNGAnimation parent;
            /// <summary>
            /// 显示
            /// </summary>
            public Action Visible;
            ///// <summary>
            ///// 隐藏
            ///// </summary>
            //public Action Hidden;
            /// <summary>
            /// 帧时间
            /// </summary>
            public int Time;
            public Animation(PNGAnimation parent, int time, Action visible)//, Action hidden)
            {
                this.parent = parent;
                Time = time;
                Visible = visible;
                //Hidden = hidden;
            }
            /// <summary>
            /// 运行该图层
            /// </summary>
            public void Run(Action EndAction = null)
            {
                //先显示该图层
                parent.Dispatcher.Invoke(Visible);
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
                    parent.Animations[parent.nowid].Run(EndAction);
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
        public void Run(Action EndAction = null)
        {
            //if(endwilldo != null && nowid != Animations.Count)
            //{
            //    endwilldo.Invoke();
            //    endwilldo = null;
            //}
            if (PlayState)
            {//如果当前正在运行,重置状态
                //IsResetPlay = true;
                Stop(true);
                StopAction = () => Run(EndAction);
                return;
            }
            nowid = 0;
            PlayState = true;
            DoEndAction = true;
            new Thread(() => Animations[0].Run(EndAction)).Start();
        }

        public void Stop(bool StopEndAction = false)
        {
            DoEndAction = !StopEndAction;
            PlayState = false;
            //IsResetPlay = false;
        }

        public void WaitForReadyRun(Action EndAction = null)
        {
            Task.Run(() =>
            {
                while (!IsReady)
                {
                    Thread.Sleep(100);
                }
                Run(EndAction);
            });
        }
    }
}
