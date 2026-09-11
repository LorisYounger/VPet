using LinePutScript;
using LinePutScript.Localization.WPF;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VPet_Simulator.Core;

namespace VPet_Simulator.Windows.Interface;

/// <summary>
/// 照片的 Windows 半
/// </summary>
/// 留在这里的是位图解码、缩略图、灰度化、剪贴板和另存为 —— 全是 WPF 的东西.
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
    public static BitmapSource ConvertToThumbnail(BitmapImage originalImage, int width, int height)
    {
        // 创建一个 RenderTargetBitmap
        if (originalImage.Width < width && originalImage.Height < height
            || width == 0
            || height == 0)
        {
            return originalImage;
        }
        // 计算缩放比例
        double scaleX = (double)width / originalImage.PixelWidth;
        double scaleY = (double)height / originalImage.PixelHeight;
        double scale = Math.Min(scaleX, scaleY); // 选择较小的比例以保持纵横比

        // 计算缩放后的尺寸
        int scaledWidth = (int)(originalImage.PixelWidth * scale);
        int scaledHeight = (int)(originalImage.PixelHeight * scale);

        RenderTargetBitmap renderBitmap = new RenderTargetBitmap(scaledWidth, scaledHeight, 96d, 96d, PixelFormats.Pbgra32);
        DrawingVisual visual = new DrawingVisual();

        using (DrawingContext drawingContext = visual.RenderOpen())
        {
            // 绘制图像
            drawingContext.DrawImage(originalImage, new Rect(0, 0, scaledWidth, scaledHeight));
        }

        renderBitmap.Render(visual);
        return renderBitmap;
    }

    /// <summary>
    /// 创建灰度图 (未解锁)
    /// </summary>
    /// <param name="originalImage">原图</param>
    /// <returns></returns>
    public static BitmapSource ConvertToGrayScale(BitmapSource originalImage)
    {
        // 创建 WriteableBitmap
        WriteableBitmap writeableBitmap = new WriteableBitmap(originalImage);
        int width = writeableBitmap.PixelWidth;
        int height = writeableBitmap.PixelHeight;

        // 获取像素数据
        int[] pixels = new int[width * height];
        writeableBitmap.CopyPixels(pixels, width * 4, 0);

        // 转换为灰度
        for (int i = 0; i < pixels.Length; i++)
        {
            // 获取 ARGB 颜色
            byte a = (byte)((pixels[i] >> 24) & 0xff); // Alpha
            byte r = (byte)((pixels[i] >> 16) & 0xff); // Red
            byte g = (byte)((pixels[i] >> 8) & 0xff);  // Green
            byte b = (byte)(pixels[i] & 0xff);         // Blue

            // 计算灰度值
            byte gray = (byte)((r + g + b) / 3); // 可以使用其他公式来计算灰度

            // 设置新的灰度像素值
            pixels[i] = (a << 24) | (gray << 16) | (gray << 8) | gray; // ARGB
        }

        // 创建新的 WriteableBitmap
        WriteableBitmap grayBitmap = new WriteableBitmap(width, height, writeableBitmap.DpiX, writeableBitmap.DpiY, PixelFormats.Pbgra32, null);
        grayBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 4, 0);

        return grayBitmap;
    }

    /// <summary>
    /// 获得当前图片图片
    /// </summary>
    public BitmapImage GetImage(IMainWindow imw)
    {
        //解压zip
        string? zippath = imw.FileSources.FindSource(Zip + ".zlps");
        if (zippath == null)
        {
            zippath = imw.FileSources.FindSource(Zip + ".zip");
        }
        if (zippath == null)
        {
            return ImageResources.NewSafeBitmapImage("pack://application:,,,/Res/img/error.png");
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

                        // 创建 BitmapImage
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = memoryStream; // 使用内存流
                        bitmap.CacheOption = BitmapCacheOption.OnLoad; // 立即加载
                        bitmap.EndInit();
                        bitmap.Freeze(); // 使 BitmapImage 可以在不同线程中使用
                        return bitmap;
                    }
                }
            }
            else
            {
                return ImageResources.NewSafeBitmapImage("pack://application:,,,/Res/img/error.png");
            }
        }
    }

    /// <summary>
    /// 获取适用于GIF的图片
    /// </summary>
    public BitmapImage GetGifImage(IMainWindow imw)
    {   //不要看 GIF图片和普通图片读取代码差不多, 实际上一旦回收了MemoryStream, GIF图片控件加载就会出问题
        //但是这个方法不回收MemoryStream, 占用内存更多, 为了节省内存, 普通图片用GetImage, GIF图片用这个

        // 解压zip
        string? zippath = imw.FileSources.FindSource(Zip + ".zlps");
        if (zippath == null)
        {
            zippath = imw.FileSources.FindSource(Zip + ".zip");
        }
        if (zippath == null)
        {
            return ImageResources.NewSafeBitmapImage("pack://application:,,,/Res/img/error.png");
        }

        using (ZipArchive archive = ZipFile.OpenRead(zippath))
        {
            // 找到指定的文件
            ZipArchiveEntry? entry = archive.GetEntry(Path);
            if (entry != null)
            {
                using (Stream stream = entry.Open())
                {
                    // 创建一个新的 MemoryStream
                    var memstr = new MemoryStream();
                    stream.CopyTo(memstr); // 将流内容复制到 MemoryStream

                    // 创建 BitmapImage
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad; // 立即加载
                    bitmap.StreamSource = memstr; // 设置流源
                    bitmap.EndInit();
                    bitmap.Freeze(); // 使 BitmapImage 可以在不同线程中使用

                    return bitmap; // 返回 BitmapImage
                }
            }
            else
            {
                return ImageResources.NewSafeBitmapImage("pack://application:,,,/Res/img/error.png");
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
        if (!Directory.Exists(System.IO.Path.Combine(GraphCore.CachePath, "photo")))
        {
            Directory.CreateDirectory(System.IO.Path.Combine(GraphCore.CachePath, "photo"));
        }
        string filepath = System.IO.Path.Combine(GraphCore.CachePath, "photo", $"pic_{Zip}_{Sub.GetHashCode(Path):x}.png");
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

        // 创建一个 DataObject 并将文件路径添加到 DataObject 中
        DataObject dataObject = new DataObject();
        dataObject.SetFileDropList(new System.Collections.Specialized.StringCollection { filepath });

        // 将 DataObject 设置为剪贴板内容
        Clipboard.SetDataObject(dataObject);

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
