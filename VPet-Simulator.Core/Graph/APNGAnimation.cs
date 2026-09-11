using LinePutScript;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
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
        private class PngChunk
        {
            public string Type = "";
            public byte[] Data = Array.Empty<byte>();
        }

        private class ApngFrameData
        {
            public int Width;
            public int Height;
            public int XOffset;
            public int YOffset;
            public ushort DelayNum;
            public ushort DelayDen;
            public byte DisposeOp;
            public byte BlendOp;
            public List<byte[]> ImageDataChunks = new List<byte[]>();
        }

        private class ParsedApng
        {
            public int CanvasWidth;
            public int CanvasHeight;
            public byte[] IhdrTemplate = Array.Empty<byte>();
            public List<PngChunk> SharedChunks = new List<PngChunk>();
            public List<ApngFrameData> Frames = new List<ApngFrameData>();
        }

        private const int DefaultFrameDuration = 100;
        private const int FrameCacheAheadCount = 2;
        private static readonly byte[] PngSignature = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
        private static readonly uint[] CrcTable = BuildCrcTable();

        private GraphCore GraphCore;
        private BitmapSource? SpriteSheetSource;
        private Int32Rect[]? FrameRects;
        private readonly object SpriteSheetLock = new object();
        private readonly object FrameCacheLock = new object();
        private readonly Dictionary<int, BitmapSource> FrameCache = new Dictionary<int, BitmapSource>();
        private readonly List<int> FrameDurations = new List<int>();
        private string SpriteSheetPath = string.Empty;
        private int FrameWidth;
        private int FrameHeight;
        private int nowid;

        public bool IsLoop { get; set; }
        public bool IsReady { get; private set; }
        public bool IsFail { get; private set; }
        public string FailMessage { get; private set; } = "";
        public GraphInfo GraphInfo { get; private set; }
        public TaskControl? Control { get; private set; }
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

                IsReady = false;
                IsFail = false;
                FailMessage = "";

                var parsed = ParseApng(Path);
                if (parsed.Frames.Count == 0)
                    throw new InvalidDataException("No APNG frames found.");

                FrameWidth = parsed.CanvasWidth;
                FrameHeight = parsed.CanvasHeight;
                if (FrameWidth > GraphCore.Resolution)
                {
                    FrameWidth = GraphCore.Resolution;
                    FrameHeight = (int)(FrameHeight * (GraphCore.Resolution / (double)parsed.CanvasWidth));
                }
                if (parsed.Frames.Count * FrameWidth >= 60000)
                {
                    FrameWidth = 60000 / parsed.Frames.Count;
                    FrameHeight = (int)(parsed.CanvasHeight * (FrameWidth / (double)parsed.CanvasWidth));
                }

                FrameRects = new Int32Rect[parsed.Frames.Count];
                lock (FrameCacheLock)
                {
                    FrameCache.Clear();
                    FrameDurations.Clear();
                    for (int i = 0; i < parsed.Frames.Count; i++)
                    {
                        FrameRects[i] = new Int32Rect(FrameWidth * i, 0, FrameWidth, FrameHeight);
                        FrameDurations.Add(GetFrameDuration(parsed.Frames[i]));
                    }
                }

                long lastWrite = File.GetLastWriteTimeUtc(Path).Ticks;
                SpriteSheetPath = System.IO.Path.Combine(GraphCore.CachePath, $"apng_{GraphCore.Resolution}_{Math.Abs(Sub.GetHashCode($"{Path}_{lastWrite}"))}_{parsed.Frames.Count}.png");
                // 锁定路径，防止多线程同时生成精灵图
                var sem = GraphCore.SpriteSheetBuildLocks.GetOrAdd(SpriteSheetPath, _ => new SemaphoreSlim(1, 1));
                await sem.WaitAsync();
                try
                {
                    if (!File.Exists(SpriteSheetPath))
                    {
                        BuildSpriteSheet(parsed);
                    }
                }
                finally
                {
                    sem.Release();
                }

                IsReady = true;
            }
            catch (Exception e)
            {
                IsFail = true;
                FailMessage = $"--APNGAnimation--{GraphInfo}--\nPath: {Path}\n{e.Message}";
            }
        }

        private void BuildSpriteSheet(ParsedApng parsed)
        {
            using var canvasBitmap = new SKBitmap(parsed.CanvasWidth, parsed.CanvasHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
            using var combinedBitmap = new SKBitmap(FrameWidth * parsed.Frames.Count, FrameHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
            using var combinedCanvas = new SKCanvas(combinedBitmap);
            canvasBitmap.Erase(SKColors.Transparent);
            combinedBitmap.Erase(SKColors.Transparent);

            for (int i = 0; i < parsed.Frames.Count; i++)
            {
                var frameData = parsed.Frames[i];
                using var previousCanvas = frameData.DisposeOp == 2 ? canvasBitmap.Copy() : null;
                using var patchBitmap = DecodeFrameBitmap(parsed, frameData);
                if (patchBitmap == null)
                    throw new InvalidDataException("Decode APNG frame bitmap failed.");

                using (var canvas = new SKCanvas(canvasBitmap))
                {
                    if (frameData.BlendOp == 0)
                    {
                        using var clearPaint = new SKPaint { BlendMode = SKBlendMode.Src, Color = SKColors.Transparent };
                        canvas.DrawRect(new SKRect(frameData.XOffset, frameData.YOffset, frameData.XOffset + frameData.Width, frameData.YOffset + frameData.Height), clearPaint);
                    }
                    canvas.DrawBitmap(patchBitmap, frameData.XOffset, frameData.YOffset);
                }

                SKBitmap drawBitmap = canvasBitmap;
                SKBitmap? scaledBitmap = null;
                if (canvasBitmap.Width != FrameWidth || canvasBitmap.Height != FrameHeight)
                {
                    scaledBitmap = new SKBitmap(FrameWidth, FrameHeight, canvasBitmap.ColorType, canvasBitmap.AlphaType);
                    canvasBitmap.ScalePixels(scaledBitmap, SKSamplingOptions.Default);
                    drawBitmap = scaledBitmap;
                }

                try
                {
                    combinedCanvas.DrawBitmap(drawBitmap, new SKRect(FrameWidth * i, 0, FrameWidth * (i + 1), FrameHeight));
                }
                finally
                {
                    scaledBitmap?.Dispose();
                }

                switch (frameData.DisposeOp)
                {
                    case 1:
                        using (var canvas = new SKCanvas(canvasBitmap))
                        using (var clearPaint = new SKPaint { BlendMode = SKBlendMode.Src, Color = SKColors.Transparent })
                        {
                            canvas.DrawRect(new SKRect(frameData.XOffset, frameData.YOffset, frameData.XOffset + frameData.Width, frameData.YOffset + frameData.Height), clearPaint);
                        }
                        break;
                    case 2:
                        using (var canvas = new SKCanvas(canvasBitmap))
                        {
                            canvas.Clear(SKColors.Transparent);
                            canvas.DrawBitmap(previousCanvas, 0, 0);
                        }
                        break;
                }
            }

            using var image = SKImage.FromBitmap(combinedBitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = File.Open(SpriteSheetPath, FileMode.Create, FileAccess.Write, FileShare.Read);
            data.SaveTo(stream);
        }

        private static ParsedApng ParseApng(string path)
        {
            using var stream = File.OpenRead(path);
            using var reader = new BinaryReader(stream);
            var signature = reader.ReadBytes(8);
            if (signature.Length != 8 || !MatchesSignature(signature))
                throw new InvalidDataException("Invalid PNG/APNG signature.");

            var result = new ParsedApng();
            ApngFrameData? currentFrame = null;
            bool imageDataStarted = false;

            while (stream.Position < stream.Length)
            {
                uint length = ReadUInt32BigEndian(reader);
                string type = Encoding.ASCII.GetString(reader.ReadBytes(4));
                byte[] data = reader.ReadBytes((int)length);
                ReadUInt32BigEndian(reader);

                switch (type)
                {
                    case "IHDR":
                        result.IhdrTemplate = data;
                        result.CanvasWidth = (int)ReadUInt32BigEndian(data, 0);
                        result.CanvasHeight = (int)ReadUInt32BigEndian(data, 4);
                        break;
                    case "acTL":
                        break;
                    case "fcTL":
                        if (data.Length < 26)
                            throw new InvalidDataException("Invalid fcTL chunk.");
                        currentFrame = new ApngFrameData
                        {
                            Width = (int)ReadUInt32BigEndian(data, 4),
                            Height = (int)ReadUInt32BigEndian(data, 8),
                            XOffset = (int)ReadUInt32BigEndian(data, 12),
                            YOffset = (int)ReadUInt32BigEndian(data, 16),
                            DelayNum = ReadUInt16BigEndian(data, 20),
                            DelayDen = ReadUInt16BigEndian(data, 22),
                            DisposeOp = data[24],
                            BlendOp = data[25],
                        };
                        result.Frames.Add(currentFrame);
                        break;
                    case "IDAT":
                        imageDataStarted = true;
                        if (currentFrame == null)
                        {
                            currentFrame = new ApngFrameData
                            {
                                Width = result.CanvasWidth,
                                Height = result.CanvasHeight,
                                XOffset = 0,
                                YOffset = 0,
                                DelayNum = 1,
                                DelayDen = 10,
                                DisposeOp = 0,
                                BlendOp = 0,
                            };
                            result.Frames.Add(currentFrame);
                        }
                        currentFrame.ImageDataChunks.Add(data);
                        break;
                    case "fdAT":
                        imageDataStarted = true;
                        if (currentFrame == null)
                            throw new InvalidDataException("fdAT found before fcTL.");
                        if (data.Length < 4)
                            throw new InvalidDataException("Invalid fdAT chunk.");
                        byte[] idatData = new byte[data.Length - 4];
                        Buffer.BlockCopy(data, 4, idatData, 0, idatData.Length);
                        currentFrame.ImageDataChunks.Add(idatData);
                        break;
                    case "IEND":
                        return result;
                    default:
                        if (!imageDataStarted)
                        {
                            result.SharedChunks.Add(new PngChunk { Type = type, Data = data });
                        }
                        break;
                }
            }

            return result;
        }

        private static SKBitmap DecodeFrameBitmap(ParsedApng parsed, ApngFrameData frame)
        {
            using var memory = new MemoryStream();
            memory.Write(PngSignature, 0, PngSignature.Length);

            var ihdr = (byte[])parsed.IhdrTemplate.Clone();
            WriteUInt32BigEndian(ihdr, 0, (uint)frame.Width);
            WriteUInt32BigEndian(ihdr, 4, (uint)frame.Height);
            WriteChunk(memory, "IHDR", ihdr);

            foreach (var chunk in parsed.SharedChunks)
            {
                WriteChunk(memory, chunk.Type, chunk.Data);
            }

            foreach (var idat in frame.ImageDataChunks)
            {
                WriteChunk(memory, "IDAT", idat);
            }

            WriteChunk(memory, "IEND", Array.Empty<byte>());
            return SKBitmap.Decode(memory.ToArray());
        }

        private BitmapSource? GetFrameSource(int frameIndex)
        {
            Touch();
            EnsureSpriteSheetLoaded();
            if (FrameRects == null || frameIndex < 0 || frameIndex >= FrameRects.Length || SpriteSheetSource == null)
                return null;
            lock (FrameCacheLock)
            {
                if (FrameCache.TryGetValue(frameIndex, out var cacheFrame))
                    return cacheFrame;

                var frame = new CroppedBitmap(SpriteSheetSource, FrameRects[frameIndex]);
                frame.Freeze();
                FrameCache[frameIndex] = frame;

                var keepKeys = GetForwardKeepKeys(frameIndex);
                var removeKeys = new List<int>();
                foreach (var key in FrameCache.Keys)
                {
                    if (!keepKeys.Contains(key))
                        removeKeys.Add(key);
                }
                foreach (var key in removeKeys)
                {
                    FrameCache.Remove(key);
                }

                return frame;
            }
        }

        private HashSet<int> GetForwardKeepKeys(int frameIndex)
        {
            var keep = new HashSet<int> { frameIndex };
            int cursor = frameIndex;
            for (int i = 0; i < FrameCacheAheadCount; i++)
            {
                cursor++;
                if (FrameRects == null || cursor >= FrameRects.Length)
                {
                    if (!IsLoop)
                        break;
                    cursor = 0;
                }
                keep.Add(cursor);
            }
            return keep;
        }

        private void EnsureSpriteSheetLoaded()
        {
            if (SpriteSheetSource != null)
                return;
            lock (SpriteSheetLock)
            {
                if (SpriteSheetSource != null)
                    return;
                BitmapImage spriteSheet = new BitmapImage();
                spriteSheet.BeginInit();
                spriteSheet.CacheOption = BitmapCacheOption.OnDemand;
                spriteSheet.CreateOptions = BitmapCreateOptions.DelayCreation;
                spriteSheet.UriSource = new Uri(SpriteSheetPath);
                spriteSheet.EndInit();
                spriteSheet.Freeze();
                SpriteSheetSource = spriteSheet;
            }
        }

        private static int GetFrameDuration(ApngFrameData frameData)
        {
            uint delayNum = frameData.DelayNum == 0 ? 1u : frameData.DelayNum;
            uint delayDen = frameData.DelayDen == 0 ? 100u : frameData.DelayDen;
            int duration = (int)Math.Round(delayNum * 1000d / delayDen);
            return duration <= 0 ? DefaultFrameDuration : duration;
        }

        private static bool MatchesSignature(byte[] signature)
        {
            for (int i = 0; i < PngSignature.Length; i++)
            {
                if (signature[i] != PngSignature[i])
                    return false;
            }
            return true;
        }

        private static uint ReadUInt32BigEndian(BinaryReader reader)
        {
            var bytes = reader.ReadBytes(4);
            if (bytes.Length != 4)
                throw new EndOfStreamException();
            return ReadUInt32BigEndian(bytes, 0);
        }

        private static uint ReadUInt32BigEndian(byte[] data, int offset)
        {
            return ((uint)data[offset] << 24)
                | ((uint)data[offset + 1] << 16)
                | ((uint)data[offset + 2] << 8)
                | data[offset + 3];
        }

        private static ushort ReadUInt16BigEndian(byte[] data, int offset)
        {
            return (ushort)(((uint)data[offset] << 8) | data[offset + 1]);
        }

        private static void WriteUInt32BigEndian(byte[] data, int offset, uint value)
        {
            data[offset] = (byte)(value >> 24);
            data[offset + 1] = (byte)(value >> 16);
            data[offset + 2] = (byte)(value >> 8);
            data[offset + 3] = (byte)value;
        }

        private static void WriteUInt32BigEndian(Stream stream, uint value)
        {
            stream.WriteByte((byte)(value >> 24));
            stream.WriteByte((byte)(value >> 16));
            stream.WriteByte((byte)(value >> 8));
            stream.WriteByte((byte)value);
        }

        private static void WriteChunk(Stream stream, string type, byte[] data)
        {
            byte[] typeBytes = Encoding.ASCII.GetBytes(type);
            WriteUInt32BigEndian(stream, (uint)data.Length);
            stream.Write(typeBytes, 0, typeBytes.Length);
            if (data.Length > 0)
                stream.Write(data, 0, data.Length);
            WriteUInt32BigEndian(stream, ComputeCrc(typeBytes, data));
        }

        private static uint[] BuildCrcTable()
        {
            var table = new uint[256];
            for (uint n = 0; n < table.Length; n++)
            {
                uint c = n;
                for (int k = 0; k < 8; k++)
                {
                    c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
                }
                table[n] = c;
            }
            return table;
        }

        private static uint ComputeCrc(byte[] typeBytes, byte[] data)
        {
            uint c = 0xFFFFFFFFu;
            for (int i = 0; i < typeBytes.Length; i++)
            {
                c = CrcTable[(c ^ typeBytes[i]) & 0xFF] ^ (c >> 8);
            }
            for (int i = 0; i < data.Length; i++)
            {
                c = CrcTable[(c ^ data[i]) & 0xFF] ^ (c >> 8);
            }
            return c ^ 0xFFFFFFFFu;
        }

        private void Play(FrameworkElement element, TaskControl control)
        {
            Touch();
            while (true)
            {
                int frameIndex;
                int duration;
                BitmapSource? frameSource;
                lock (FrameCacheLock)
                {
                    if (FrameDurations.Count == 0)
                    {
                        control.Type = TaskControl.ControlType.Status_Stoped;
                        Task.Run(() => control.EndAction?.Invoke());
                        return;
                    }
                    frameIndex = nowid;
                    duration = FrameDurations[frameIndex];
                }
                frameSource = GetFrameSource(frameIndex);

                element.Dispatcher.Invoke(() =>
                {
                    if (element is System.Windows.Controls.Image image)
                    {
                        image.Source = frameSource;
                    }
                    element.Margin = new Thickness(0, 0, 0, 0);
                });

                Thread.Sleep(duration);

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
                        if (++nowid >= FrameDurations.Count)
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
                        break;
                }
            }
        }

        public Task Run(System.Windows.Controls.Image img, Action? EndAction = null)
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
                if (img.Tag == this)
                {
                    return new Task(() => Play(img, Control));
                }
                img.Tag = this;
                img.Source = GetFrameSource(0);
                img.Width = 500;
                return new Task(() => Play(img, Control));
            });
        }

        public void Run(Decorator parant, Action? EndAction = null)
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
                img.Source = GetFrameSource(0);
                img.Width = 500;
                Task.Run(() => Play((System.Windows.Controls.Image)parant.Child, newControl));
            });
        }

        public void Touch() => Interlocked.Exchange(ref LastUseTimeTicks, DateTime.UtcNow.Ticks);

        public void CleanupIdleCache(long cleanTicks)
        {
            if (Control?.PlayState == true)
                return;
            if (SpriteSheetSource == null)
                return;
            long lastUse = Interlocked.Read(ref LastUseTimeTicks);
            if (cleanTicks < lastUse)
                return;

            lock (SpriteSheetLock)
            {
                SpriteSheetSource = null;
            }
            lock (FrameCacheLock)
            {
                FrameCache.Clear();
            }
        }

        public void Dispose()
        {
            FrameRects = [];
            lock (SpriteSheetLock)
            {
                SpriteSheetSource = null;
            }
            lock (FrameCacheLock)
            {
                FrameCache.Clear();
                FrameDurations.Clear();
            }
            //GraphCore = null;
        }
    }
}
