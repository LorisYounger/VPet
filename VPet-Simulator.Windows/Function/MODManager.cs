using LinePutScript;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace VPet_Simulator.Windows
{
    /// <summary>
    /// MOD 管理器
    /// <para>以 <see cref="MainWindow.CoreMODs"/> 为唯一数据源, 负责异步扫描磁盘上"尚未加载"的 MOD 目录
    /// (本地 mod 文件夹 + Steam 创意工坊), 供运行时热启用使用. 不重复解析已加载 MOD 的 info.lps.</para>
    /// </summary>
    internal class MODManager
    {
        private readonly MainWindow mw;

        public MODManager(MainWindow mainWindow)
        {
            mw = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
        }

        /// <summary>
        /// 收集所有 MOD 根目录(本地 mod 文件夹 + Steam 创意工坊).
        /// 与实际加载逻辑保持一致: 无论是否 Steam 用户, 都读取 <c>Set["workshop"]</c>.
        /// </summary>
        private List<DirectoryInfo> CollectAllModDirectories()
        {
            var result = new List<DirectoryInfo>();
            var local = new DirectoryInfo(mw.ModPath);
            if (local.Exists)
                result.AddRange(local.EnumerateDirectories());
            foreach (ISub ws in mw.Set["workshop"])
            {
                var wsdir = new DirectoryInfo(ws.Name);
                if (wsdir.Exists)
                    result.AddRange(wsdir.EnumerateDirectories());
            }
            return result;
        }

        /// <summary>
        /// 异步扫描磁盘, 返回"存在 info.lps 但尚未加载"的 MOD 目录.
        /// 并发由调用方(刷新时禁用按钮)保证, 此处不做额外防抖.
        /// </summary>
        public Task<List<DirectoryInfo>> ScanUnloadedModsAsync() => Task.Run(() =>
        {
            var loadedPaths = new HashSet<string>(
                mw.CoreMODs.Where(m => m.Path != null).Select(m => m.Path.FullName),
                StringComparer.OrdinalIgnoreCase);

            return CollectAllModDirectories()
                .Where(dir => !loadedPaths.Contains(dir.FullName)
                              && File.Exists(dir.FullName + @"\info.lps"))
                .ToList();
        });
    }
}
