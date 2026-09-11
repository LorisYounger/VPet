namespace VPet_Simulator.Windows.Interface;

/// <summary>
/// 翻译转发 (跨平台版)
/// </summary>
/// 共享源码不直接引用 LinePutScript.Localization.WPF 或 LinePutScript.Localization
/// 中的任何一个, 而是调这个垫片, 两个平台各放一份指向自己那个包.
///
/// 不能让 Interface 同时引用两个 Localization 包: Windows 的部署目录会多出一个 dll
/// (CoreMOD 有写死的白名单), 而且同一个进程里会出现两份互不相干的翻译表.
///
/// internal 是有意的 —— 它不进公开 API 表面, 对已编译的 MOD 完全不可见.
internal static class Localize
{
    public static string Translate(this string text)
        => LinePutScript.Localization.LocalizeCore.Translate(text);

    public static string Translate(this string text, params object[] args)
        => LinePutScript.Localization.LocalizeCore.Translate(text, args);
}
