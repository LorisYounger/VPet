namespace HKW.HKWUtils;

/// <summary>
/// 可观察地点
/// </summary>
/// <typeparam name="T">类型</typeparam>
public class ObservablePoint<T>
    : ObservableClass<ObservablePoint<T>>,
        IEquatable<ObservablePoint<T>>
{
    private T _x;
    public T X
    {
        get => _x;
        set => SetProperty(ref _x, value);
    }

    private T _y;
    public T Y
    {
        get => _y;
        set => SetProperty(ref _y, value);
    }

    public ObservablePoint() { }

    public ObservablePoint(T x, T y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// 复制一个新的对象
    /// </summary>
    /// <returns>新对象</returns>
    public ObservablePoint<T> Copy()
    {
        return new(X, Y);
    }

    #region Other

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is ObservablePoint<T> temp
            && EqualityComparer<T>.Default.Equals(X, temp.X)
            && EqualityComparer<T>.Default.Equals(Y, temp.Y);
    }

    /// <inheritdoc/>
    public bool Equals(ObservablePoint<T>? other)
    {
        return Equals(obj: other);
    }

    /// <inheritdoc/>
    public static bool operator ==(ObservablePoint<T> a, ObservablePoint<T> b)
    {
        return Equals(a, b);
    }

    /// <inheritdoc/>
    public static bool operator !=(ObservablePoint<T> a, ObservablePoint<T> b)
    {
        return Equals(a, b) is not true;
    }

    #endregion
}
