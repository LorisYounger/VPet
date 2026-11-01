using LinePutScript;
using SkiaSharp;
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

        public TaskControl? Control { get; set; }

        /// <summary>
        /// 图片资源
        /// </summary>
        public string Path { get; set; } = "";
        private GraphCore GraphCore;
        private BitmapSource? SpriteSheetSource;
        private Int32Rect[]? FrameRects;
        private readonly object SpriteSheetLock = new object();
        private BitmapSource? CurrentFrame;
        private int CurrentFrameIndex = -1;
        private volatile bool disposed;
        private int FrameWidth;
        private int FrameHeight;
        public long LastUseTimeTicks = DateTime.UtcNow.Ticks;


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
            GraphInfo = graphinfo;
            GraphCore = graphCore;
            if (!GraphCore.CommConfig.ContainsKey("PA_Setup"))
            {
                GraphCore.CommConfig["PA_Setup"] = true;
                GraphCore.Dispatcher.Invoke(() =>
                {
                    GraphCore.CommUIElements["Image1.PNGAnimation"] = new System.Windows.Controls.Image() { Height = 500 };
                    GraphCore.CommUIElements["Image2.PNGAnimation"] = new System.Windows.Controls.Image() { Height = 500 };
                    GraphCore.CommUIElements["Image3.PNGAnimation"] = new System.Windows.Controls.Image() { Height = 500 }; // 多整个, 防止动画闪烁
                });
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

        /// <summary>
        /// 最大同时加载数
        /// </summary>
        public static int MaxLoadMemory = 2000;

        private async Task startup(string path, FileInfo[] paths)
        {
            while (Function.MemoryUsage() > MaxLoadMemory)
            {
                await Task.Delay(100);
            }
            try
            {
                //新方法:加载大图片
                //生成大文件加载非常慢,先看看有没有缓存能用
                Path = System.IO.Path.Combine(GraphCore.CachePath, $"{GraphCore!.Resolution}_{Math.Abs(Sub.GetHashCode(path))}_{paths.Length}.png");
                var sem = GraphCore.SpriteSheetBuildLocks.GetOrAdd(Path, _ => new SemaphoreSlim(1, 1));
                await sem.WaitAsync();
                try
                {
                    if (!File.Exists(Path))
                    {
                        int w = 0;
                        int h = 0;
                        // Load the first image
                        using (var firstImage = SKBitmap.Decode(paths[0].FullName))
                        {
                            w = firstImage.Width;
                            h = firstImage.Height;

                            // Adjust width and height based on resolution
                            if (w > GraphCore.Resolution)
                            {
                                w = GraphCore.Resolution;
                                h = (int)(h * (GraphCore.Resolution / (double)firstImage.Width));
                            }

                            if (paths.Length * w >= 60000)
                            {//修复大长动画导致过长分辨率导致可能的报错
                                w = 60000 / paths.Length;
                                h = (int)(firstImage.Height * (w / (double)firstImage.Width));
                            }
                        }

                        FrameWidth = w;
                        FrameHeight = h;

                        // Create a new bitmap to draw on
                        using (var combinedBitmap = new SKBitmap(w * paths.Length, h))
                        using (var canvas = new SKCanvas(combinedBitmap))
                        {
                            // Draw the first image
                            using (var firstImage = SKBitmap.Decode(paths[0].FullName))
                            {
                                canvas.DrawBitmap(firstImage, new SKRect(0, 0, w, h));
                            }

                            // Create an array to hold bitmaps for the remaining images
                            SKBitmap[] bitmaps = new SKBitmap[paths.Length - 1];

                            // Load and draw remaining images in parallel
                            Parallel.For(1, paths.Length, i =>
                            {
                                var img = SKBitmap.Decode(paths[i].FullName);
                                bitmaps[i - 1] = img; // Store the bitmap in the array
                            });

                            // Now draw the bitmaps onto the combined canvas
                            for (int i = 0; i < bitmaps.Length; i++)
                            {
                                canvas.DrawBitmap(bitmaps[i], new SKRect(w * (i + 1), 0, w * (i + 2), h));
                                bitmaps[i]?.Dispose();
                            }

                            // Save the combined image to the cache path
                            using (var image = SKImage.FromBitmap(combinedBitmap))
                            using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                            using (var stream = File.OpenWrite(Path))
                            {
                                data.SaveTo(stream);
                            }
                        }
                    }
                }
                finally
                {
                    sem.Release();
                }
                if (FrameWidth == 0 || FrameHeight == 0)
                {
                    using (var firstImage = SKBitmap.Decode(paths[0].FullName))
                    {
                        FrameWidth = firstImage.Width;
                        FrameHeight = firstImage.Height;
                        if (FrameWidth > GraphCore.Resolution)
                        {
                            FrameWidth = GraphCore.Resolution;
                            FrameHeight = (int)(FrameHeight * (GraphCore.Resolution / (double)firstImage.Width));
                        }
                        if (paths.Length * FrameWidth >= 60000)
                        {
                            FrameWidth = 60000 / paths.Length;
                            FrameHeight = (int)(firstImage.Height * (FrameWidth / (double)firstImage.Width));
                        }
                    }
                }

                FrameRects = new Int32Rect[paths.Length];

                for (int i = 0; i < paths.Length; i++)
                {
                    FrameRects[i] = new Int32Rect(FrameWidth * i, 0, FrameWidth, FrameHeight);
                    var noExtFileName = System.IO.Path.GetFileNameWithoutExtension(paths[i].Name);
                    int time = int.Parse(noExtFileName.Substring(noExtFileName.LastIndexOf('_') + 1));
                    Animations.Add(new Animation(this, time, i));
                }
                IsReady = true;
            }
            catch (Exception e)
            {
                IsFail = true;
                FailMessage = $"--PNGAnimation--{GraphInfo}--\nPath: {path}\n{e.Message}";
            }
        }

        /// <summary>
        /// 单帧动画
        /// </summary>
        public class Animation
        {
            private PNGAnimation parent;
            public int FrameIndex;
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
            public Animation(PNGAnimation parent, int time, int frameIndex)//, Action hidden)
            {
                this.parent = parent;
                Time = time;
                //Visible = visible;
                //Hidden = hidden;
                FrameIndex = frameIndex;
            }
            /// <summary>
            /// 运行该图层
            /// </summary>
            /// <param name="Control">动画控制</param>
            /// <param name="This">显示的图层</param>
            public void Run(FrameworkElement This, TaskControl Control)
            {
                int index = FrameIndex;
                while (!parent.disposed && ReferenceEquals(parent.Control, Control) && Control.PlayState)
                {
                    var animation = parent.Animations[index];
                    var frameSource = parent.GetFrameSource(animation.FrameIndex);
                    This.Dispatcher.Invoke(() =>
                    {
                        if (parent.disposed || !ReferenceEquals(parent.Control, Control) || !Control.PlayState)
                            return;
                        if (This is System.Windows.Controls.Image image)
                            image.Source = frameSource;
                        This.Margin = new Thickness(0);
                    });
                    Thread.Sleep(animation.Time);
                    if (!Control.PlayState) break;
                    if (++index < parent.Animations.Count) continue;
                    if (parent.IsLoop) index = 0;
                    else if (Control.Type == TaskControl.ControlType.Continue)
                    {
                        Control.Type = TaskControl.ControlType.Status_Quo;
                        index = 0;
                    }
                    else break;
                }
                Control.Type = TaskControl.ControlType.Status_Stoped;
                if (!parent.disposed && ReferenceEquals(parent.Control, Control))
                {
                    lock (parent.SpriteSheetLock)
                    {
                        // The displayed Image keeps its last frame; stopped animations
                        // need not retain another reference to the entire sprite sheet.
                        parent.CurrentFrame = null;
                        parent.CurrentFrameIndex = -1;
                        parent.SpriteSheetSource = null;
                    }
                    var endAction = Control.EndAction;
                    Control.EndAction = null;
                    endAction?.Invoke();
                }
            }
        }
        /// <summary>
        /// 从0开始运行该动画
        /// </summary>
        public void Run(Decorator parant, Action? EndAction = null)
        {
            if (disposed) return;
            Touch();
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
            var NEWControl = new TaskControl(EndAction);
            Control = NEWControl;
            parant.Dispatcher.Invoke(() =>
            {
                if (disposed) return;
                if (parant.Tag == this)
                {
                    var target = (System.Windows.Controls.Image)parant.Child;
                    Task.Run(() => Animations[0].Run(target, NEWControl));
                    return;
                }
                System.Windows.Controls.Image img;

                if (parant.Child == GraphCore!.CommUIElements["Image1.PNGAnimation"])
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
                    if (!ReferenceEquals(parant.Child, img))
                    {
                        if (img.Parent is not null)
                        {
                            img = (System.Windows.Controls.Image)GraphCore.CommUIElements["Image1.PNGAnimation"];
                        }

                        if (!ReferenceEquals(parant.Child, img))
                        {
                            if (img.Parent is Decorator oldParent)
                                oldParent.Child = null;
                            parant.Child = img;
                        }
                    }
                }
                parant.Tag = this;
                img.Tag = this;
                img.Source = GetFrameSource(0);
                img.Width = 500;
                Task.Run(() => Animations[0].Run(img, NEWControl));
            });
        }
        /// <summary>
        /// 指定图像图像控件准备运行该动画
        /// </summary>
        /// <param name="img">用于显示的Image</param>
        /// <param name="EndAction">结束动画</param>
        /// <returns>准备好的线程</returns>
        public Task Run(System.Windows.Controls.Image img, Action? EndAction = null)
        {
            if (disposed) return new Task(() => { });
            Touch();
            if (!IsReady)
            {
                EndAction?.Invoke();
                return new Task(() => { });
            }
            if (Control?.PlayState == true)
            {//如果当前正在运行,重置状态
                Control.EndAction = null;
                Control.Type = TaskControl.ControlType.Stop;
            }
            var newControl = new TaskControl(EndAction);
            Control = newControl;
            img.Dispatcher.Invoke(() =>
            {
                if (disposed || img.Tag == this) return;
                img.Tag = this;
                img.Source = GetFrameSource(0);
                img.Width = 500;
            });
            return new Task(() => Animations[0].Run(img, newControl));
        }

        private BitmapSource? GetFrameSource(int frameIndex)
        {
            lock (SpriteSheetLock)
            {
                if (disposed || FrameRects == null)
                    return null;
                Touch();
                if (CurrentFrameIndex == frameIndex)
                    return CurrentFrame;
                if (SpriteSheetSource == null)
                {
                    var sheet = new BitmapImage();
                    sheet.BeginInit();
                    sheet.CacheOption = BitmapCacheOption.OnDemand;
                    sheet.CreateOptions = BitmapCreateOptions.DelayCreation;
                    sheet.UriSource = new Uri(Path);
                    sheet.EndInit();
                    sheet.Freeze();
                    SpriteSheetSource = sheet;
                }
                var frame = new CroppedBitmap(SpriteSheetSource, FrameRects[frameIndex]);
                frame.Freeze();
                CurrentFrame = frame;
                CurrentFrameIndex = frameIndex;
                return frame;
            }
        }

        public void Touch() => Interlocked.Exchange(ref LastUseTimeTicks, DateTime.UtcNow.Ticks);


        public void CleanupIdleCache(long cleanTicks)
        {
            lock (SpriteSheetLock)
            {
                if (Control?.PlayState == true || cleanTicks < Interlocked.Read(ref LastUseTimeTicks))
                    return;
                CurrentFrame = null;
                CurrentFrameIndex = -1;
                SpriteSheetSource = null;
            }
        }

        public void Dispose()
        {
            disposed = true;
            Control?.Stop();
            lock (SpriteSheetLock)
            {
                IsReady = false;
                CurrentFrame = null;
                CurrentFrameIndex = -1;
                SpriteSheetSource = null;
                FrameRects = null;
            }
            // Playback may still be leaving its final sleep. Keep its small frame
            // metadata intact until that task exits; release all cached pixels now.
        }
    }
}
