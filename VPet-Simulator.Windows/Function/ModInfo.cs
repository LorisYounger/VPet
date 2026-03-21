using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LinePutScript;

namespace VPet_Simulator.Windows
{
    /// <summary>
    /// MOD信息类 - 轻量级模型，仅包含基础属性
    /// </summary>
    public class ModInfo
    {
        /// <summary>
        /// MOD名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// MOD路径
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// 游戏版本要求
        /// </summary>
        public int GameVer { get; set; }

        /// <summary>
        /// MOD标签
        /// </summary>
        public HashSet<string> Tag { get; set; } = new HashSet<string>();

        /// <summary>
        /// 是否已加载
        /// </summary>
        public bool IsLoaded { get; set; }

        /// <summary>
        /// MOD作者
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// MOD简介
        /// </summary>
        public string Intro { get; set; }

        /// <summary>
        /// MOD版本
        /// </summary>
        public int Ver { get; set; }

        /// <summary>
        /// 作者ID
        /// </summary>
        public long AuthorID { get; set; }

        /// <summary>
        /// Steam Item ID
        /// </summary>
        public ulong ItemID { get; set; }

        /// <summary>
        /// 初始化MOD信息
        /// </summary>
        /// <param name="directory">MOD目录</param>
        public ModInfo(DirectoryInfo directory)
        {
            Path = directory.FullName;
            Name = directory.Name; // 默认名称为目录名，如果info.lps解析失败
            
            try
            {
                var infoFile = Path.Combine(directory.FullName, "info.lps");
                if (File.Exists(infoFile))
                {
                    var modlps = new LPS(File.ReadAllText(infoFile));
                    
                    // 解析基本信息
                    Name = modlps.FindLine("vupmod").Info;
                    Intro = modlps.FindLine("intro").Info ?? "";
                    GameVer = modlps.FindSub("gamever").InfoToInt;
                    Ver = modlps.FindSub("ver").InfoToInt;
                    Author = modlps.FindSub("author").Info?.Split('[').FirstOrDefault() ?? "";
                    
                    if (modlps.FindLine("authorid") != null)
                        AuthorID = modlps.FindLine("authorid").InfoToInt64;
                    else
                        AuthorID = 0;
                        
                    if (modlps.FindLine("itemid") != null)
                        ItemID = Convert.ToUInt64(modlps.FindLine("itemid").info);
                    else
                        ItemID = 0;

                    // 分析MOD类型标签
                    foreach (var subDir in directory.EnumerateDirectories())
                    {
                        var dirName = subDir.Name.ToLowerInvariant();
                        switch (dirName)
                        {
                            case "theme":
                                Tag.Add("theme");
                                break;
                            case "pet":
                                Tag.Add("pet");
                                break;
                            case "food":
                                Tag.Add("food");
                                break;
                            case "image":
                                Tag.Add("image");
                                break;
                            case "file":
                                Tag.Add("file");
                                break;
                            case "photo":
                                Tag.Add("photo");
                                break;
                            case "text":
                                Tag.Add("text");
                                break;
                            case "lang":
                                Tag.Add("lang");
                                break;
                            case "plugin":
                                Tag.Add("plugin");
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 解析失败时记录错误但不抛出异常，避免单个MOD影响整体扫描
                Name = $"{directory.Name} (解析失败)";
            }
        }

        /// <summary>
        /// 静态方法：从DirectoryInfo创建ModInfo对象
        /// </summary>
        /// <param name="directory">MOD目录</param>
        /// <returns>ModInfo对象</returns>
        public static ModInfo FromDirectory(DirectoryInfo directory)
        {
            return new ModInfo(directory);
        }
    }
}