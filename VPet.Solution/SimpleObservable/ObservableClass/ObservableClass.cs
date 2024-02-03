using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HKW.HKWUtils.Observable;

/// <summary>
/// 可观察对象
/// <para>示例:<code><![CDATA[
/// public class ObservableClassExample : ObservableClass<ObservableClassExample>
/// {
///     int _value = 0;
///     public int Value
///     {
///         get => _value;
///         set => SetProperty(ref _value, value);
///     }
/// }]]></code></para>
/// </summary>
public abstract class ObservableClass<TObject>
    : INotifyPropertyChanging,
        INotifyPropertyChanged,
        INotifyPropertyChangingX<TObject>,
        INotifyPropertyChangedX<TObject>
    where TObject : ObservableClass<TObject>
{
    public ObservableClass()
    {
        if (GetType() != typeof(TObject))
            throw new InvalidCastException(
                $"Inconsistency between target type [{GetType().FullName}] and generic type [{typeof(TObject).FullName}]"
            );
    }

    #region OnPropertyChange
    /// <summary>
    /// 设置属性值
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="newValue">新值</param>
    /// <param name="propertyName">属性名称</param>
    /// <returns>成功为 <see langword="true"/> 失败为 <see langword="false"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual bool SetProperty<TValue>(
        ref TValue value,
        TValue newValue,
        [CallerMemberName] string propertyName = null!
    )
    {
        if (EqualityComparer<TValue>.Default.Equals(value, newValue) is true)
            return false;
        var oldValue = value;
        if (OnPropertyChanging(oldValue, newValue, propertyName))
            return false;
        value = newValue;
        OnPropertyChanged(oldValue, newValue, propertyName);
        return true;
    }

    /// <summary>
    /// 属性改变前
    /// </summary>
    /// <param name="oldValue">旧值</param>
    /// <param name="newValue">新值</param>
    /// <param name="propertyName">属性名称</param>
    /// <returns>取消为 <see langword="true"/> 否则为 <see langword="false"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual bool OnPropertyChanging(
        object? oldValue,
        object? newValue,
        [CallerMemberName] string propertyName = null!
    )
    {
        PropertyChanging?.Invoke(this, new(propertyName));
        if (PropertyChangingX is null)
            return false;
        var e = new PropertyChangingXEventArgs(propertyName, oldValue, newValue);
        PropertyChangingX?.Invoke((TObject)this, e);
        if (e.Cancel)
            PropertyChanged?.Invoke(this, new(propertyName));
        return e.Cancel;
    }

    /// <summary>
    /// 属性改变后
    /// </summary>
    /// <param name="oldValue">旧值</param>
    /// <param name="newValue">新值</param>
    /// <param name="propertyName">属性名称</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual void OnPropertyChanged(
        object? oldValue,
        object? newValue,
        [CallerMemberName] string propertyName = null!
    )
    {
        PropertyChanged?.Invoke(this, new(propertyName));
        PropertyChangedX?.Invoke((TObject)this, new(propertyName, oldValue, newValue));
    }
    #endregion

    #region Event
    /// <inheritdoc/>
    public event PropertyChangingEventHandler? PropertyChanging;

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc/>
    public event PropertyChangingXEventHandler<TObject>? PropertyChangingX;

    /// <inheritdoc/>
    public event PropertyChangedXEventHandler<TObject>? PropertyChangedX;
    #endregion
}
