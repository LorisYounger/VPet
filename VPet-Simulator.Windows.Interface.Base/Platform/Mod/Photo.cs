//跨平台: 对应 VPet-Simulator.Windows.Interface/Mod/Photo.cs (照片的 Windows 半). 位图解码、缩略图、灰度化
//这几处是 WPF 的东西, 这里用 Avalonia 的 Bitmap 与 SkiaSharp 做; 解压、另存为、解锁记录逐行相同.
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using LinePutScript;
using LinePutScript.Localization;
using SkiaSharp;
using VPet_Simulator.Core.MutiPlatform.Display.Shell;
using System;
using System.IO;
using System.IO.Compression;
using VPet_Simulator.Core;

namespace VPet_Simulator.Windows.Interface;

/// <summary>
/// 照片的跨平台半
/// </summary>
/// 留在这里的是位图解码、缩略图、灰度化、剪贴板和另存为.
/// 解锁条件、标签、存档里的解锁记录都在共享源码那一半.
public partial class Photo
{
    /// <summary>
    /// 创建缩略图 (以最小的为准)
    /// </summary>
    /// <param name="originalImage">原图</param>
    /// <param name="width">长度</param>
    /// <param name="height">高度</param>
    /// <returns></returns>
    public static Bitmap ConvertToThumbnail(Bitmap originalImage, int width, int height)
    {
        if (originalImage.PixelSize.Width < width && originalImage.PixelSize.Height < height
            || width == 0
            || height == 0)
        {
            return originalImage;
        }
        // 计算缩放比例
        double scaleX = (double)width / originalImage.PixelSize.Width;
        double scaleY = (double)height / originalImage.PixelSize.Height;
        double scale = Math.Min(scaleX, scaleY); // 选择较小的比例以保持纵横比

        // 计算缩放后的尺寸
        int scaledWidth = (int)(originalImage.PixelSize.Width * scale);
        int scaledHeight = (int)(originalImage.PixelSize.Height * scale);

        return originalImage.CreateScaledBitmap(new Avalonia.PixelSize(Math.Max(1, scaledWidth), Math.Max(1, scaledHeight)));
    }

