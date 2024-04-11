using LinePutScript;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static VPet_Simulator.Core.IGraph;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// 食物动画 支持显示前中后3层夹心动画
    /// 不一定只用于食物,只是叫这个名字
    /// </summary>
    public class FoodAnimation : IRunImage
    {
        /// <summary>
        /// 创建食物动画 第二层夹心为运行时提供
        /// </summary>
        /// <param name="graphCore">动画核心</param>
        /// <param name="graphinfo">动画信息</param>
        /// <param name="front_Lay">前层 动画名</param>
        /// <param name="back_Lay">后层 动画名</param>
        /// <param name="animations">中间层运动轨迹</param>
        /// <param name="isLoop">是否循环</param>
        public FoodAnimation(GraphCore graphCore, GraphInfo graphinfo, string front_Lay,
            string back_Lay, ILine animations, bool isLoop = false)
        {
            IsLoop = isLoop;
            GraphInfo = graphinfo;
            GraphCore = graphCore;
            Front_Lay = front_Lay;
            Back_Lay = back_Lay;
            Animations = new List<Animation>();
            int i = 0;
            ISub sub = animations.Find("a" + i);
            while (sub != null)
            {
                Animations.Add(new Animation(this, sub));
                sub = animations.Find("a" + ++i);
            }
        }

        public static void LoadGraph(GraphCore graph, FileSystemInfo path, ILine info)
        {
            bool isLoop = info[(gbol)"loop"];
            FoodAnimation pa = new FoodAnimation(graph, new GraphInfo(path, info), info[(gstr)"front_lay"], info[(gstr)"back_lay"], info, isLoop);
            graph.AddGraph(pa);
        }
        /// <summary>
        /// 前层名字
        /// </summary>
        public string Front_Lay;
        /// <summary>
        /// 后层名字
        /// </summary>
        public string Back_Lay;
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
        /// 是否继续播放
        /// </summary>
        public bool IsContinue { get; set; } = false;
        /// <summary>
        /// 动画信息
        /// </summary>
        public GraphInfo GraphInfo { get; private set; }
        /// <summary>
        /// 是否准备完成
        /// </summary>
        public bool IsReady => true;
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
        /// 单帧动画
        /// </summary>
        public class Animation
        {
            private FoodAnimation parent;
            public Thickness MarginWI;
            public double Rotate = 0;
            public double Opacity = 1;
            public bool IsVisiable = true;
            public double Width;
            /// <summary>
            /// 帧时间
            /// </summary>
            public int Time;
            /// <summary>
            /// 创建单帧动画
            /// </summary>
            /// <param name="parent">FoodAnimation</param>
            /// <param name="time">持续时间</param>
            /// <param name="wx"></param>
            public Animation(FoodAnimation parent, int time, Thickness wx, double width, double rotate = 0, bool isVisiable = true, double opacity = 1)
            {
                this.parent = parent;
                Time = time;
                MarginWI = wx;
                Rotate = rotate;
                IsVisiable = isVisiable;
                Width = width;
                Opacity = opacity;
            }
            /// <summary>
            /// 创建单帧动画
            /// </summary>
            public Animation(FoodAnimation parent, ISub sub)
            {
                this.parent = parent;
                var strs = sub.GetInfos();
                Time = int.Parse(strs[0]);//0: Time
                if (strs.Length == 1)
                    IsVisiable = false;
                else
                {//1,2: Margin X,Y
                    Width = double.Parse(strs[3]);//3:Width
                    MarginWI = new Thickness(double.Parse(strs[1]), double.Parse(strs[2]), 0, 0);
                    if (strs.Length > 4)
                    {
                        Rotate = double.Parse(strs[4]);//Rotate
                        if (strs.Length > 5)
                            Opacity = double.Parse(strs[5]);//Opacity
                    }
                }
            }
            /// <summary>
            /// 运行该图层
            /// </summary>
            public void Run(FrameworkElement This, Action EndAction = null)
            {
                //先显示该图层
                This.Dispatcher.Invoke(() =>
                {
                    if (IsVisiable)
                    {
                        This.Visibility = Visibility.Visible;
                        This.Margin = MarginWI;
                        This.LayoutTransform = new RotateTransform(Rotate);
                        This.Opacity = Opacity;
                        This.Width = Width;
                        This.Height = Width;
                    }
                    else
                    {
                        This.Visibility = Visibility.Collapsed;
                    }

                });
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
                            //等待其他两个动画完成
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
        public static FoodAnimatGrid FoodGrid = new FoodAnimatGrid();
        public class FoodAnimatGrid : Grid
        {
            public FoodAnimatGrid()
            {
                Width = 500;
                Height = 500;
                VerticalAlignment = VerticalAlignment.Top;
                HorizontalAlignment = HorizontalAlignment.Left;
                Front = new Image();
                Back = new Image();
                Food = new Image
                {
                    RenderTransformOrigin = new Point(0.5, 0.5),
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Visibility = Visibility.Collapsed,
                };
                this.Children.Add(Back);
                this.Children.Add(Food);
                this.Children.Add(Front);
            }
            public Image Front;
            public Image Food;
            public Image Back;
        }

        public void Run(Decorator parant, Action EndAction = null) => Run(parant, null, EndAction);

        public void Run(Decorator parant, ImageSource image, Action EndAction = null)
        {
            if (PlayState)
            {//如果当前正在运行,重置状态
             //IsResetPlay = true;
                StopAction = () => Run(parant, image, EndAction);
                return;
            }
            nowid = 0;
            PlayState = true;
            DoEndAction = true;
            parant.Dispatcher.Invoke(() =>
            {
                parant.Tag = this;
                if (parant.Child != FoodGrid)
                {
                    if (FoodGrid.Parent != null)
                    {
                        ((Decorator)FoodGrid.Parent).Child = null;
                    }
                    parant.Child = FoodGrid;
                }
                var FL = GraphCore.FindGraph(Front_Lay, GraphInfo.Animat, GraphInfo.ModeType);
                var BL = GraphCore.FindGraph(Back_Lay, GraphInfo.Animat, GraphInfo.ModeType);
                var t1 = FL?.Run(FoodGrid.Front);
                var t2 = BL?.Run(FoodGrid.Back);
                if (FoodGrid.Food.Source != image)
                {
                    if (FoodGrid.Food.Source is BitmapImage bitmapImage)
                    {//内存回收
                        bitmapImage.StreamSource?.Dispose();
                    }
                    FoodGrid.Food.Source = image;
                }
                t1?.Start();
                t2?.Start();
                Task.Run(() => Animations[0].Run(FoodGrid.Food, EndAction));
            });
        }

        public void Stop(bool StopEndAction = false)
        {
            DoEndAction = !StopEndAction;
            PlayState = false;
        }
    }
}
