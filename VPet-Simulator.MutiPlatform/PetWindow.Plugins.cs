using System;
using System.Collections.Generic;
using System.Linq;
using VPet_Simulator.Core.MutiPlatform;
using VPet_Simulator.Core.MutiPlatform.Display;
using LinePutScript.Localization;
using VPet_Simulator.Core.MutiPlatform.Display.Shell;
using VPet_Simulator.Unified.Interface;
using VPet_Simulator.Unified.Services;

namespace VPet_Simulator.MutiPlatform;

/// <summary>
/// 统一契约插件的加载与生命周期
/// </summary>
/// 调用时机与 Windows 侧一一对应, 这是"同一个 dll 两边跑"的前提:
///
///   构造          MOD 扫完之后、读档之前  —— 插件在这里注册物品创建器
///   LoadPlugin    桌宠控件挂上去之后      —— 存档已读好, 可以挂事件、加菜单
///   LoadDIY       紧随其后, 可反复调用
///   GameLoaded    动画就绪、桌宠跑起来之后
///   Save          每次存档
///   EndGame       窗口关闭最开头
public partial class PetWindow
{
    /// <summary>
    /// 已加载的统一契约插件宿主
    /// </summary>
    private readonly List<UnifiedHost> unifiedHosts = new List<UnifiedHost>();

    /// <summary>
    /// 已加载的统一契约插件
    /// </summary>
    internal IEnumerable<UnifiedPlugin> UnifiedPlugins => unifiedHosts.Select(x => x.Plugin);

    /// <summary>
    /// 扫描并实例化统一契约插件
    /// </summary>
    /// 在读档之前调用: 插件构造函数里注册的物品创建器正好赶得上存档里的自定义物品。
    ///
    /// 不加载 Windows 专用插件, 也不静默跳过 —— 那种 dll 一旦真的加载起来, 它的
    /// 模块初始化器会先跑, 在 Linux 上后果不可预期。
    private void LoadUnifiedPlugins()
    {
        foreach (var mod in HostMods)
        {
            if (mod.PluginDirectory == null)
                continue;
            if (!ModSwitchStore.IsOn(settings, mod.Name))
            {
                Log($"MOD [{mod.Name}] 没启用, 代码插件跳过");
                continue;
            }

            var warnings = new List<string>();
            var candidates = PluginLoader.Scan(mod.Name, mod.PluginDirectory, warnings);
            foreach (var warning in warnings)
                Log($"MOD [{mod.Name}] {warning}");
            if (candidates.Count == 0)
                continue;

            foreach (var candidate in candidates)
            {
                if (!PluginLoader.IsTrusted(settings, candidate))
                {
                    // 非 Windows 上没有 Authenticode 可查, 放行名单是唯一的门.
                    // 问一次玩家, 答应了就记下指纹 —— 下次 MOD 更新(指纹变了)会再问一遍,
                    // 放行一次永久有效等于没有防线。
                    if (!AskTrust(mod, candidate))
                    {
                        Log($"MOD [{mod.Name}] 的代码插件玩家没放行, 已跳过");
                        continue;
                    }
                    PluginLoader.Trust(settings, candidate);
                    SaveSettings();
                    Log($"MOD [{mod.Name}] 的代码插件已被玩家放行");
                }

                var loadWarnings = new List<string>();
                var types = PluginLoader.LoadTypes(candidate, loadWarnings);
                foreach (var type in types)
                {
                    var host = UnifiedHost.Create(this, mod, type, loadWarnings);
                    if (host != null)
                    {
                        unifiedHosts.Add(host);
                        Log($"MOD [{mod.Name}] 已加载插件 {host.Plugin.PluginName}");
                    }
                }
                //加载和构造两个阶段的问题一起报, 免得漏掉构造期新增的
                foreach (var warning in loadWarnings)
                    Log($"MOD [{mod.Name}] {warning}");
            }
        }
    }

