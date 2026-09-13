using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace VPet_Simulator.Core.MutiPlatform.Display.Shell;

/// <summary>
/// 会动的图 (GIF 一类): 逐帧解码好, 交给 ImageBehavior 轮播
/// </summary>
/// 跨平台: Windows 版靠 WpfAnimatedGif 包 (ImageBehavior.SetAnimatedSource(image, bitmapImage)) 播 GIF, Avalonia 的 Image 只画静图,
/// 这里用 SkiaSharp 的 SKCodec 把每一帧解出来, 由 ImageBehavior 按各帧的时长轮着换 Image.Source. 不是动图的文件就只有一帧.
public sealed class AnimatedBitmap
{
    /// <summary>
    /// 各帧
    /// </summary>
    public IReadOnlyList<Bitmap> Frames { get; }
    /// <summary>
    /// 各帧停留时长 (毫秒)
    /// </summary>
    public IReadOnlyList<int> Durations { get; }
    /// <summary>
    /// 第一帧, 当静图用
    /// </summary>
    public Bitmap First => Frames[0];

    private AnimatedBitmap(IReadOnlyList<Bitmap> frames, IReadOnlyList<int> durations)
    {
        Frames = frames;
        Durations = durations;
    }

    /// <summary>
    /// 从文件内容解码
    /// </summary>
    public static AnimatedBitmap Load(Stream stream)
    {
        var bytes = new MemoryStream();
        stream.CopyTo(bytes);
        var data = bytes.ToArray();
        try
        {
            using var codec = SKCodec.Create(new MemoryStream(data));
            if (codec != null && codec.FrameCount > 1)
            {
                var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
                using var canvas = new SKBitmap(info);
                var frames = new List<Bitmap>();
                var durations = new List<int>();
                for (int i = 0; i < codec.FrameCount; i++)
                {
                    //上一帧还留在 canvas 里, 让解码器按 GIF 的处置规则在它上面画这一帧
                    var options = new SKCodecOptions(i, i == 0 ? -1 : i - 1);
                    codec.GetPixels(info, canvas.GetPixels(), options);
                    var frame = new WriteableBitmap(new Avalonia.PixelSize(info.Width, info.Height), new Avalonia.Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
                    using (var buffer = frame.Lock())
                    {
                        var row = new byte[info.Width * 4];
                        for (int y = 0; y < info.Height; y++)
                        {
                            Marshal.Copy(canvas.GetPixels() + y * canvas.RowBytes, row, 0, row.Length);
                            Marshal.Copy(row, 0, buffer.Address + y * buffer.RowBytes, row.Length);
                        }
                    }
                    frames.Add(frame);
                    var duration = codec.FrameInfo[i].Duration;
                    durations.Add(duration <= 0 ? 100 : duration);
                }
                return new AnimatedBitmap(frames, durations);
            }
        }
        catch
        {
            //解不出帧就当静图
        }
        return new AnimatedBitmap(new[] { new Bitmap(new MemoryStream(data)) }, new[] { 0 });
    }

    /// <summary>
    /// 静图也包成这个类型, 好让调用处一律走 ImageBehavior
    /// </summary>
    public static AnimatedBitmap FromBitmap(Bitmap bitmap) => new AnimatedBitmap(new[] { bitmap }, new[] { 0 });
}

/// <summary>
/// 让 Image 播动图, 对应 WpfAnimatedGif 的 ImageBehavior
/// </summary>
public static class ImageBehavior
{
    private static readonly ConditionalWeakTable<Image, DispatcherTimer> timers = new();

    /// <summary>
    /// 给 Image 设一张会动的图 (UI 线程调用); 再设一次会停掉上一张
    /// </summary>
    public static void SetAnimatedSource(Image image, AnimatedBitmap source)
    {
        if (timers.TryGetValue(image, out var old))
        {
            old.Stop();
            timers.Remove(image);
        }
        image.Source = source.First;
        if (source.Frames.Count <= 1)
            return;
        int index = 0;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(source.Durations[0]) };
        timer.Tick += (_, _) =>
        {
            index = (index + 1) % source.Frames.Count;
            image.Source = source.Frames[index];
            timer.Interval = TimeSpan.FromMilliseconds(source.Durations[index]);
        };
        timers.Add(image, timer);
        timer.Start();
    }
}