    /// <summary>
    /// 创建灰度图 (未解锁)
    /// </summary>
    /// <param name="originalImage">原图</param>
    /// <returns></returns>
    public static Bitmap ConvertToGrayScale(Bitmap originalImage)
    {
        //Avalonia 的 Bitmap 不给逐像素访问, 绕道 SkiaSharp: 编码成 PNG 再解出来改
        using var stream = new MemoryStream();
        originalImage.Save(stream);
        stream.Position = 0;
        using var skia = SKBitmap.Decode(stream);
        int width = skia.Width;
        int height = skia.Height;
        using var gray = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                var c = skia.GetPixel(x, y);
                // 计算灰度值
                byte g = (byte)((c.Red + c.Green + c.Blue) / 3); // 可以使用其他公式来计算灰度
                gray.SetPixel(x, y, new SKColor(g, g, g, c.Alpha));
            }
        using var image = SKImage.FromBitmap(gray);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var output = new MemoryStream();
        data.SaveTo(output);
        output.Position = 0;
        return new Bitmap(output);
    }

    /// <summary>
    /// 获得当前图片图片
    /// </summary>
    public Bitmap GetImage(IMainWindow imw)
    {
        //解压zip
        string? zippath = imw.FileSources.FindSource(Zip + ".zlps");
        if (zippath == null)
        {
            zippath = imw.FileSources.FindSource(Zip + ".zip");
        }
        if (zippath == null)
        {
            return ImageResources.NewSafeBitmapImage(ImageResources.ErrorImage);
        }
        using (ZipArchive archive = ZipFile.OpenRead(zippath))
        {
            // 找到指定的文件
            ZipArchiveEntry? entry = archive.GetEntry(Path);
            if (entry != null)
            {
                using (Stream stream = entry.Open())
                {
                    // 将流内容复制到内存流中
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        stream.CopyTo(memoryStream);
                        memoryStream.Position = 0; // 重置内存流的位置

                        // 创建 Bitmap (Avalonia 的 Bitmap 本来就能跨线程用, 不需要 Freeze)
                        return new Bitmap(memoryStream);
                    }
                }
            }
            else
            {
                return ImageResources.NewSafeBitmapImage(ImageResources.ErrorImage);
            }
        }
    }

    /// <summary>
    /// 获取适用于GIF的图片
    /// </summary>
    /// 跨平台: Avalonia 的 Bitmap 不会自己播 GIF, 这里把各帧解出来 (AnimatedBitmap), 交给 ImageBehavior 播; 静图就只有一帧
    public AnimatedBitmap GetGifImage(IMainWindow imw)
    {
        // 解压zip
        string? zippath = imw.FileSources.FindSource(Zip + ".zlps");
        if (zippath == null)
        {
            zippath = imw.FileSources.FindSource(Zip + ".zip");
        }
        if (zippath == null)
        {
            return AnimatedBitmap.FromBitmap(ImageResources.NewSafeBitmapImage(ImageResources.ErrorImage));
        }

        using (ZipArchive archive = ZipFile.OpenRead(zippath))
        {
            // 找到指定的文件
            ZipArchiveEntry? entry = archive.GetEntry(Path);
            if (entry != null)
            {
                using (Stream stream = entry.Open())
                {
                    return AnimatedBitmap.Load(stream);
                }
            }
            else
            {
                return AnimatedBitmap.FromBitmap(ImageResources.NewSafeBitmapImage(ImageResources.ErrorImage));
            }
        }
    }

    /// <summary>
    /// 图片另存为文件
    /// </summary>
    public void SaveAs(IMainWindow imw, string filepath)
    {
        //解压zip
        string? zippath = imw.FileSources.FindSource(Zip + ".zlps");
        if (zippath == null)
        {
            zippath = imw.FileSources.FindSource(Zip + ".zip");
        }
        if (zippath == null)
        {
            return;
        }
        using (ZipArchive archive = ZipFile.OpenRead(zippath))
        {
            // 找到指定的文件
            ZipArchiveEntry? entry = archive.GetEntry(Path);
            if (entry != null)
            {
                // 打开源文件流
                using (Stream sourceStream = entry.Open())
                {
                    // 创建目标文件流
                    using (FileStream destinationStream = new FileStream(filepath, FileMode.Create, FileAccess.Write))
                    {
                        // 将源文件流复制到目标文件流
                        sourceStream.CopyTo(destinationStream);
                    }
                }
            }
            else
            {
                return;
            }
        }
    }

    /// <summary>
    /// 复制图片到剪贴板
    /// </summary>
    /// 跨平台: 先解到缓存目录 (与 Windows 版同一个位置), 再把文件路径放进剪贴板
    public bool CopyImageToClipboard(IMainWindow imw)
    {
        // 解压zip
        string? zippath = imw.FileSources.FindSource(Zip + ".zlps");
        if (zippath == null)
        {
            zippath = imw.FileSources.FindSource(Zip + ".zip");
        }
        if (zippath == null)
        {
            return false;
        }
        //先看看缓存里面有没有
        if (!Directory.Exists(System.IO.Path.Combine(VPet_Simulator.Core.MutiPlatform.AppPaths.CacheRoot, "photo")))
        {
            Directory.CreateDirectory(System.IO.Path.Combine(VPet_Simulator.Core.MutiPlatform.AppPaths.CacheRoot, "photo"));
        }
        string filepath = System.IO.Path.Combine(VPet_Simulator.Core.MutiPlatform.AppPaths.CacheRoot, "photo", $"pic_{Zip}_{Sub.GetHashCode(Path):x}.png");
        if (!File.Exists(filepath))
        {
            using (ZipArchive archive = ZipFile.OpenRead(zippath))
            {
                // 找到指定的文件
                ZipArchiveEntry? entry = archive.GetEntry(Path);
                if (entry != null)
                {
                    // 打开源文件流
                    using (Stream sourceStream = entry.Open())
                    {
                        // 创建目标文件流
                        using (FileStream destinationStream = new FileStream(filepath, FileMode.Create, FileAccess.Write))
                        {
                            // 将源文件流复制到目标文件流
                            sourceStream.CopyTo(destinationStream);
                        }
                    }
                }
                else
                {
                    return false;
                }
            }
        }

        // 把图片放进剪贴板 (WPF 版放的是文件列表; Avalonia 各平台对文件列表的支持不一, 放位图最稳)
        var clipboard = Avalonia.Controls.TopLevel.GetTopLevel(imw.Main)?.Clipboard;
        if (clipboard == null)
            return false;
        Avalonia.Input.Platform.ClipboardExtensions.SetBitmapAsync(clipboard, new Bitmap(filepath));

        return true;
    }

    /// <summary>
    /// 解锁这张图片
    /// </summary>
    public void Unlock(IMainWindow imw)
    {
        ISub sub = imw.GameSavesData["photo"][Name];
        PlayerInfo = new Info(sub);
        PlayerInfo.UnlockTime = DateTime.Now;
    }

    public void LoadUserInfo(IMainWindow imw)
    {
        if (imw.GameSavesData["photo"].Contains(Name))
        {
            PlayerInfo = new Info(imw.GameSavesData["photo"][Name]);
        }
        else
            PlayerInfo = null;
    }

}
