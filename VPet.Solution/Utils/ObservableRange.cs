namespace HKW.HKWUtils;

/// <summary>
/// 可观察的范围
/// </summary>
/// <typeparam name="T">类型</typeparam>
public class ObservableRange<T>
    : ObservableClass<ObservableRange<T>>,
        IEquatable<ObservableRange<T>>
{
    private T _min;
    public T Min
    {
        get => _min;
        set => SetProperty(ref _min, value);
    }

    private T _max;
    public T Max
    {
        get => _max;
        set => SetProperty(ref _max, value);
    }

    public ObservableRange() { }

    public ObservableRange(T min, T max)
    {
        _min = min;
        _max = max;
    }

    /// <summary>
    /// 复制一个新的对象
    /// </summary>
    /// <returns>新对象</returns>
    public ObservableRange<T> Copy()
    {
        return new(Min, Max);
    }

    #region Other

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(Min, Max);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is ObservableRange<T> temp
            && EqualityComparer<T>.Default.Equals(Min, temp.Min)
            && EqualityComparer<T>.Default.Equals(Max, temp.Max);
    }

    /// <inheritdoc/>
    public bool Equals(ObservableRange<T>? other)
    {
        return Equals(obj: other);
    }

    /// <inheritdoc/>
    public static bool operator ==(ObservableRange<T> a, ObservableRange<T> b)
    {
        return Equals(a, b);
    }

    /// <inheritdoc/>
    public static bool operator !=(ObservableRange<T> a, ObservableRange<T> b)
    {
        return Equals(a, b) is not true;
    }

    #endregion
}
