namespace HKW.HKWUtils;

public class ObservableRect : ObservableClass<ObservableRect>, IEquatable<ObservableRect>
{
    private double _x;
    public double X
    {
        get => _x;
        set => SetProperty(ref _x, value);
    }

    private double _y;
    public double Y
    {
        get => _y;
        set => SetProperty(ref _y, value);
    }

    private double _width;
    public double Width
    {
        get => _width;
        set => SetProperty(ref _width, value);
    }

    private double _heigth;
    public double Height
    {
        get => _heigth;
        set => SetProperty(ref _heigth, value);
    }

    public ObservableRect() { }

    public ObservableRect(double x, double y, double width, double hetght)
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
    public ObservableRect Copy()
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
        return obj is ObservableRect temp
            && EqualityComparer<double>.Default.Equals(X, temp.X)
            && EqualityComparer<double>.Default.Equals(Y, temp.Y)
            && EqualityComparer<double>.Default.Equals(Width, temp.Width)
            && EqualityComparer<double>.Default.Equals(Height, temp.Height);
    }

    /// <inheritdoc/>
    public bool Equals(ObservableRect? other)
    {
        return Equals(obj: other);
    }

    /// <inheritdoc/>
    public static bool operator ==(ObservableRect a, ObservableRect b)
    {
        return Equals(a, b);
    }

    /// <inheritdoc/>
    public static bool operator !=(ObservableRect a, ObservableRect b)
    {
        return Equals(a, b) is not true;
    }

    #endregion
}
