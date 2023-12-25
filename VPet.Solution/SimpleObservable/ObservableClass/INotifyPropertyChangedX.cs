namespace HKW.HKWUtils.Observable;

/// <summary>
/// 通知属性改变后接口
/// </summary>
public interface INotifyPropertyChangedX<TSender>
{
    /// <summary>
    /// 通知属性改变后事件
    /// </summary>
    public event PropertyChangedXEventHandler<TSender>? PropertyChangedX;
}
