using LinePutScript;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VPet_Simulator.Unified.Services;

/// <summary>
/// 存档文件的命名、轮换与索引
/// </summary>
/// 从 Windows 版 MainWindow.Save() / LoadLatestSave() / winSaveManager 里抽出来的,
/// 命名规则一字未改:
///
///   存档   Saves/Save{前缀}_{递增编号}.lps
///   备份   Saves_BKP/Save{前缀}_{内容哈希 % 255 的十六进制}.lps
///   旧版   根目录的 Save.lps, 读过一次之后改名成 Save.bkp
///
/// 备份目录用内容哈希命名是有意的: 同样的内容只会占一个坑, 不同的内容自然散开,
/// 上限 255 个. 它**不参与**轮换删除 —— 这也是原样保留的行为.
public static class SaveCatalog
{
    /// <summary>
    /// 存档编号的起点, 与 Windows 版 Setting.SaveTimes 的默认值一致
    /// </summary>
    public const int FirstSaveTimes = 100000;

    /// <summary>
    /// 存档目录名
    /// </summary>
    public const string SaveFolderName = "Saves";

    /// <summary>
    /// 备份目录名
    /// </summary>
    public const string BackupFolderName = "Saves_BKP";

    /// <summary>
    /// 存档目录
    /// </summary>
    public static string SaveDirectory(string rootDirectory)
        => System.IO.Path.Combine(rootDirectory, SaveFolderName);

    /// <summary>
    /// 备份目录
    /// </summary>
    public static string BackupDirectory(string rootDirectory)
        => System.IO.Path.Combine(rootDirectory, BackupFolderName);

    /// <summary>
    /// 一份存档文件
    /// </summary>
    public readonly struct SaveFile
    {
        /// <summary>文件完整路径</summary>
        public string Path { get; }
        /// <summary>文件名里的编号; 备份目录里是哈希, 取不到时为 0</summary>
        public int Number { get; }
        /// <summary>最后写入时间</summary>
        public DateTime Time { get; }
        /// <summary>是否来自备份目录</summary>
        public bool IsBackup { get; }

        public SaveFile(string path, int number, DateTime time, bool isBackup)
        {
            Path = path;
            Number = number;
            Time = time;
            IsBackup = isBackup;
        }
    }

    /// <summary>
    /// 从文件名里取出编号
    /// </summary>
    /// 与 Windows 版一致: 取最后一个下划线之后、扩展名之前的那段. 解析不出来算 0,
    /// 这样坏名字的文件会排在最前面, 也就最先被轮换掉.
    public static int ParseNumber(string path)
    {
        var name = System.IO.Path.GetFileNameWithoutExtension(path);
        var tail = name.Split('_').Last();
        return int.TryParse(tail, out int i) ? i : 0;
    }

    /// <summary>
    /// 列出存档目录里的存档, 按编号从小到大
    /// </summary>
    public static List<SaveFile> List(string saveDirectory, string prefix)
        => Enumerate(saveDirectory, prefix, false).OrderBy(x => x.Number).ToList();

    /// <summary>
    /// 列出存档和备份目录里的全部存档, 按写入时间从新到旧
    /// </summary>
    /// 给存档管理器用: 玩家在那里看的是"什么时候存的", 不是编号
    public static List<SaveFile> ListAll(string saveDirectory, string backupDirectory, string prefix)
        => Enumerate(saveDirectory, prefix, false)
            .Concat(Enumerate(backupDirectory, prefix, true))
            .OrderByDescending(x => x.Time).ToList();

    /// <summary>
    /// 列出游戏根目录下两个存档目录里的全部存档, 按写入时间从新到旧
    /// </summary>
    public static List<SaveFile> ListAll(string rootDirectory, string prefix)
        => ListAll(SaveDirectory(rootDirectory), BackupDirectory(rootDirectory), prefix);

