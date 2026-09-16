using Avalonia.Media.Imaging;
using LinePutScript;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using VPet_Simulator.Windows.Interface;

namespace VPet_Simulator.Windows.Interface;

/// <summary>
/// 照片图库
/// </summary>
/// 照片本体压在 zip 里(MOD 目录下的 file/*.zlps 或 *.zip), lps 里记的是"哪个包
/// 里的哪个文件"加上一串解锁条件。解锁条件那部分是共享源码(Photo.UnlockCondition),
/// 两个平台判出来的结果一样; 这里只管把图从包里取出来。
///
/// 解压出来的图会缓存: 一张图翻来覆去看很正常, 每次都重新开包解压太慢。
public class PhotoStore : IDisposable
{
    private readonly Dictionary<string, Bitmap> cache
        = new Dictionary<string, Bitmap>(StringComparer.OrdinalIgnoreCase);
    private readonly object gate = new object();

    /// <summary>
    /// 扫到的照片
    /// </summary>
    public List<Photo> Photos { get; } = new List<Photo>();

    /// <summary>
    /// 照片包: 包名到 zip 路径
    /// </summary>
    public Dictionary<string, string> Archives { get; }
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 出问题时记一条
    /// </summary>
    public Action<string>? OnError { get; set; }

    /// <summary>
    /// 从一个 MOD 的 photo 目录读照片
    /// </summary>
    public void LoadPhotos(DirectoryInfo directory)
    {
        foreach (var file in directory.EnumerateFiles("*.lps"))
        {
            try
            {
                foreach (var line in new LPS(File.ReadAllText(file.FullName)))
                {
                    if (line.Name != "photo")
                        continue;
                    Photos.Add(new Photo((Line)line));
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"照片表 {file.Name} 读取失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 登记一个照片包
    /// </summary>
    /// <param name="name">包名(不含扩展名)</param>
    /// <param name="path">zip 路径</param>
    public void AddArchive(string name, string path) => Archives[name] = path;

    /// <summary>
    /// 从 MOD 的 file 目录里找照片包
    /// </summary>
    /// 与 Windows 版一致: 认 .zlps 和 .zip 两种扩展名
    public void LoadArchives(DirectoryInfo directory)
    {
        foreach (var file in directory.EnumerateFiles("*.zlps"))
            AddArchive(Path.GetFileNameWithoutExtension(file.Name), file.FullName);
        foreach (var file in directory.EnumerateFiles("*.zip"))
            AddArchive(Path.GetFileNameWithoutExtension(file.Name), file.FullName);
        foreach (var sub in directory.EnumerateDirectories())
            LoadArchives(sub);
    }

    /// <summary>
    /// 把玩家的解锁记录读进来
    /// </summary>
    /// <param name="data">存档的数据部分</param>
    /// 存档里 photo 行下面每个子项是一张已解锁的照片, 与 Windows 版同一份格式
    public void LoadUnlocked(ILPS data)
    {
        var line = data.FindLine("photo");
        if (line == null)
            return;
        foreach (var photo in Photos)
        {
            var sub = line.Find(photo.Name);
            if (sub != null)
                photo.PlayerInfo = new Photo.Info(sub);
        }
    }

    /// <summary>
    /// 解锁一张照片
    /// </summary>
    public void Unlock(ILPS data, Photo photo)
    {
        if (photo.IsUnlock)
            return;
        var line = data.FindorAddLine("photo");
        var sub = line.Find(photo.Name);
        if (sub == null)
        {
            sub = new Sub(photo.Name);
            line.AddSub(sub);
        }
        photo.PlayerInfo = new Photo.Info(sub) { UnlockTime = DateTime.Now };
    }

    /// <summary>
    /// 取一张照片的图
    /// </summary>
    /// <returns>包里没有或读不出来时返回 null</returns>
    public Bitmap? GetImage(Photo photo)
    {
        var key = photo.Zip + "|" + photo.Path;
        lock (gate)
        {
            if (cache.TryGetValue(key, out var cached))
                return cached;
        }

        Bitmap? bitmap = null;
        if (Archives.TryGetValue(photo.Zip, out var archivePath))
        {
            try
            {
                using var archive = ZipFile.OpenRead(archivePath);
                var entry = archive.GetEntry(photo.Path);
                if (entry == null)
                {
                    OnError?.Invoke($"照片包 {photo.Zip} 里没有 {photo.Path}");
                }
                else
                {
                    using var stream = entry.Open();
                    // Bitmap 要能随机读, zip 的解压流不行, 先落到内存里
                    using var memory = new MemoryStream();
                    stream.CopyTo(memory);
                    memory.Position = 0;
                    bitmap = new Bitmap(memory);
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"照片 {photo.Name} 读取失败: {ex.Message}");
            }
        }
        else
        {
            OnError?.Invoke($"找不到照片包 {photo.Zip}");
        }

        if (bitmap == null)
            return null;
        lock (gate)
        {
            if (cache.TryGetValue(key, out var raced))
            {
                bitmap.Dispose();
                return raced;
            }
            cache[key] = bitmap;
        }
        return bitmap;
    }

    public void Dispose()
    {
        List<Bitmap> old;
        lock (gate)
        {
            old = cache.Values.ToList();
            cache.Clear();
        }
        foreach (var bitmap in old)
            bitmap.Dispose();
    }
}
