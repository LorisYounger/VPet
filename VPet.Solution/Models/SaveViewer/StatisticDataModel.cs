namespace VPet.Solution.Models.SaveViewer;

/// <summary>
/// 统计数据模型
/// </summary>
public class StatisticDataModel : ObservableClass<StatisticDataModel>
{
    #region Id
    private string _id;

    /// <summary>
    /// ID
    /// </summary>
    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }
    #endregion

    #region Name
    private string _name;

    /// <summary>
    /// 名称
    /// </summary>
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }
    #endregion

    #region Value
    private object _value;

    /// <summary>
    /// 值
    /// </summary>
    public object Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }
    #endregion
}
