namespace HKW.HKWUtils.Observable;

/// <summary>
/// 通知属性改变前接口
/// </summary>
public interface INotifyPropertyChangingX<TSender>
{
    /// <summary>
    /// 属性改变前事件
    /// </summary>
    public event PropertyChangingXEventHandler<TSender>? PropertyChangingX;
}
