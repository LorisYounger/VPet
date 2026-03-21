using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Panuon.WPF.UI;
using LinePutScript;
using Steamworks;

namespace VPet_Simulator.Windows
{
    /// <summary>
    /// MOD管理器类 - 修正版，解决异步、防抖、内存和扫描范围问题
    /// </summary>
    public class MODManager : IDisposable
    {
        private MainWindow mainWindow;
        private List<ModInfo> _cachedMods = new List<ModInfo>();
        private DateTime _lastRefreshTime = DateTime.MinValue;
        private readonly object _lockObject = new object();
        private volatile bool _isRefreshing = false;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private bool disposedValue = false;
        
        public MODManager(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // 释放托管状态(托管对象)
                    _semaphore?.Dispose();
                }

                // 释放非托管资源(非托管对象)并重写终结器
                // 将大型字段设置为 null
                disposedValue = true;
            }
        }

        // // 可以取消注释以下代码来使用终结器，仅当在以上 "Dispose(bool disposing)" 中拥有非托管资源时才使用
        // ~MODManager()
        // {
        //     // 不要更改此代码。请将清理代码放入 "Dispose(bool disposing)" 方法中
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入 "Dispose(bool disposing)" 方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 异步获取MOD列表 - 带缓存和防抖机制
        /// </summary>
        /// <param name="forceRefresh">是否强制刷新</param>
        /// <returns>MOD信息列表</returns>
        public async Task<List<ModInfo>> ListModAsync(bool forceRefresh = false)
        {
            // 防抖机制：如果距离上次刷新不到1.5秒，且不是强制刷新，则返回缓存
            if (!forceRefresh && (DateTime.Now - _lastRefreshTime).TotalSeconds < 1.5)
            {
                return new List<ModInfo>(_cachedMods); // 返回副本以防止并发问题
            }
            
            // 使用信号量确保只有一个线程可以执行刷新操作
            await _semaphore.WaitAsync();
            
            try
            {
                // 再次检查是否需要刷新（双重检查锁定模式）
                if (!forceRefresh && (DateTime.Now - _lastRefreshTime).TotalSeconds < 1.5)
                {
                    return new List<ModInfo>(_cachedMods);
                }
                
                // 异步执行磁盘扫描
                var modList = await Task.Run(() =>
                {
                    List<ModInfo> modList = new List<ModInfo>();
                    var modDir = new DirectoryInfo(mainWindow.ModPath);
                    
                    if (modDir.Exists)
                    {
                        foreach (var dir in modDir.EnumerateDirectories())
                        {
                            try
                            {
                                var modInfo = new ModInfo(dir);
                                // 检查此MOD是否已被加载
                                modInfo.IsLoaded = mainWindow.CoreMODs.Any(coreMod => coreMod.Name == modInfo.Name);
                                modList.Add(modInfo);
                            }
                            catch (Exception ex)
                            {
                                // 记录错误但继续处理其他MOD
                                Console.WriteLine($"警告: 加载MOD {dir.Name} 时出错: {ex.Message}");
                                continue; // 跳过这个有问题的MOD
                            }
                        }
                    }
                    
                    // 如果是Steam用户，也包含Workshop的MOD
                    if (mainWindow.IsSteamUser)
                    {
                        var workshop = mainWindow.Set["workshop"];
                        foreach (LinePutScript.Sub ws in workshop)
                        {
                            var workshopDir = new DirectoryInfo(ws.Name);
                            if (workshopDir.Exists)
                            {
                                foreach (var dir in workshopDir.EnumerateDirectories())
                                {
                                    try
                                    {
                                        var modInfo = new ModInfo(dir);
                                        // 检查此MOD是否已被加载
                                        modInfo.IsLoaded = mainWindow.CoreMODs.Any(coreMod => coreMod.Name == modInfo.Name);
                                        modList.Add(modInfo);
                                    }
                                    catch (Exception ex)
                                    {
                                        // 记录错误但继续处理其他MOD
                                        Console.WriteLine($"警告: 加载Workshop MOD {dir.Name} 时出错: {ex.Message}");
                                        continue; // 跳过这个有问题的MOD
                                    }
                                }
                            }
                        }
                    }
                    
                    return modList;
                });
                
                // 更新缓存
                lock (_lockObject)
                {
                    _cachedMods = modList;
                    _lastRefreshTime = DateTime.Now;
                }
                
                return new List<ModInfo>(modList); // 返回副本
            }
            catch (Exception ex)
            {
                // 返回之前的缓存
                return new List<ModInfo>(_cachedMods);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// 同步获取MOD列表（不推荐在UI线程使用）
        /// </summary>
        /// <returns>MOD信息列表</returns>
        public List<ModInfo> ListMod()
        {
            return ListModAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// 异步显示MOD列表对话框
        /// </summary>
        public async void ShowModListAsync()
        {
            // 防抖机制：检查是否正在显示或刚显示过
            if ((DateTime.Now - _lastRefreshTime).TotalMilliseconds < 500)
            {
                // 检查是否正在刷新
                if (_isRefreshing)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBoxX.Show("MOD列表正在刷新，请稍后再试", "MOD列表");
                    });
                    return;
                }
            }
            
            var modList = await ListModAsync();
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    if (modList.Count == 0)
                    {
                        MessageBoxX.Show("没有找到任何MOD", "MOD列表");
                        return;
                    }
                    
                    // 创建一个格式化的MOD列表字符串
                    var modListString = "MOD列表 (总数: " + modList.Count + ")\n\n";
                    
                    foreach (var mod in modList)
                    {
                        try
                        {
                            string status = mod.IsLoaded ? "[已加载]" : "[未加载]";
                            string tags = mod.Tag.Count > 0 ? " (" + string.Join(", ", mod.Tag) + ")" : "";
                            modListString += $"{status} {mod.Name}\n";
                            modListString += $"  作者: {mod.Author}\n";
                            modListString += $"  版本: {mod.Ver} (游戏版本: {mod.GameVer})\n";
                            modListString += $"  路径: {mod.Path}{tags}\n";
                            modListString += $"  简介: {mod.Intro}\n\n";
                        }
                        catch (Exception ex)
                        {
                            // 如果某个MOD信息有问题，跳过它并记录错误
                            Console.WriteLine($"警告: 处理MOD {mod?.Name ?? "unknown"} 时出错: {ex.Message}");
                            continue;
                        }
                    }
                    
                    // 使用Panuon.WPF.UI的MessageBoxX显示MOD列表
                    var scrollViewer = new ScrollViewer();
                    var textBlock = new TextBlock();
                    textBlock.Text = modListString;
                    textBlock.TextWrapping = TextWrapping.Wrap;
                    textBlock.Margin = new Thickness(10);
                    scrollViewer.Content = textBlock;
                    scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                    scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                    scrollViewer.MaxHeight = 500;
                    scrollViewer.MaxWidth = 800;
                    
                    var stackPanel = new StackPanel();
                    stackPanel.Children.Add(scrollViewer);
                    
                    MessageBoxX.Show(stackPanel, "MOD 列表", MessageBoxButton.OK, MessageBoxXButtonOptions.AnimateShow);
                }
                catch (Exception ex)
                {
                    MessageBoxX.Show($"显示MOD列表时发生错误: {ex.Message}", "错误");
                }
            });
        }
        
        /// <summary>
        /// 获取已加载的MOD数量 - 使用缓存
        /// </summary>
        /// <returns>已加载MOD数量</returns>
        public async Task<int> GetLoadedModCountAsync()
        {
            var modList = await ListModAsync();
            return modList.Count(mod => mod.IsLoaded);
        }
        
        /// <summary>
        /// 获取MOD总数 - 使用缓存
        /// </summary>
        /// <returns>MOD总数</returns>
        public async Task<int> GetTotalModCountAsync()
        {
            var modList = await ListModAsync();
            return modList.Count;
        }
        
        /// <summary>
        /// 检查MOD是否有效（存在info.lps文件）
        /// </summary>
        /// <param name="modPath">MOD路径</param>
        /// <returns>是否有效</returns>
        public bool IsValidMod(string modPath)
        {
            return File.Exists(Path.Combine(modPath, "info.lps"));
        }
        
        /// <summary>
        /// 获取特定类型的MOD - 使用缓存
        /// </summary>
        /// <param name="tag">MOD类型标签</param>
        /// <returns>符合条件的MOD列表</returns>
        public async Task<List<ModInfo>> GetModsByTagAsync(string tag)
        {
            var modList = await ListModAsync();
            return modList.Where(mod => mod.Tag.Contains(tag)).ToList();
        }
        
        /// <summary>
        /// 获取已加载的MOD列表 - 使用缓存
        /// </summary>
        /// <returns>已加载MOD列表</returns>
        public async Task<List<ModInfo>> GetLoadedModsAsync()
        {
            var modList = await ListModAsync();
            return modList.Where(mod => mod.IsLoaded).ToList();
        }
        
        /// <summary>
        /// 获取未加载的MOD列表 - 使用缓存
        /// </summary>
        /// <returns>未加载MOD列表</returns>
        public async Task<List<ModInfo>> GetUnloadedModsAsync()
        {
            var modList = await ListModAsync();
            return modList.Where(mod => !mod.IsLoaded).ToList();
        }
        
        /// <summary>
        /// 搜索MOD（按名称）- 使用缓存
        /// </summary>
        /// <param name="searchTerm">搜索关键词</param>
        /// <returns>匹配的MOD列表</returns>
        public async Task<List<ModInfo>> SearchModsAsync(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return await ListModAsync();
            
            var modList = await ListModAsync();
            return modList.Where(mod => 
                mod.Name.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0 ||
                mod.Author.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0 ||
                mod.Intro.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0
            ).ToList();
        }
        
        /// <summary>
        /// 刷新MOD缓存
        /// </summary>
        public async Task RefreshModsAsync()
        {
            _lastRefreshTime = DateTime.MinValue; // 使缓存失效
            await ListModAsync(true); // 强制刷新
        }
    }
}