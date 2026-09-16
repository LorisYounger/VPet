using LinePutScript;
using System;
using System.Security.Cryptography;
using System.Text;

namespace VPet_Simulator.Unified.Services;

/// <summary>
/// 存档防作弊哈希
/// </summary>
/// 从 Windows 版 VPet-Simulator.Windows.Interface/GameSave_v2.cs 里抽出来的.
/// 两个平台的 GameSave_v2 都调这里, 保证同一份存档在哪边算出来的哈希都一样 ——
/// 玩家在 Linux 上玩一局回到 Windows, 不该因为算法有出入就丢掉徽章.
///
/// 注意: 哈希覆盖的是**去掉 hash 行之后**整份文档的 ToString(). 所以行的顺序、
/// 数字的格式化方式都会影响结果. 跨平台侧要把区域文化钉成 Invariant, 否则
/// 德语环境下小数点是逗号, 哈希就对不上了.
public static class SaveHash
{
    /// <summary>
    /// 当前哈希版本
    /// </summary>
    public const int CurrentVersion = 2;

    /// <summary>
    /// 计算存档哈希
    /// </summary>
    /// <param name="lps">已经去掉 hash 行的存档文档</param>
    public static long Compute(ILPS lps) => Sub.GetHashCode(lps.ToString()!);

    /// <summary>
    /// 把哈希写进存档
    /// </summary>
    /// <param name="lps">存档文档, 里面已有的 hash 行会被替换</param>
    /// <param name="hashCheck">当前存档是否还通过防作弊检查</param>
    /// 与 Windows 版 GameSave_v2.ToLPS 逐行对应: 检查已经失效时写 -1, 这个值永远
    /// 算不出来, 所以一旦失去就再也回不来 —— 这是设计如此, 不是 bug.
    public static void Write(ILPS lps, bool hashCheck)
    {
        lps.Remove("hash");
        if (hashCheck)
        {
            lps[(gi64)"hash"] = Compute(lps);
        }
        else
        {
            lps[(gint)"hash"] = -1;
        }
        lps["hash"][(gint)"ver"] = CurrentVersion;
    }

    /// <summary>
    /// 校验存档哈希
    /// </summary>
    /// <param name="lps">完整的存档文档, 校验过程中 hash 行会被摘掉</param>
    /// <param name="version">读到的哈希版本</param>
    /// <returns>是否通过</returns>
    /// 与 Windows 版 GameSave_v2.load 逐行对应, 包括 ver 2 之前那条 MD5 的回退路径:
    /// 老存档是用 MD5 算的, 先按 MD5 试一次, 不过再按新算法试一次.
    public static bool Verify(ILPS lps, out int version)
    {
        long hash = lps.GetInt64("hash");
        version = lps["hash"].GetInt("ver");
        if (!lps.Remove("hash"))
            return false;

        if (version == CurrentVersion)
            return Compute(lps) == hash;

        try
        {
            using (MD5 md5 = MD5.Create())
            {
                if (BitConverter.ToInt64(md5.ComputeHash(Encoding.UTF8.GetBytes(lps.ToString()!)), 0) == hash)
                    return true;
            }
        }
        catch (Exception)
        {
            return false;
        }
        return Compute(lps) == hash;
    }
}