    /// <summary>
    /// 问玩家要不要放行一个代码插件
    /// </summary>
    /// <returns>玩家答应了返回 true</returns>
    /// 这里刻意同步等待: 插件必须在读档之前实例化, 而读档没法一边等玩家一边进行.
    /// 桌宠启动时本来就停在这一步, 阻塞不会让别的事情停下来。
    private bool AskTrust(ModLoader mod, PluginLoader.Candidate candidate)
    {
        var known = ModSwitchStore.GetPassHash(settings, mod.Name);
        var fingerprint = candidate.Hash.Length > 16 ? candidate.Hash.Substring(0, 16) : candidate.Hash;
        var text = LocalizeCore.Translate(
            "MOD「{0}」带有代码插件。\n代码插件能做的事和游戏本身一样多, 请只放行你信任来源的 MOD。\n\n文件: {1}\n指纹: {2}\n\n要加载它吗?",
            mod.Name, System.IO.Path.GetFileName(candidate.Path), fingerprint);
        if (known != null)
            text = LocalizeCore.Translate("这个 MOD 的代码插件更新过了, 需要重新确认。") + "\n\n" + text;
        try
        {
            return DialogService.ConfirmAsync(text, LocalizeCore.Translate("是否加载代码插件"), this)
                .GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            //问不出来就当没放行 —— 出错时宁可不加载
            Log($"放行询问出错, 按未放行处理: {ex.Message}");
            return false;
        }
    }
    /// <summary>
    /// 存档已读好, 桌宠控件也挂上去了
    /// </summary>
    private void RaiseUnifiedLoadPlugin()
    {
        foreach (var host in unifiedHosts)
        {
            try { host.OnLoadPlugin(); }
            catch (Exception ex) { Log($"插件 {host.Plugin.PluginName} 初始化出错: {ex}"); }
        }
        RaiseUnifiedLoadDIY();
    }

    /// <summary>
    /// 重建自定菜单
    /// </summary>
    /// 会被反复调用, 每次调用前宿主都清空过自定菜单
    internal void RaiseUnifiedLoadDIY()
    {
        foreach (var host in unifiedHosts)
        {
            try { host.Plugin.LoadDIY(); }
            catch (Exception ex) { Log($"插件 {host.Plugin.PluginName} 自定菜单出错: {ex}"); }
        }
    }

    /// <summary>
    /// 动画就绪、桌宠跑起来了
    /// </summary>
    private void RaiseUnifiedGameLoaded()
    {
        foreach (var host in unifiedHosts)
        {
            try { host.Plugin.GameLoaded(); }
            catch (Exception ex) { Log($"插件 {host.Plugin.PluginName} 启动完成回调出错: {ex}"); }
        }
    }

    /// <summary>
    /// 存档
    /// </summary>
    private void RaiseUnifiedSave()
    {
        foreach (var host in unifiedHosts)
        {
            try { host.Plugin.Save(); }
            catch (Exception ex) { Log($"插件 {host.Plugin.PluginName} 保存出错: {ex}"); }
        }
    }

    /// <summary>
    /// 退出
    /// </summary>
    /// 每个插件单独兜一层: 一个插件抛异常不该拖住其他插件的清理
    private void RaiseUnifiedEndGame()
    {
        foreach (var host in unifiedHosts)
        {
            try { host.OnEndGame(); }
            catch (Exception ex) { Log($"插件 {host.Plugin.PluginName} 退出出错: {ex}"); }
        }
    }

    /// <summary>
    /// 把覆盖了 Setting 的插件挂进"系统"菜单
    /// </summary>
    /// 判据与 Windows 版一致: 覆盖了才显示入口
    private void LoadUnifiedSettingMenu(ToolBar toolbar)
    {
        foreach (var host in unifiedHosts)
        {
            var plugin = host.Plugin;
            if (plugin.GetType().GetMethod("Setting")?.DeclaringType == typeof(UnifiedPlugin))
                continue;
            var name = plugin.PluginName;
            toolbar.AddMenuButton(ToolBar.MenuType.Setting,
                LinePutScript.Localization.LocalizeCore.Translate("MOD设置"), name, () =>
                {
                    try { plugin.Setting(); }
                    catch (Exception ex) { Log($"插件 {name} 设置界面出错: {ex}"); }
                });
        }
    }
}
