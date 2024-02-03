namespace HKW.HKWUtils;

/// <summary>
/// 可观察的枚举标签模型
/// </summary>
/// <typeparam name="T">枚举类型</typeparam>
public class ObservableEnumFlags<T> : ObservableClass<ObservableEnumFlags<T>>
    where T : Enum
{
    private T _EnumValue;
    public T EnumValue
    {
        get => _EnumValue;
        set => SetProperty(ref _EnumValue, value);
    }

    /// <summary>
    /// 添加枚举命令
    /// </summary>
    public ObservableCommand<T> AddCommand { get; } = new();

    /// <summary>
    /// 删除枚举命令
    /// </summary>
    public ObservableCommand<T> RemoveCommand { get; } = new();

    /// <summary>
    /// 枚举类型
    /// </summary>
    public Type EnumType = typeof(T);

    /// <summary>
    /// 枚举基类
    /// </summary>
    public Type UnderlyingType { get; } = Enum.GetUnderlyingType(typeof(T));

    public ObservableEnumFlags()
    {
        if (Attribute.IsDefined(EnumType, typeof(FlagsAttribute)) is false)
            throw new Exception($"此枚举类型未使用特性 [{nameof(FlagsAttribute)}]");
        AddCommand.ExecuteCommand += AddCommand_Execute;
        RemoveCommand.ExecuteCommand += RemoveCommand_Execute;
    }

    public ObservableEnumFlags(T value)
        : this()
    {
        EnumValue = value;
    }

    private void AddCommand_Execute(T v)
    {
        if (UnderlyingType == typeof(int))
        {
            EnumValue = (T)
                Enum.Parse(EnumType, (Convert.ToInt32(EnumValue) | Convert.ToInt32(v)).ToString());
        }
        else
            throw new NotImplementedException($"Value type: {UnderlyingType}");
    }

    private void RemoveCommand_Execute(T v)
    {
        if (UnderlyingType == typeof(int))
        {
            EnumValue = (T)
                Enum.Parse(EnumType, (Convert.ToInt32(EnumValue) & ~Convert.ToInt32(v)).ToString());
        }
        else
            throw new NotImplementedException($"Value type: {UnderlyingType}");
    }
}
