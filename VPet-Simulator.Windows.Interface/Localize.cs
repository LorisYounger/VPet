namespace VPet_Simulator.Windows.Interface;

/// <summary>
/// 翻译转发 (Windows 版)
/// </summary>
/// 共享源码 (Base/) 里写 "文本".Translate(), 由这里转发到本平台用的那个本地化包;
/// 跨平台宿主那边整个工程用的就是 LinePutScript.Localization, 在 csproj 里全局 using 即可, 没有垫片.
///
/// Interface 自己的文件仍然可以保留原来的 using LinePutScript.Localization.WPF,
/// 不会有歧义: 同命名空间里的扩展方法比编译单元级的 using 优先.
///
/// internal 是有意的 —— 它不进公开 API 表面, 对已编译的 MOD 完全不可见.
internal static class Localize
{
    public static string Translate(this string text)
        => LinePutScript.Localization.WPF.LocalizeCore.Translate(text);

    public static string Translate(this string text, params object[] args)
        => LinePutScript.Localization.WPF.LocalizeCore.Translate(text, args);
}
