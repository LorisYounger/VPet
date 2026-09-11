using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace VPet_Simulator.Core.MutiPlatform;

/// <summary>
/// 一个 MOD 的插件加载上下文
/// </summary>
/// 每个 MOD 一个, 这样两个 MOD 各自带了不同版本的同一个库时不会打架.
///
/// 有一条是必须守住的: **契约和后端一律回落到主上下文**. 契约要是在这里被重新
/// 加载一份, `exportedType.BaseType == typeof(UnifiedPlugin)` 就永远为假 —— 表现
/// 是所有插件被静默跳过, 没有任何报错, 极难查.
///
/// 不做可回收(collectible): 插件会挂事件、开计时器、建界面, 卸载时机根本控制不住,
/// 强行可回收只会换来"卸载时随机崩溃". Windows 版也是从头到尾不卸载的.
public sealed class PluginLoadContext : AssemblyLoadContext
{
    /// <summary>
    /// 一律走主上下文的程序集
    /// </summary>
    /// 契约与后端在这里, 宿主与插件必须看到同一份类型.
    private static readonly string[] Shared =
    {
        "VPet-Simulator.Unified.Interface",
        "VPet-Simulator.Unified.Services",
        "VPet-Simulator.Windows.Interface.Base",
        "VPet_Simulator.Core.Base",
        "VPet_Simulator.Core.MutiPlatform",
        "LinePutScript",
        "LinePutScript.Localization",
    };

    private readonly AssemblyDependencyResolver resolver;

    /// <summary>
    /// MOD 名, 用来给上下文起名方便排错
    /// </summary>
    public string ModName { get; }

    public PluginLoadContext(string modName, string mainAssemblyPath)
        : base(name: "VPetMod:" + modName, isCollectible: false)
    {
        ModName = modName;
        resolver = new AssemblyDependencyResolver(mainAssemblyPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var name = assemblyName.Name;
        if (name == null)
            return null;

        // 共享的一律交给主上下文
        foreach (var shared in Shared)
        {
            if (string.Equals(name, shared, StringComparison.OrdinalIgnoreCase))
                return null;
        }
        // 主上下文里已经有的同名程序集也交回去: 两份 Avalonia 会当场炸
        foreach (var loaded in Default.Assemblies)
        {
            if (string.Equals(loaded.GetName().Name, name, StringComparison.OrdinalIgnoreCase))
                return null;
        }

        var path = resolver.ResolveAssemblyToPath(assemblyName);
        return path == null ? null : LoadFromAssemblyPath(path);
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var path = resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return path == null ? IntPtr.Zero : LoadUnmanagedDllFromPath(path);
    }
}