    private static IEnumerable<SaveFile> Enumerate(string directory, string prefix, bool isBackup)
    {
        if (!Directory.Exists(directory))
            yield break;
        foreach (var path in Directory.GetFiles(directory, $"Save{prefix}_*.lps"))
        {
            yield return new SaveFile(path, ParseNumber(path), File.GetLastWriteTime(path), isBackup);
        }
    }

    /// <summary>
    /// 取当前该用的存档编号
    /// </summary>
    /// 设置里记的编号可能比目录里实际存在的小(比如换了台机器拷回来的存档),
    /// 这时以目录里最大的为准, 免得新存档把旧的覆盖掉. 与 Windows 版
    /// LoadLatestSave 开头那段一致.
    public static int SyncSaveTimes(string saveDirectory, string prefix, int settingSaveTimes)
    {
        var files = List(saveDirectory, prefix);
        if (files.Count == 0)
            return settingSaveTimes;
        int last = files[files.Count - 1].Number;
        return settingSaveTimes < last ? last : settingSaveTimes;
    }

    /// <summary>
    /// 按编号从新到旧列出候选存档
    /// </summary>
    /// 调用方按顺序试着解析, 第一个能解析的就是要用的那份 —— 与 Windows 版
    /// LoadLatestSave 的倒序循环一致.
    public static List<string> LoadCandidates(string saveDirectory, string prefix)
        => List(saveDirectory, prefix).Select(x => x.Path).Reverse().ToList();

    /// <summary>
    /// 写入存档
    /// </summary>
    /// <param name="lps">存档文档</param>
    /// <param name="saveDirectory">存档目录</param>
    /// <param name="backupDirectory">备份目录</param>
    /// <param name="prefix">多开前缀</param>
    /// <param name="saveTimes">本次使用的编号, 调用方负责递增并存回设置</param>
    /// <param name="backupMaxNum">存档目录里最多保留几份</param>
    /// <returns>写出的存档文件路径</returns>
    /// 备份名里的哈希必须用 ILPS 自己的 GetHashCode(LPS_D 重写过, 是确定性的),
    /// 不能拿正文字符串去算 —— .NET Core 的 string.GetHashCode 每个进程都不一样,
    /// 那样每次启动都会在备份目录里堆出一份新文件.
    public static string Write(ILPS lps, string saveDirectory, string backupDirectory,
        string prefix, int saveTimes, int backupMaxNum)
    {
        var content = lps.ToString();
        if (content == null)
            throw new ArgumentException("存档内容为空", nameof(lps));

        Directory.CreateDirectory(saveDirectory);
        Directory.CreateDirectory(backupDirectory);

        // 先轮换再写: 与 Windows 版同序, 保证目录里的份数不会超
        var files = List(saveDirectory, prefix);
        while (files.Count > backupMaxNum)
        {
            File.Delete(files[0].Path);
            files.RemoveAt(0);
        }

        var savePath = System.IO.Path.Combine(saveDirectory, $"Save{prefix}_{saveTimes}.lps");
        // 备份按内容哈希命名: 同样的内容只占一个坑
        int hash = Math.Abs(lps.GetHashCode() % 255);
        var backupPath = System.IO.Path.Combine(backupDirectory, $"Save{prefix}_{hash:X}.lps");

        WriteAtomic(savePath, content);
        WriteAtomic(backupPath, content);
        return savePath;
    }

    /// <summary>
    /// 先写临时文件再替换
    /// </summary>
    /// 桌宠是长时间挂着的程序, 写到一半被强杀或断电的概率不低, 直接覆盖原文件会
    /// 留下半截存档. Windows 版是直接 WriteAllText 的, 这里更稳一点, 落到磁盘上的
    /// 文件名和内容完全一样.
    private static void WriteAtomic(string path, string content)
    {
        var temp = path + ".tmp";
        File.WriteAllText(temp, content);
        File.Move(temp, path, true);
    }

    /// <summary>
    /// 更老的存档目录名
    /// </summary>
    public const string LegacyFolderName = "BackUP";

