using LinePutScript;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using static VPet_Simulator.Core.IGraph;
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
        /// 是否循环播放
        /// </summary>
        public bool IsLoop { get; set; }

        /// <summary>
        /// 动画信息
        /// </summary>
        public GraphInfo GraphInfo { get; private set; }

        /// <summary>
        /// 是否准备完成
        /// </summary>
        public bool IsReady { get; private set; } = false;

        public TaskControl Control { get; set; }

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

        public bool IsFail { get; set; } = false;

        public string FailMessage { get; set; } = "";
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
                GraphCore.CommUIElements["Image3.PNGAnimation"] = new System.Windows.Controls.Image() { Height = 500 }; // 多整个, 防止动画闪烁
            }
            Task.Run(() => startup(path, paths));
        }

        public static void LoadGraph(GraphCore graph, FileSystemInfo path, ILine info)
        {
            if (!(path is DirectoryInfo p))
            {
                Picture.LoadGraph(graph, path, info);
                return;
            }
            var paths = p.GetFiles("*.png");
            if (paths.Length == 0)
            {
                return;
            }
            else if (paths.Length == 1)
            {
                Picture.LoadGraph(graph, paths[0], info);
                return;
            }

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
            try
            {
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
            }
            catch (Exception e)
            {
                IsFail = true;
                FailMessage = $"--PNGAnimation--{GraphInfo}--\nPath: {path}\n{e.Message}";
            }
            finally
            {
                Interlocked.Decrement(ref NowLoading);
            }
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
            /// <param name="Control">动画控制</param>
            /// <param name="This">显示的图层</param>
            public void Run(FrameworkElement This, TaskControl Control)
            {
                //先显示该图层
                This.Dispatcher.Invoke(() => This.Margin = new Thickness(MarginWIX, 0, 0, 0));
                //然后等待帧时间毫秒
                Thread.Sleep(Time);
                //判断是否要下一步
                switch (Control.Type)
                {
                    case TaskControl.ControlType.Stop:
                        Control.EndAction?.Invoke();
                        return;
                    case TaskControl.ControlType.Status_Stoped:
                        return;
                    case TaskControl.ControlType.Status_Quo:
                    case TaskControl.ControlType.Continue:
                        if (++parent.nowid >= parent.Animations.Count)
                            if (parent.IsLoop)
                            {
                                parent.nowid = 0;
                                //让循环动画重新开始立线程,不stackoverflow
                                Task.Run(() => parent.Animations[0].Run(This, Control));
                                return;
                            }
                            else if (Control.Type == TaskControl.ControlType.Continue)
                            {
                                Control.Type = TaskControl.ControlType.Status_Quo;
                                parent.nowid = 0;
                            }
                            else
                            {
                                Control.Type = TaskControl.ControlType.Status_Stoped;
                                Control.EndAction?.Invoke(); //运行结束动画时事件                                
                                return;
                            }
                        //要下一步
                        parent.Animations[parent.nowid].Run(This, Control);
                        return;
                }
            }
        }
        /// <summary>
        /// 从0开始运行该动画
        /// </summary>
        public void Run(Decorator parant, Action EndAction = null)
        {
            if (!IsReady)
            {
                EndAction?.Invoke();
                return;
            }
            if (Control?.PlayState == true)
            {//如果当前正在运行,重置状态
                Control.Stop(() => Run(parant, EndAction));
                return;
            }
            nowid = 0;
            var NEWControl = new TaskControl(EndAction);
            Control = NEWControl;            
            parant.Dispatcher.Invoke(() =>
            {
                if (parant.Tag == this)
                {
                    Task.Run(() => Animations[0].Run((System.Windows.Controls.Image)parant.Child, NEWControl));
                    return;
                }
                System.Windows.Controls.Image img;

                if (parant.Child == GraphCore.CommUIElements["Image1.PNGAnimation"])
                {
                    img = (System.Windows.Controls.Image)GraphCore.CommUIElements["Image1.PNGAnimation"];
                }
                else if (parant.Child == GraphCore.CommUIElements["Image3.PNGAnimation"])
                {
                    img = (System.Windows.Controls.Image)GraphCore.CommUIElements["Image3.PNGAnimation"];
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
                img.Source = new BitmapImage(new Uri(Path));
                img.Width = Width;
                Task.Run(() => Animations[0].Run((System.Windows.Controls.Image)parant.Child, NEWControl));
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
            if (!IsReady)
            {
                EndAction?.Invoke();
                return Task.CompletedTask;
            }
            if (Control?.PlayState == true)
            {//如果当前正在运行,重置状态
                Control.EndAction = null;
                Control.Type = TaskControl.ControlType.Stop;
            }
            nowid = 0;
            Control = new TaskControl(EndAction);
            return img.Dispatcher.Invoke(() =>
            {
                if (img.Tag == this)
                {
                    return new Task(() => Animations[0].Run(img, Control));
                }
                img.Tag = this;
                img.Source = new BitmapImage(new Uri(Path));
                img.Width = Width;
                return new Task(() => Animations[0].Run(img, Control));
            });
        }
    }
}
