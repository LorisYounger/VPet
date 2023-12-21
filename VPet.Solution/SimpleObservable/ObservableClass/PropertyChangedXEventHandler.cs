namespace HKW.HKWUtils.Observable;

/// <summary>
/// 属性改变后事件
/// </summary>
/// <param name="sender">发送者</param>
/// <param name="e">参数</param>
public delegate void PropertyChangedXEventHandler<TSender>(
    TSender sender,
    PropertyChangedXEventArgs e
);
