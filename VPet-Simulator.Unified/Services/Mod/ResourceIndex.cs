using System;
using System.Collections.Generic;
using System.IO;

namespace VPet_Simulator.Unified.Services;

/// <summary>
/// MOD 提供的图片和文件的名字到路径的索引
/// </summary>
/// 从 Windows 版 CoreMOD.LoadImage / LoadFile 和 Interface 的 Resources 里抽出来的.
/// 键的拼法一字未改, 这是硬要求 —— MOD 换官方素材靠的就是"用同一个键覆盖":
///
///   image/food/可乐.png     ->  food_可乐
///   image/food.png          ->  food
///   image/work/A/b.png      ->  work_a_b
///   file/pack/x.zip         ->  pack_x.zip   (文件带扩展名, 图片不带)
///
/// 与 Windows 版不同的一点: 这里只存路径, 不存解码后的位图. 解码交给各自宿主
/// (WPF 的 BitmapImage / Avalonia 的 Bitmap), 后端不碰界面类型.
public class ResourceIndex
{
    private readonly Dictionary<string, string> sources = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 名字到路径的全部映射
    /// </summary>
    public IReadOnlyDictionary<string, string> Sources => sources;

    /// <summary>
    /// 递归收录一个目录下的图片
    /// </summary>
    /// <param name="directory">目录</param>
    /// <param name="prefix">键的前缀</param>
    /// <param name="settings">遇到的 set_*.lps 按遍历顺序放进来, 由宿主自己解释</param>
    /// 与 Windows 版 CoreMOD.LoadImage 一致: 文件名去掉 .png 并转小写, 嵌套目录名
    /// 按 目录_ 逐层加在前面. 后来的覆盖之前的.
    ///
    /// 遍历顺序必须是"本层图片 → 子目录 → 本层 lps 声明", 与 Windows 版逐字一致 ——
    /// 覆盖是按顺序来的, 换个顺序就会换一批赢家.
    public void AddImages(DirectoryInfo directory, string prefix = "", List<string>? settings = null)
    {
        if (!directory.Exists)
            return;
        foreach (var file in directory.EnumerateFiles("*.png"))
        {
            sources[prefix + Path.GetFileNameWithoutExtension(file.Name).ToLowerInvariant()] = file.FullName;
        }
        foreach (var sub in directory.EnumerateDirectories())
        {
            AddImages(sub, prefix + sub.Name.ToLowerInvariant() + "_", settings);
        }
        foreach (var file in directory.EnumerateFiles("*.lps"))
        {
            // set_ 开头的是图片设置(定位锚点之类), 不是图片声明
            if (file.Name.StartsWith("set_", StringComparison.OrdinalIgnoreCase))
                settings?.Add(file.FullName);
            else
                AddDeclarations(new LinePutScript.LpsDocument(File.ReadAllText(file.FullName)), directory.FullName);
        }
    }

    /// <summary>
    /// 从 lps 声明里收录资源
    /// </summary>
    /// <param name="lps">声明表, 每行的 source 子项是相对 baseDirectory 的路径</param>
    /// <param name="baseDirectory">相对路径的基准目录</param>
    /// 与 Windows 版 Resources.AddSources 一致, 但不改动传进来的那份声明 ——
    /// 原实现是直接往 source 子项上拼路径的, 同一份声明加两次就会拼两遍.
    public void AddDeclarations(LinePutScript.ILPS lps, string baseDirectory = "")
    {
        foreach (var line in lps)
        {
            var source = line.Find("source");
            if (source == null)
                continue;
            sources[line.Name.ToLowerInvariant()] = string.IsNullOrEmpty(baseDirectory)
                ? source.Info
                : Path.Combine(baseDirectory, source.Info);
        }
    }

    /// <summary>
    /// 递归收录一个目录下的文件
    /// </summary>
    /// <param name="directory">目录</param>
    /// <param name="prefix">键的前缀</param>
    /// 与 Windows 版 CoreMOD.LoadFile 一致: 键**带**扩展名, 因为同名不同后缀的
    /// 文件是两样东西(图库就同时找 .zlps 和 .zip).
    public void AddFiles(DirectoryInfo directory, string prefix = "")
    {
        if (!directory.Exists)
            return;
        foreach (var file in directory.EnumerateFiles())
        {
            sources[prefix + file.Name.ToLowerInvariant()] = file.FullName;
        }
        foreach (var sub in directory.EnumerateDirectories())
        {
            AddFiles(sub, prefix + sub.Name.ToLowerInvariant() + "_");
        }
    }

    /// <summary>
    /// 直接登记一条映射
    /// </summary>
    public void Add(string name, string path) => sources[name.ToLowerInvariant()] = path;

    /// <summary>
    /// 查找资源路径
    /// </summary>
    /// <param name="name">资源名称</param>
    /// <param name="superior">找不到时退回的上级资源名</param>
    /// <returns>找不到返回 null</returns>
    /// 上级回退是给"某样东西没有专属图"准备的: 例如某个食物没配图就退回 food 这张
    /// 通用图, 而不是显示一个错误图标.
    public string? Find(string name, string? superior = null)
    {
        if (sources.TryGetValue(name, out var path))
            return path;
        if (superior != null && sources.TryGetValue(superior, out path))
            return path;
        return null;
    }
}
