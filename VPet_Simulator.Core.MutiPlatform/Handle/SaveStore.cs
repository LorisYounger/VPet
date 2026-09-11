using LinePutScript;
using LinePutScript.Dictionary;
using System;
using System.Collections.Generic;
using System.IO;
using VPet_Simulator.Unified.Services;
using VPet_Simulator.Windows.Interface;

namespace VPet_Simulator.Core.MutiPlatform;

/// <summary>
/// 存档读写
/// </summary>
/// 读写的是与 Windows 版**完全相同**的 GameSave_v2 容器: 除了桌宠数值(vpet 行),
/// 里面还有统计(statistics 行)、背包、图库解锁、日程表、吃腻度这些放在 Data 里的
/// 自定义行, 以及一行防作弊哈希.
///
/// 这一点是硬要求. 之前这里只写一个 vpet 行, 后果是: 玩家把 Windows 的存档拷过来
/// 玩一次再拷回去, 统计/背包/图库/日程全没了, 而且没有任何报错.
///
/// 文件命名、轮换、备份规则全部走共享后端的 SaveCatalog, 与 Windows 版同一份实现.
public static class SaveStore
{
    /// <summary>
    /// 存档目录
    /// </summary>
    public static string SaveDirectory => Path.Combine(AppPaths.DataRoot, "Saves");

    /// <summary>
    /// 备份目录
    /// </summary>
    /// 按内容哈希命名, 同样的内容只占一个坑, 上限 255 份, 不参与轮换删除 ——
    /// 与 Windows 版一致.
    public static string BackupDirectory => Path.Combine(AppPaths.DataRoot, "Saves_BKP");

    /// <summary>
    /// 读取最新的存档
    /// </summary>
    /// <param name="settings">设置, 用来同步存档编号</param>
    /// <param name="prefix">多开前缀</param>
    /// <param name="warnings">读取过程中的问题描述</param>
    /// <returns>没有可用存档时返回 null</returns>
    /// 与 Windows 版 LoadLatestSave 同序: 先把设置里的编号同步到目录里实际最大的
    /// (免得新存档覆盖旧的), 再从新到旧逐个试, 第一个能解析的就用它.
    public static GameSave_v2? LoadLatest(AppSettings settings, string prefix, out List<string> warnings)
    {
        warnings = new List<string>();

        // 老版本的跨平台存档在 Saves/Save.lps, 迁一次到新命名
        MigrateSingleFile(prefix, warnings);

        settings.SaveTimes = SaveCatalog.SyncSaveTimes(SaveDirectory, prefix, settings.SaveTimes);

        foreach (var path in SaveCatalog.LoadCandidates(SaveDirectory, prefix))
        {
            var save = TryLoad(path, warnings);
            if (save != null)
                return save;
        }

        // 存档目录里没有能用的, 再翻备份目录 —— Windows 版没有这一步, 但备份就是
        // 为了这种时候存在的, 白白放着不用没道理
        foreach (var file in SaveCatalog.ListAll(BackupDirectory, BackupDirectory, prefix))
        {
            var save = TryLoad(file.Path, warnings);
            if (save != null)
            {
                warnings.Add($"主存档都读不出来, 已回退到备份 {Path.GetFileName(file.Path)}");
                return save;
            }
        }
        return null;
    }

