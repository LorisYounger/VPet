namespace HKW.HKWUtils;

public class ObservableRect<T> : ObservableClass<ObservableRect<T>>, IEquatable<ObservableRect<T>>
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

    private T _width;
    public T Width
    {
        get => _width;
        set => SetProperty(ref _width, value);
    }

    private T _heigth;
    public T Height
    {
        get => _heigth;
        set => SetProperty(ref _heigth, value);
    }

    public ObservableRect() { }

    public ObservableRect(T x, T y, T width, T hetght)
    {
        X = x;
        Y = y;
        Width = width;
        Height = hetght;
    }

    /// <summary>
    /// 复制一个新的对象
    /// </summary>
    /// <returns>新对象</returns>
    public ObservableRect<T> Copy()
    {
        return new(X, Y, Width, Height);
    }

    #region Other

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y, Width, Height);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is ObservableRect<T> temp
            && EqualityComparer<T>.Default.Equals(X, temp.X)
            && EqualityComparer<T>.Default.Equals(Y, temp.Y)
            && EqualityComparer<T>.Default.Equals(Width, temp.Width)
            && EqualityComparer<T>.Default.Equals(Height, temp.Height);
    }

    /// <inheritdoc/>
    public bool Equals(ObservableRect<T>? other)
    {
        return Equals(obj: other);
    }

    /// <inheritdoc/>
    public static bool operator ==(ObservableRect<T> a, ObservableRect<T> b)
    {
        return Equals(a, b);
    }

    /// <inheritdoc/>
    public static bool operator !=(ObservableRect<T> a, ObservableRect<T> b)
    {
        return Equals(a, b) is not true;
    }

    #endregion
}