    /// <summary>
    /// 把更老版本的 BackUP 目录并进 Saves
    /// </summary>
    /// <returns>本次是否真的搬了</returns>
    /// 目录不存在就直接跳过. Saves 还没有的话整个改名过去; 已经有了就逐个搬,
    /// 同名的以 Saves 里那份为准(旧的删掉), 最后把空目录删干净. 与 Windows 版
    /// MainWindow.xaml.cs 里"更新存档系统"那段逐字一致.
    public static bool MigrateLegacyFolder(string rootDirectory)
    {
        var legacy = System.IO.Path.Combine(rootDirectory, LegacyFolderName);
        if (!Directory.Exists(legacy))
            return false;

        var saveDir = SaveDirectory(rootDirectory);
        if (!Directory.Exists(saveDir))
        {
            Directory.Move(legacy, saveDir);
            return true;
        }

        foreach (var file in new DirectoryInfo(legacy).GetFiles())
        {
            var target = System.IO.Path.Combine(saveDir, file.Name);
            if (File.Exists(target))
                file.Delete();
            else
                file.MoveTo(target);
        }
        Directory.Delete(legacy, true);
        return true;
    }

    /// <summary>
    /// 把旧版根目录存档改名让位
    /// </summary>
    /// <returns>本次是否真的改了名</returns>
    /// 与 Windows 版一致: 老版本的存档在根目录叫 Save.lps, 读过一次之后改成
    /// Save.bkp, 之后就只认 Saves 目录了.
    public static bool MigrateLegacy(string rootDirectory)
    {
        var legacy = System.IO.Path.Combine(rootDirectory, "Save.lps");
        if (!File.Exists(legacy))
            return false;
        var backup = System.IO.Path.Combine(rootDirectory, "Save.bkp");
        if (File.Exists(backup))
            File.Delete(backup);
        File.Move(legacy, backup);
        return true;
    }

    /// <summary>
    /// Steam 云存档的路径前缀
    /// </summary>
    public const string CloudPrefix = "VPetCloud/";

    /// <summary>
    /// Steam 云存档的文件名
    /// </summary>
    /// 编号是"分钟数的十六进制", 与 Windows 版一致
    public static string CloudName(string prefix, DateTime time)
        => $"{CloudPrefix}Save{prefix}_{time.Ticks / 60000:X}.lps";

    /// <summary>
    /// 云存档名的匹配
    /// </summary>
    public static bool IsCloudName(string name, string prefix)
        => name.StartsWith($"{CloudPrefix}Save{prefix}_", StringComparison.Ordinal)
            && name.EndsWith(".lps", StringComparison.Ordinal);

    /// <summary>
    /// 从云存档名里取出存档时间, 取不出来返回 MinValue
    /// </summary>
    /// 名字里那段是十六进制的. Windows 版 MainWindow.Save 里用 int.TryParse 按十进制
    /// 读它, 除了纯数字的那少数几个之外一律读成 0, 于是"删最旧的"实际删的是任意一个.
    /// 存档管理器那边(winSaveManager.ParseSteamSaveTime)读的是十六进制, 两处对不上.
    /// 这里统一按十六进制读.
    public static DateTime ParseCloudTime(string name)
    {
        var tail = System.IO.Path.GetFileNameWithoutExtension(name).Split('_').Last();
        if (!long.TryParse(tail, System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture, out long minutes))
            return DateTime.MinValue;
        long ticks = minutes * 60000;
        if (ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks)
            return DateTime.MinValue;
        return new DateTime(ticks);
    }

    /// <summary>
    /// 挑出本前缀的云存档并按时间从旧到新排序
    /// </summary>
    /// 调用方从头上删起就是"先删最旧的"
    public static List<string> ListCloud(IEnumerable<string> names, string prefix)
        => names.Where(x => IsCloudName(x, prefix)).OrderBy(ParseCloudTime).ToList();

    /// <summary>
    /// 旧版根目录存档的路径, 不存在时返回 null
    /// </summary>
    public static string? FindLegacy(string rootDirectory)
    {
        var legacy = System.IO.Path.Combine(rootDirectory, "Save.lps");
        return File.Exists(legacy) ? legacy : null;
    }
}
