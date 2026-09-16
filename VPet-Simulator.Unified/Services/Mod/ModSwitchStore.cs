using LinePutScript;
using System;
using System.Collections.Generic;

namespace VPet_Simulator.Unified.Services;

/// <summary>
/// MOD 的启用/放行开关
/// </summary>
/// 从 Windows 版 CoreMOD.cs 末尾那组 Setting 扩展方法抽出来的, 行为一字未改:
/// 开关都存在 Setting.lps 的 onmod / passmod / msgmod 三行里, 子项名一律小写.
///
/// 两个平台共用这一份, 所以用户的 Setting.lps 在两边可以直接拷来拷去 ——
/// 在 Windows 上启用过的 MOD, 换到 Linux 还是启用的.
public static class ModSwitchStore
{
    /// <summary>
    /// 永远启用、不允许关掉的 MOD
    /// </summary>
    public static readonly IReadOnlyList<string> AlwaysOn = new[] { "Core", "PCat" };

    /// <summary>
    /// 这个 MOD 是否启用
    /// </summary>
    public static bool IsOn(ILPS settings, string modName)
    {
        foreach (var name in AlwaysOn)
        {
            if (name == modName)
                return true;
        }
        var line = settings.FindLine("onmod");
        if (line == null)
            return false;
        return line.Find(modName.ToLowerInvariant()) != null;
    }

    /// <summary>
    /// 启用这个 MOD
    /// </summary>
    public static void On(ILPS settings, string modName)
    {
        if (string.IsNullOrWhiteSpace(modName))
            return;
        settings.FindorAddLine("onmod").AddorReplaceSub(new Sub(modName.ToLowerInvariant()));
    }

    /// <summary>
    /// 停用这个 MOD
    /// </summary>
    public static void Off(ILPS settings, string modName)
    {
        settings.FindorAddLine("onmod").Remove(modName.ToLowerInvariant());
    }

    /// <summary>
    /// 玩家是否已经放行这个 MOD 的代码插件
    /// </summary>
    public static bool IsPassed(ILPS settings, string modName)
    {
        var line = settings.FindLine("passmod");
        if (line == null)
            return false;
        return line.Find(modName.ToLowerInvariant()) != null;
    }

    /// <summary>
    /// 放行这个 MOD 的代码插件
    /// </summary>
    public static void Pass(ILPS settings, string modName)
    {
        settings.FindorAddLine("passmod").AddorReplaceSub(new Sub(modName.ToLowerInvariant()));
    }

    /// <summary>
    /// 收回对这个 MOD 代码插件的放行
    /// </summary>
    public static void PassRemove(ILPS settings, string modName)
    {
        settings.FindorAddLine("passmod").Remove(modName.ToLowerInvariant());
    }

    /// <summary>
    /// 这个 MOD 的"含代码插件"提示是不是还没给玩家看过
    /// </summary>
    /// <returns>第一次问返回 true, 之后一直是 false</returns>
    /// 与 Windows 版 IsMSGMOD 一样是"问一次就记下"的语义: 调用本身有副作用.
    public static bool ShouldNotice(ILPS settings, string modName)
    {
        var line = settings.FindorAddLine("msgmod");
        if (line.GetBool(modName))
            return false;
        line.SetBool(modName, true);
        return true;
    }

    /// <summary>
    /// 记住这个 MOD 代码插件的指纹
    /// </summary>
    /// 跨平台侧独有: 非 Windows 上没有 Authenticode 可查, 放行名单是唯一的门.
    /// 把放行时的 dll 指纹记下来, MOD 更新之后重新问一次, 免得放行一次就永久有效.
    public static void SetPassHash(ILPS settings, string modName, string hash)
    {
        settings.FindorAddLine("passmodhash").AddorReplaceSub(new Sub(modName.ToLowerInvariant(), hash));
    }

    /// <summary>
    /// 取出上次放行时记下的指纹, 没有则返回 null
    /// </summary>
    public static string? GetPassHash(ILPS settings, string modName)
    {
        return settings.FindLine("passmodhash")?.Find(modName.ToLowerInvariant())?.Info;
    }
}
