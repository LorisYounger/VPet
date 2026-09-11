using LinePutScript;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using VPet_Simulator.Unified.Interface;
using VPet_Simulator.Unified.Services;

namespace VPet_Simulator.Core.MutiPlatform;

/// <summary>
/// 找出并加载按统一契约写的插件
/// </summary>
/// 与 Windows 版最大的不同是**信任怎么建立**: Windows 靠 Authenticode 签名, 非
/// Windows 上没有等价物, 所以这里退回到"玩家点头 + 记住指纹". 指纹变了(MOD 更新
/// 或被人换掉)就重新问一次 —— 放行一次永久有效等于没有防线.
public static class PluginLoader
{
    /// <summary>
    /// 一个候选插件
    /// </summary>
    public sealed class Candidate
    {
        /// <summary>所属 MOD 名</summary>
        public string ModName { get; }
        /// <summary>dll 完整路径</summary>
        public string Path { get; }
        /// <summary>不加载就判出来的类别</summary>
        public PluginKind Kind { get; }
        /// <summary>dll 内容的 SHA-256</summary>
        public string Hash { get; }

        internal Candidate(string modName, string path, PluginKind kind, string hash)
        {
            ModName = modName;
            Path = path;
            Kind = kind;
            Hash = hash;
        }
    }

    /// <summary>
    /// 扫描一个 MOD 的 plugin 目录, 判出每个 dll 是什么
    /// </summary>
    /// <param name="modName">MOD 名</param>
    /// <param name="pluginDirectory">plugin 目录</param>
    /// <param name="warnings">Windows 专用插件会往这里记一条</param>
    /// 全程只读 PE 头, 不执行目标里的任何代码 —— 加载一个 Windows 专用插件不只是
    /// 会抛异常, 它的模块初始化器会先跑起来, 在 Linux 上后果不可预期.
    public static List<Candidate> Scan(string modName, DirectoryInfo pluginDirectory, List<string> warnings)
    {
        var result = new List<Candidate>();
        if (!pluginDirectory.Exists)
            return result;

        ILPS? rules = null;
        var loadFile = System.IO.Path.Combine(pluginDirectory.FullName, "load.lps");
        if (File.Exists(loadFile))
        {
            try { rules = new LpsDocument(File.ReadAllText(loadFile)); }
            catch (Exception ex) { warnings.Add($"load.lps 读取失败: {ex.Message}"); }
        }

        bool anyLegacy = false;
        foreach (var file in pluginDirectory.EnumerateFiles("*.dll").OrderBy(x => x.Name, StringComparer.Ordinal))
        {
            if (PluginClassifier.ShouldSkip(file.Name, rules))
                continue;
            var kind = PluginClassifier.Classify(file.FullName);
            if (kind == PluginKind.Library)
                continue;
            if (kind == PluginKind.LegacyWindows)
            {
                anyLegacy = true;
                continue;
            }
            result.Add(new Candidate(modName, file.FullName, kind, HashFile(file.FullName)));
        }

        if (anyLegacy && result.Count == 0)
            warnings.Add("该 MOD 的代码插件是 Windows 专用的, 跨平台版跳过");
        else if (anyLegacy)
            warnings.Add("该 MOD 里有 Windows 专用的代码插件, 那一部分跨平台版跳过");
        return result;
    }

    /// <summary>
    /// 这个插件玩家放行过吗
    /// </summary>
    /// <param name="settings">设置文档</param>
    /// <param name="candidate">候选插件</param>
    /// 放行名单与 Windows 版共用同一个 passmod 行, 所以在 Windows 上放行过的 MOD
    /// 换到 Linux 上不用再问一遍. 指纹是跨平台侧独有的一道: Windows 有签名可查,
    /// 这边只能靠它认出"还是当初放行的那个文件".
    public static bool IsTrusted(ILPS settings, Candidate candidate)
    {
        if (!ModSwitchStore.IsPassed(settings, candidate.ModName))
            return false;
        var known = ModSwitchStore.GetPassHash(settings, candidate.ModName);
        // 从 Windows 那边拷过来的设置里没有指纹, 这时认签名放行的结果并补记一份
        if (known == null)
        {
            ModSwitchStore.SetPassHash(settings, candidate.ModName, candidate.Hash);
            return true;
        }
        return string.Equals(known, candidate.Hash, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 记下玩家的放行
    /// </summary>
    public static void Trust(ILPS settings, Candidate candidate)
    {
        ModSwitchStore.Pass(settings, candidate.ModName);
        ModSwitchStore.SetPassHash(settings, candidate.ModName, candidate.Hash);
    }

    /// <summary>
    /// 加载一个插件 dll, 取出里面的插件类型
    /// </summary>
    /// <returns>直接派生自 UnifiedPlugin 的类型; 加载不了则是空表</returns>
    /// 判据与 Windows 版逐字一致: 只认直接派生一层.
    public static List<Type> LoadTypes(Candidate candidate, List<string> warnings)
    {
        try
        {
            var context = new PluginLoadContext(candidate.ModName, candidate.Path);
            var assembly = context.LoadFromAssemblyPath(candidate.Path);
            Type?[] types;
            try
            {
                types = assembly.GetExportedTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                // 一部分类型加载失败不该让整个插件报废: 能认出来的照样用
                types = ex.Types;
                warnings.Add($"{System.IO.Path.GetFileName(candidate.Path)} 有部分类型加载失败, 已跳过那些");
            }
            return types.Where(x => x != null && x.BaseType == typeof(UnifiedPlugin))
                .Select(x => x!).ToList();
        }
        catch (Exception ex)
        {
            warnings.Add($"{System.IO.Path.GetFileName(candidate.Path)} 加载失败: {ex.Message}");
            return new List<Type>();
        }
    }

    private static string HashFile(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            return Convert.ToHexString(SHA256.HashData(stream));
        }
        catch (Exception)
        {
            // 读不了就给一个不可能与真指纹相等的值, 于是永远问一次
            return string.Empty;
        }
    }
}
