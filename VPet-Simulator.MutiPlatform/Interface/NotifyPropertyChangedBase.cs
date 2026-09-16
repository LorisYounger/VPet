using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VPet_Simulator.Windows.Interface;

/// <summary>
/// 属性变更通知基类
/// </summary>
/// 对应 Panuon.WPF 的 NotifyPropertyChangedBase (Item 的基类). Windows 版 Item 的基类子句写在
/// Windows 半里, 这边同样写在跨平台半里, 共享的那半不知道基类是谁.
public abstract class NotifyPropertyChangedBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 通知某个属性变了 (Panuon 里就叫这个名字, Windows 版代码直接调它)
    /// </summary>
    public void NotifyOfPropertyChange([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    protected void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
        => NotifyOfPropertyChange(propertyName);

    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        NotifyPropertyChanged(propertyName);
        return true;
    }
}
