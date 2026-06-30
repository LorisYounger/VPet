using LinePutScript;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VPet_Simulator.Windows.Interface;

namespace VPet_Simulator.Windows
{
    /// <summary>
    /// MOD 管理器
    /// <para>职责: 以 <see cref="MainWindow.CoreMODs"/> 为唯一数据源, 提供 MOD 列表的查询/筛选,
    /// 并负责异步、带缓存、带防抖地扫描磁盘上"尚未加载"的 MOD 目录(本地 mod + Steam 创意工坊),
    /// 供运行时热启用使用.</para>
    /// <para>注意: 本类不重复解析已加载 MOD 的 info.lps, 元数据全部取自 <see cref="CoreMOD"/>, 避免与实际加载结果漂移.</para>
    /// </summary>
    internal class MODManager
    {
        private readonly MainWindow mw;

        /// <summary>
        /// 刷新防抖间隔, 期间内的重复刷新请求会被合并
        /// </summary>
        private static readonly TimeSpan RefreshDebounce = TimeSpan.FromMilliseconds(800);

        /// <summary>
        /// 保证同一时刻只有一次磁盘扫描在进行
        /// </summary>
        private readonly SemaphoreSlim refreshLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// 上次完成扫描的时间戳(UTC ticks). 与 <see cref="RefreshDebounce"/> 配合做防抖
        /// </summary>
        private long lastRefreshTicks = 0;

        /// <summary>
        /// 缓存: 磁盘上存在但尚未加载进 <see cref="MainWindow.CoreMODs"/> 的 MOD 目录.
        /// 不手动刷新时, 外部直接读这个缓存, 避免冗余 IO.
        /// </summary>
        private readonly List<DirectoryInfo> unloadedCache = new List<DirectoryInfo>();

        public MODManager(MainWindow mainWindow)
        {
            mw = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
        }

        /// <summary>
        /// 已加载的全部 MOD (唯一数据源, 直接来自 CoreMODs, 以 IModInfo 暴露)
        /// </summary>
        public IReadOnlyList<IModInfo> Mods => mw.CoreMODs;

        /// <summary>
        /// 磁盘上存在但尚未加载的 MOD 目录(缓存视图)
        /// </summary>
        public IReadOnlyList<DirectoryInfo> UnloadedMods => unloadedCache;

        /// <summary>
        /// 是否正在进行磁盘扫描
        /// </summary>
        public bool IsBusy => refreshLock.CurrentCount == 0;

        /// <summary>
        /// 按条件筛选已加载 MOD
        /// </summary>
        /// <param name="keyword">名称/作者/介绍关键字, 为空表示不过滤</param>
        /// <param name="onlyEnabled">仅显示已启用</param>
        /// <param name="onlyPlugin">仅显示包含代码插件</param>
        public IEnumerable<IModInfo> Filter(string keyword = null, bool onlyEnabled = false, bool onlyPlugin = false)
        {
            IEnumerable<IModInfo> query = mw.CoreMODs;
            if (onlyEnabled)
                query = query.Where(m => mw.Set.IsOnMod(m.Name));
            if (onlyPlugin)
                query = query.Where(m => m.Tag.Contains("plugin"));
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var k = keyword.Trim();
                query = query.Where(m =>
                    (m.Name?.Contains(k) ?? false) ||
                    (m.Author?.Contains(k) ?? false) ||
                    (m.Intro?.Contains(k) ?? false));
            }
            return query.ToList();
        }

        /// <summary>
        /// 收集所有应被纳入考虑的 MOD 根目录(本地 mod 文件夹 + Steam 创意工坊).
        /// 与实际加载逻辑保持一致: 无论是否 Steam 用户, 都读取 <c>Set["workshop"]</c>,
        /// 修正了"创意工坊只在 Steam 用户时扫描"导致的列表/实际加载不一致问题.
        /// </summary>
        private List<DirectoryInfo> CollectAllModDirectories()
        {
            var result = new List<DirectoryInfo>();
            try
            {
                var local = new DirectoryInfo(mw.ModPath);
                if (local.Exists)
                    result.AddRange(local.EnumerateDirectories());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MODManager: 枚举本地 mod 目录失败: {ex.Message}");
            }

            try
            {
                foreach (ISub ws in mw.Set["workshop"])
                {
                    try
                    {
                        var wsdir = new DirectoryInfo(ws.Name);
                        if (wsdir.Exists)
                            result.AddRange(wsdir.EnumerateDirectories());
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"MODManager: 枚举创意工坊目录 {ws.Name} 失败: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MODManager: 读取创意工坊设置失败: {ex.Message}");
            }
            return result;
        }

        /// <summary>
        /// 异步扫描磁盘, 找出"存在 info.lps 但尚未加载"的 MOD 目录, 刷新 <see cref="UnloadedMods"/> 缓存.
        /// <para>带防抖: <paramref name="force"/> 为 false 时, 距上次扫描不足 <see cref="RefreshDebounce"/> 则直接返回缓存.</para>
        /// </summary>
        /// <param name="force">true 表示忽略防抖间隔强制刷新(用户手动点击刷新时使用)</param>
        public async Task<IReadOnlyList<DirectoryInfo>> RefreshAsync(bool force = false)
        {
            if (!force)
            {
                var elapsed = TimeSpan.FromTicks(DateTime.UtcNow.Ticks - Interlocked.Read(ref lastRefreshTicks));
                if (elapsed < RefreshDebounce)
                    return unloadedCache;
            }

            // 合并并发刷新: 若已有扫描在进行, 等其完成后直接返回缓存, 不重复扫
            if (!await refreshLock.WaitAsync(0).ConfigureAwait(false))
            {
                await refreshLock.WaitAsync().ConfigureAwait(false);
                refreshLock.Release();
                return unloadedCache;
            }

            try
            {
                var found = await Task.Run(() =>
                {
                    var loadedPaths = new HashSet<string>(
                        mw.CoreMODs.Where(m => m.Path != null).Select(m => m.Path.FullName),
                        StringComparer.OrdinalIgnoreCase);

                    var list = new List<DirectoryInfo>();
                    foreach (var dir in CollectAllModDirectories())
                    {
                        try
                        {
                            if (loadedPaths.Contains(dir.FullName))
                                continue;
                            if (!File.Exists(dir.FullName + @"\info.lps"))
                                continue;
                            list.Add(dir);
                        }
                        catch (Exception ex)
                        {
                            // 单个坏 MOD 不影响整体扫描
                            Console.WriteLine($"MODManager: 扫描 MOD 目录 {dir.Name} 出错, 已跳过: {ex.Message}");
                        }
                    }
                    return list;
                }).ConfigureAwait(false);

                unloadedCache.Clear();
                unloadedCache.AddRange(found);
                Interlocked.Exchange(ref lastRefreshTicks, DateTime.UtcNow.Ticks);
                return unloadedCache;
            }
            finally
            {
                refreshLock.Release();
            }
        }
    }
}