    /// <summary>
    /// 试着读一份存档文件
    /// </summary>
    /// <returns>读不出来或内容不合理时返回 null</returns>
    /// 与 Windows 版 SavesLoad 的判定一致: 全零的存档视为损坏, 数值溢出的自动回正.
    private static GameSave_v2? TryLoad(string path, List<string> warnings)
    {
        try
        {
            var text = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(text))
                return null;
            var save = new GameSave_v2(new LPS_D(text));
            if (save.GameSave == null)
                return null;
            // 数据全是 0, 多半是写坏了
            if (save.GameSave.Money == 0 && save.GameSave.Likability == 0 && save.GameSave.Exp == 0
                && save.GameSave.StrengthDrink == 0 && save.GameSave.StrengthFood == 0)
                return null;

            FixOverflow(save, warnings);
            return save;
        }
        catch (Exception ex)
        {
            warnings.Add($"{Path.GetFileName(path)} 读取失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 数值溢出的回正与补偿
    /// </summary>
    /// 与 Windows 版 SavesLoad 里那段逐行对应. 经验和金钱都是 long, 玩到溢出会变成
    /// 极大的负数, 直接回正并按游玩时长补偿回来.
    private static void FixOverflow(GameSave_v2 save, List<string> warnings)
    {
        if (save.GameSave.Exp < -1000000000)
        {
            save.GameSave.Exp = 1000000;
            save.Data[(gbol)"round"] = true;
            warnings.Add("检测到经验值溢出, 已自动回正");
        }
        if (save.GameSave.Money < -1000000000)
        {
            save.GameSave.Money = 100000;
            warnings.Add("检测到金钱溢出, 已自动回正");
        }

        if (save.Data[(gbol)"round"])
        {//根据游玩时间补偿数据溢出
            warnings.Add("以前遭遇过数据溢出, 已根据游戏时长把数值加回来");
            var totalhour = (int)(save.Statistics[(gint)"stat_total_time"] / 3600);//总计游玩时间/小时
            if (totalhour < 500)
            {
                save.GameSave.Exp += totalhour * 200;
            }
            else
            {
                double lm = Math.Sqrt(totalhour / 500);
                save.GameSave.LevelMax += (int)lm;
                save.GameSave.Exp += (totalhour % 500 + (lm - (int)lm) * 500) * 200;
            }
            save.GameSave.LikabilityMax += totalhour / 10;
            save.Data[(gbol)"round"] = false;
        }
    }

    /// <summary>
    /// 保存
    /// </summary>
    /// <param name="save">存档容器</param>
    /// <param name="settings">设置, 编号会递增并写回</param>
    /// <param name="prefix">多开前缀</param>
    /// 编号递增和写回设置放在一起, 与 Windows 版 Setting.SaveTimesPP 的语义一致:
    /// 取一次就加一次. 调用方保存完存档要记得把设置也存了, 否则下次启动编号会倒退,
    /// 新存档就把上一份覆盖掉了.
    public static void Save(GameSave_v2 save, AppSettings settings, string prefix)
    {
        settings.SaveTimes += 1;
        SaveCatalog.Write(save.ToLPS(), SaveDirectory, BackupDirectory, prefix,
            settings.SaveTimes, settings.BackupSaveMaxNum);
    }

    /// <summary>
    /// 把老版本跨平台存档(Saves/Save.lps)迁到新命名
    /// </summary>
    /// 第一阶段的跨平台版把存档写成了固定的 Saves/Save.lps, 那个名字 Windows 版
    /// 不认. 这里读一次把它转成 Save{前缀}_{编号}.lps, 之后就走统一的命名了.
    private static void MigrateSingleFile(string prefix, List<string> warnings)
    {
        var legacy = Path.Combine(SaveDirectory, "Save.lps");
        if (!File.Exists(legacy))
            return;
        try
        {
            var text = File.ReadAllText(legacy);
            var document = new LPS_D(text);
            var line = document.FindLine("vpet");
            if (line == null)
                return;

            // 老格式只有一个 vpet 行, 用它拼一个完整的容器
            var save = new GameSave_v2(new LPS_D(text));
            Directory.CreateDirectory(SaveDirectory);
            Directory.CreateDirectory(BackupDirectory);
            SaveCatalog.Write(save.ToLPS(), SaveDirectory, BackupDirectory, prefix,
                SaveCatalog.FirstSaveTimes + 1, 50);
            File.Move(legacy, legacy + ".bkp", true);
            warnings.Add("已把旧版单文件存档迁到新的命名规则");
        }
        catch (Exception ex)
        {
            warnings.Add($"旧版存档迁移失败: {ex.Message}");
        }
    }
}
