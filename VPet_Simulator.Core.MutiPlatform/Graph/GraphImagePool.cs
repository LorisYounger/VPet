using Avalonia.Controls;
using Avalonia.Threading;

namespace VPet_Simulator.Core.MutiPlatform.Graph;

/// <summary>
/// 动画共用的 Image 控件池
/// </summary>
/// 桌宠用两层 Decorator 做双缓冲(见 MainDisplay.Display), 切换动画时新动画要先
/// 在隐藏的那一层上渲染出首帧, 旧动画才停. 也就是说同一时刻可能有两层各自需要一个
/// Image 控件, 只准备一个是不够的 —— 而且 Avalonia 里一个控件只能有一个父级,
/// 把已挂在 PetGrid 上的 Image 再设给 PetGrid2 会直接抛异常.
///
/// Windows 版为此给每种动画准备了三个共用 Image (Image1/2/3.XXX), 这里保持一致.
/// 复用控件而不是每次新建, 是为了避免频繁创建控件造成的闪烁和 GC 压力.
internal static class GraphImagePool
{
    /// <summary>
    /// 从共用池里挑一个可用的 Image 挂到 parent 上
    /// </summary>
    /// <param name="parent">目标层</param>
    /// <param name="core">图形核心</param>
    /// <param name="kind">动画种类, 例如 PNGAnimation</param>
    /// <returns>已经挂在 parent 上的 Image</returns>
    /// 必须在 UI 线程调用: Avalonia 的控件有线程亲和性, 在后台线程创建的控件
    /// 挂到 UI 线程持有的 Decorator 上会抛"调用线程无法访问此对象".
    /// 动画的加载是在后台线程做的, 所以控件只能等到这里(已经回到 UI 线程)才创建.
    public static Image Attach(Decorator parent, GraphCore core, string kind)
    {
        Dispatcher.UIThread.VerifyAccess();

        var first = Obtain(core, $"Image1.{kind}");
        var second = Obtain(core, $"Image2.{kind}");
        var third = Obtain(core, $"Image3.{kind}");

        // parent 上已经挂着池里的控件时直接复用, 避免不必要的重新挂载
        if (ReferenceEquals(parent.Child, first))
            return first;
        if (ReferenceEquals(parent.Child, third))
            return third;
        if (ReferenceEquals(parent.Child, second))
            return second;

        // 优先用 2 号; 它要是正被另一层占着, 就退而用 1 号
        var target = second.Parent == null ? second : first;
        if (target.Parent is Decorator old && !ReferenceEquals(old, parent))
            old.Child = null;
        parent.Child = target;
        return target;
    }

    private static Image Obtain(GraphCore core, string key)
    {
        if (core.CommUIElements.TryGetValue(key, out var existing) && existing is Image image)
            return image;
        var created = new Image { Width = 500 };
        core.CommUIElements[key] = created;
        return created;
    }
}
