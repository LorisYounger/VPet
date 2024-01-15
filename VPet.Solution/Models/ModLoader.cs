using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LinePutScript;
using LinePutScript.Converter;
using LinePutScript.Dictionary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;
using System.Windows.Media.Imaging;

namespace VPet.Solution.Models;

/// <summary>
/// 模组加载器
/// </summary>
public class ModLoader
{
    /// <summary>
    /// 名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 作者
    /// </summary>
    public string Author { get; }

    /// <summary>
    /// 如果是上传至Steam,则为SteamUserID
    /// </summary>
    public long AuthorID { get; }

    /// <summary>
    /// 上传至Steam的ItemID
    /// </summary>
    public ulong ItemID { get; }

    /// <summary>
    /// 简介
    /// </summary>
    public string Intro { get; }

    /// <summary>
    /// 模组路径
    /// </summary>
    public string ModPath { get; }

    /// <summary>
    /// 支持的游戏版本
    /// </summary>
    public int GameVer { get; }

    /// <summary>
    /// 版本
    /// </summary>
    public int Ver { get; }

    /// <summary>
    /// 标签
    /// </summary>
    public HashSet<string> Tags { get; } = new();

    /// <summary>
    /// 缓存数据
    /// </summary>
    public DateTime CacheDate { get; } = DateTime.MinValue;

    public BitmapImage? Image { get; } = null;

    public ModLoader(string path)
    {
        ModPath = path;
        var modlps = new LpsDocument(File.ReadAllText(Path.Combine(path + @"\info.lps")));
        Name = modlps.FindLine("vupmod").Info;
        Intro = modlps.FindLine("intro").Info;
        GameVer = modlps.FindSub("gamever").InfoToInt;
        Ver = modlps.FindSub("ver").InfoToInt;
        Author = modlps.FindSub("author").Info.Split('[').First();
        if (modlps.FindLine("authorid") != null)
            AuthorID = modlps.FindLine("authorid").InfoToInt64;
        else
            AuthorID = 0;
        if (modlps.FindLine("itemid") != null)
            ItemID = Convert.ToUInt64(modlps.FindLine("itemid").info);
        else
            ItemID = 0;
        CacheDate = modlps.GetDateTime("cachedate", DateTime.MinValue);
        var imagePath = Path.Combine(path, "icon.png");
        if (File.Exists(imagePath))
        {
            try
            {
                Image = Utils.LoadImageToStream(imagePath);
            }
            catch { }
        }
        foreach (var dir in Directory.EnumerateDirectories(path))
        {
            switch (dir.ToLower())
            {
                case "pet":
                    //宠物模型
                    Tags.Add("pet");
                    break;
                case "food":
                    Tags.Add("food");
                    break;
                case "image":
                    Tags.Add("image");
                    break;
                case "text":
                    Tags.Add("text");
                    break;
                case "lang":
                    Tags.Add("lang");
                    break;
            }
        }
    }
}
