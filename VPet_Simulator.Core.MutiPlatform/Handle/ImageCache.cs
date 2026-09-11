using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;

namespace VPet_Simulator.Core.MutiPlatform;

/// <summary>
/// 按路径缓存解码好的位图
/// </summary>
/// 官方 MOD 就有一百多张食物图, 加上物品、照片、主题, 全部预解码既慢又占内存;
/// 而每次用都重解一遍, 翻商店时会一格一格地卡。所以按用到的解, 解过的留着。
///
/// 一张图坏了不该让整个界面用不了 —— 解不出来记一条日志给 null, 调用方自己兜底。
public class ImageCache : IDisposable
{
    private readonly Dictionary<string, Bitmap?> cache
        = new Dictionary<string, Bitmap?>(StringComparer.OrdinalIgnoreCase);
    private readonly object gate = new object();

    /// <summary>
    /// 解不出来时记一条
    /// </summary>
    public Action<string>? OnError { get; set; }

    /// <summary>
    /// 缓存里现在有多少张
    /// </summary>
    public int Count
    {
        get { lock (gate) return cache.Count; }
    }

    /// <summary>
    /// 取一张图, 取不到返回 null
    /// </summary>
    /// <param name="path">图片路径, null 直接返回 null</param>
    /// 解码失败的也会记进缓存(记成 null): 一张坏图不该每次用到都重试一遍,
    /// 那会在动画循环里反复读磁盘。
    public Bitmap? Get(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return null;
        lock (gate)
        {
            if (cache.TryGetValue(path, out var cached))
                return cached;
        }

        Bitmap? bitmap = null;
        try
        {
            bitmap = new Bitmap(path);
        }
        catch (Exception ex)
        {
            OnError?.Invoke($"图片加载失败 {path}: {ex.Message}");
        }

        lock (gate)
        {
            // 解码期间别的线程可能已经放进去了, 那就用它那张, 免得留下两份
            if (cache.TryGetValue(path, out var raced))
            {
                bitmap?.Dispose();
                return raced;
            }
            cache[path] = bitmap;
        }
        return bitmap;
    }

    /// <summary>
    /// 清空缓存
    /// </summary>
    /// 换主题时用: 主题会整套换掉图片, 留着旧的只是占内存
    public void Clear()
    {
        List<Bitmap?> old;
        lock (gate)
        {
            old = new List<Bitmap?>(cache.Values);
            cache.Clear();
        }
        foreach (var bitmap in old)
            bitmap?.Dispose();
    }

    public void Dispose() => Clear();
}
