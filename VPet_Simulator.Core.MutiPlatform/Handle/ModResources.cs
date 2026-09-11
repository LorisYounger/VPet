using System;
using System.Collections.Generic;
using System.IO;

namespace VPet_Simulator.Core.MutiPlatform;

/// <summary>
/// MOD 提供的数据资源
/// </summary>
/// 对应 Windows 版 VPet-Simulator.Windows.Interface/Resources.cs 里跟图片和食物
/// 有关的那部分. 那边的 ImageSources 直接存 BitmapImage(WPF 类型), 这里只存文件
/// 路径, 什么时候解码交给用到它的界面决定 —— 食物图有一百多张, 全部预解码没必要.
public class ModResources
{
    /// <summary>
    /// 所有食物
    /// </summary>
    public List<FoodItem> Foods { get; } = new List<FoodItem>();

    /// <summary>
    /// 扫描到的主题
    /// </summary>
    public List<ThemeInfo> Themes { get; } = new List<ThemeInfo>();

    /// <summary>
    /// 照片图库
    /// </summary>
    public PhotoStore Photos { get; } = new PhotoStore();

    /// <summary>
    /// MOD 提供的文件: 文件名到路径
    /// </summary>
    /// 与图片索引的区别是键**带**扩展名 —— 同名不同后缀的是两样东西
    /// (图库就同时找 .zlps 和 .zip)
    public Dictionary<string, string> Files { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 查找 MOD 提供的文件
    /// </summary>
    public string? FindFilePath(string name)
        => Files.TryGetValue(name, out var path) ? path : null;

    /// <summary>
    /// 递归收录一个目录下的文件
    /// </summary>
    internal void AddFiles(DirectoryInfo directory, string prefix = "")
    {
        foreach (var file in directory.EnumerateFiles())
            Files[prefix + file.Name.ToLowerInvariant()] = file.FullName;
        foreach (var sub in directory.EnumerateDirectories())
            AddFiles(sub, prefix + sub.Name.ToLowerInvariant() + "_");
    }

    /// <summary>
    /// 点击桌宠时可能说的话
    /// </summary>
    public List<ClickText> ClickTexts { get; } = new List<ClickText>();

    /// <summary>
    /// 饿了会说的话
    /// </summary>
    public List<LowText> LowFoodTexts { get; } = new List<LowText>();

    /// <summary>
    /// 渴了会说的话
    /// </summary>
    public List<LowText> LowDrinkTexts { get; } = new List<LowText>();

    /// <summary>
    /// 供玩家在对话框里选的话
    /// </summary>
    public List<SelectText> SelectTexts { get; } = new List<SelectText>();

    /// <summary>
    /// 图片名到文件路径的映射
    /// </summary>
    /// 键的拼法与 Windows 版 CoreMOD.LoadImage 一致: 全小写的文件名(去掉 .png),
    /// 嵌套的文件夹名按 文件夹_ 逐层加在前面. 例如 image/food/可乐.png 的键是
    /// food_可乐, image/food.png 的键就是 food.
    public Dictionary<string, string> Images { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 查找图片, 找不到就退回上级图片
    /// </summary>
    /// <param name="imageName">图片名称</param>
    /// <param name="superior">上级图片, 没有专属图片时用它</param>
    public string? FindImagePath(string imageName, string? superior = null)
    {
        if (Images.TryGetValue(imageName, out var path))
            return path;
        if (superior != null && Images.TryGetValue(superior, out path))
            return path;
        return null;
    }

    /// <summary>
    /// 递归收录一个 image 目录下的所有图片
    /// </summary>
    /// <param name="directory">目录</param>
    /// <param name="prefix">键的前缀</param>
    internal void AddImages(DirectoryInfo directory, string prefix = "")
    {
        foreach (var file in directory.EnumerateFiles("*.png"))
        {
            var key = prefix + Path.GetFileNameWithoutExtension(file.Name).ToLowerInvariant();
            // 后来的覆盖之前的, 与 Windows 版一致 —— MOD 靠这个替换官方素材
            Images[key] = file.FullName;
        }
        foreach (var sub in directory.EnumerateDirectories())
        {
            AddImages(sub, prefix + sub.Name.ToLowerInvariant() + "_");
        }
    }

    /// <summary>
    /// 给食物挂上图片路径
    /// </summary>
    /// 必须在所有 MOD 都扫完之后调用: 食物和图片可能来自不同的 MOD
    public void ResolveFoodImages()
    {
        foreach (var food in Foods)
        {
            food.ImagePath = FindImagePath("food_" + (food.Image ?? food.Name), "food");
        }
    }
}
