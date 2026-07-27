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

    public partial class APNGAnimation : IImageRun
    {
        private class AnimationFrame
        {
            public BitmapSource Source;
            public int Duration;
        }

        private const int DefaultFrameDuration = 100;
        private GraphCore GraphCore;
        private readonly object FrameLock = new object();
        private List<AnimationFrame> Frames = new List<AnimationFrame>();
        private int nowid;

        public bool IsLoop { get; set; }

        public bool IsReady { get; private set; }

        public bool IsFail { get; private set; }

        public string FailMessage { get; private set; } = "";

        public GraphInfo GraphInfo { get; private set; }

        public TaskControl Control { get; private set; }

        public string Path { get; private set; }

        public long LastUseTimeTicks = DateTime.UtcNow.Ticks;

        public APNGAnimation(GraphCore graphCore, string path, GraphInfo graphinfo, bool isLoop = false)
        {
            GraphCore = graphCore;
            Path = path;
            GraphInfo = graphinfo;
            IsLoop = isLoop;
            if (!GraphCore.CommConfig.ContainsKey("APA_Setup"))
            {
                GraphCore.CommConfig["APA_Setup"] = true;
                GraphCore.Dispatcher.Invoke(() =>
                {
                    GraphCore.CommUIElements["Image1.APNGAnimation"] = new System.Windows.Controls.Image() { Height = 500 };
                    GraphCore.CommUIElements["Image2.APNGAnimation"] = new System.Windows.Controls.Image() { Height = 500 };
                    GraphCore.CommUIElements["Image3.APNGAnimation"] = new System.Windows.Controls.Image() { Height = 500 };
                });
            }
            Task.Run(startup);
        }

        public static void LoadGraph(GraphCore graph, FileSystemInfo path, ILine info)
        {
            if (!(path is FileInfo file) || path.Extension.ToLowerInvariant() != ".png")
                return;

            bool isLoop = info[(gbol)"loop"];
            APNGAnimation pa = new APNGAnimation(graph, file.FullName, new GraphInfo(path, info), isLoop);
            graph.AddGraph(pa);
        }

        private async Task startup()
        {
            while (Function.MemoryUsage() > PNGAnimation.MaxLoadMemory)
            {
                await Task.Delay(100);
            }

            try
            {
                if (!File.Exists(Path))
                    throw new FileNotFoundException($"Can not find file: {Path}");

                List<AnimationFrame> loadFrames = new List<AnimationFrame>();
                using (var stream = File.OpenRead(Path))
                using (var codec = SKCodec.Create(stream))
                {
                    if (codec == null)
                        throw new InvalidDataException("Invalid PNG/APNG file.");

                    int frameCount = Math.Max(1, codec.FrameCount);
                    var frameInfos = codec.FrameInfo;
                    var info = codec.Info;

                    int targetWidth = info.Width;
                    int targetHeight = info.Height;
                    if (targetWidth > GraphCore.Resolution)
                    {
                        targetWidth = GraphCore.Resolution;
                        targetHeight = (int)(targetHeight * (GraphCore.Resolution / (double)info.Width));
                    }

                    for (int i = 0; i < frameCount; i++)
                    {
                        using var frameBitmap = new SKBitmap(info.Width, info.Height, info.ColorType, info.AlphaType);
                        var options = new SKCodecOptions(i, i - 1);
                        var result = codec.GetPixels(info, frameBitmap.GetPixels(), options);
                        if (result != SKCodecResult.Success && result != SKCodecResult.IncompleteInput && result != SKCodecResult.ErrorInInput)
                            throw new InvalidDataException($"Decode APNG frame failed: {result}");

                        SKBitmap outputBitmap = frameBitmap;
                        if (frameBitmap.Width != targetWidth || frameBitmap.Height != targetHeight)
                        {
                            outputBitmap = new SKBitmap(targetWidth, targetHeight, frameBitmap.ColorType, frameBitmap.AlphaType);
                            frameBitmap.ScalePixels(outputBitmap, SKSamplingOptions.Default);
                        }

                        try
                        {
                            using var image = SKImage.FromBitmap(outputBitmap);
                            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                            using var memory = new MemoryStream(data.ToArray());
                            BitmapImage bitmapImage = new BitmapImage();
                            bitmapImage.BeginInit();
                            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                            bitmapImage.StreamSource = memory;
                            bitmapImage.EndInit();
                            bitmapImage.Freeze();

                            int duration = DefaultFrameDuration;
                            if (frameInfos != null && i < frameInfos.Length && frameInfos[i].Duration > 0)
                                duration = frameInfos[i].Duration;

                            loadFrames.Add(new AnimationFrame
                            {
                                Source = bitmapImage,
                                Duration = duration,
                            });
                        }
                        finally
                        {
                            if (!ReferenceEquals(outputBitmap, frameBitmap))
                                outputBitmap.Dispose();
                        }
                    }
                }

                lock (FrameLock)
                {
                    Frames = loadFrames;
                }
                IsReady = loadFrames.Count > 0;
            }
            catch (Exception e)
            {
                IsFail = true;
                FailMessage = $"--APNGAnimation--{GraphInfo}--\nPath: {Path}\n{e.Message}";
            }
        }

        private void Play(FrameworkElement element, TaskControl control)
        {
            Touch();
            while (true)
            {
                AnimationFrame frame;
                lock (FrameLock)
                {
                    if (Frames == null || Frames.Count == 0)
                    {
                        control.Type = TaskControl.ControlType.Status_Stoped;
                        control.EndAction?.Invoke();
                        return;
                    }
                    frame = Frames[nowid];
                }

                element.Dispatcher.Invoke(() =>
                {
                    if (element is System.Windows.Controls.Image image)
                    {
                        image.Source = frame.Source;
                    }
                    element.Margin = new Thickness(0, 0, 0, 0);
                });

                Thread.Sleep(frame.Duration);

                switch (control.Type)
                {
                    case TaskControl.ControlType.Stop:
                        control.Type = TaskControl.ControlType.Status_Stoped;
                        control.EndAction?.Invoke();
                        return;
                    case TaskControl.ControlType.Status_Stoped:
                        return;
                    case TaskControl.ControlType.Status_Quo:
                    case TaskControl.ControlType.Continue:
                        lock (FrameLock)
                        {
                            nowid++;
                            if (nowid >= Frames.Count)
                            {
                                if (IsLoop)
                                {
                                    nowid = 0;
                                }
                                else if (control.Type == TaskControl.ControlType.Continue)
                                {
                                    control.Type = TaskControl.ControlType.Status_Quo;
                                    nowid = 0;
                                }
                                else
                                {
                                    control.Type = TaskControl.ControlType.Status_Stoped;
                                    control.EndAction?.Invoke();
                                    return;
                                }
                            }
                        }
                        break;
                }
            }
        }

        public Task Run(System.Windows.Controls.Image img, Action EndAction = null)
        {
            Touch();
            if (!IsReady)
            {
                EndAction?.Invoke();
                return Task.CompletedTask;
            }
            if (Control?.PlayState == true)
            {
                Control.EndAction = null;
                Control.Type = TaskControl.ControlType.Stop;
            }

            nowid = 0;
            Control = new TaskControl(EndAction);
            return img.Dispatcher.Invoke(() =>
            {
                if (img.Tag != this)
                {
                    img.Tag = this;
                    lock (FrameLock)
                    {
                        img.Source = Frames[0].Source;
                    }
                    img.Width = 500;
                }
                return new Task(() => Play(img, Control));
            });
        }

        public void Run(Decorator parant, Action EndAction = null)
        {
            Touch();
            if (!IsReady)
            {
                EndAction?.Invoke();
                return;
            }
            if (Control?.PlayState == true)
            {
                Control.Stop(() => Run(parant, EndAction));
                return;
            }

            nowid = 0;
            var newControl = new TaskControl(EndAction);
            Control = newControl;
            parant.Dispatcher.Invoke(() =>
            {
                if (parant.Tag == this)
                {
                    Task.Run(() => Play((System.Windows.Controls.Image)parant.Child, newControl));
                    return;
                }

                System.Windows.Controls.Image img;
                if (parant.Child == GraphCore.CommUIElements["Image1.APNGAnimation"])
                {
                    img = (System.Windows.Controls.Image)GraphCore.CommUIElements["Image1.APNGAnimation"];
                }
                else if (parant.Child == GraphCore.CommUIElements["Image3.APNGAnimation"])
                {
                    img = (System.Windows.Controls.Image)GraphCore.CommUIElements["Image3.APNGAnimation"];
                }
                else
                {
                    img = (System.Windows.Controls.Image)GraphCore.CommUIElements["Image2.APNGAnimation"];
                    if (parant.Child != GraphCore.CommUIElements["Image2.APNGAnimation"])
                    {
                        if (img.Parent == null)
                        {
                            parant.Child = img;
                        }
                        else
                        {
                            img = (System.Windows.Controls.Image)GraphCore.CommUIElements["Image1.APNGAnimation"];
                            if (img.Parent != null)
                                ((Decorator)img.Parent).Child = null;
                            parant.Child = img;
                        }
                    }
                }

                parant.Tag = this;
                lock (FrameLock)
                {
                    img.Source = Frames[0].Source;
                }
                img.Width = 500;
                Task.Run(() => Play((System.Windows.Controls.Image)parant.Child, newControl));
            });
        }

        public void Touch() => Interlocked.Exchange(ref LastUseTimeTicks, DateTime.UtcNow.Ticks);

        public void CleanupIdleCache(long cleanTicks)
        {
            if (Control?.PlayState == true)
                return;
            long lastUse = Interlocked.Read(ref LastUseTimeTicks);
            if (cleanTicks < lastUse)
                return;
            lock (FrameLock)
            {
                Frames?.Clear();
            }
            IsReady = false;
        }

        public void Dispose()
        {
            lock (FrameLock)
            {
                Frames?.Clear();
                Frames = null;
            }
            GraphCore = null;
            Control = null;
        }
    }
}
