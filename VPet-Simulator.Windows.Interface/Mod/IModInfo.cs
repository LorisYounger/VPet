using LinePutScript.Converter;
using LinePutScript.Dictionary;
using LinePutScript.Localization.WPF;
using LinePutScript;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using VPet_Simulator.Core;

namespace VPet_Simulator.Windows.Interface;

/// <summary>
/// 模组信息接口
/// </summary>
public interface IModInfo
{
    /// <summary>
    /// 模组名称
    /// </summary>
    public string Name { get; }
    /// <summary>
    /// 模组作者
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
    /// 介绍
    /// </summary>
    public string Intro { get; }
    /// <summary>
    /// 模组路径
    /// </summary>
    public DirectoryInfo Path { get; }
    /// <summary>
    /// 游戏版本
    /// </summary>
    public int GameVer { get; }
    /// <summary>
    /// 模组版本
    /// </summary>
    public int Ver { get; }
    /// <summary>
    /// 模组标签
    /// </summary>
    public HashSet<string> Tag { get; }
}
