namespace HKW.HKWUtils.Observable;

/// <summary>
/// 执行事件
/// </summary>
public delegate void ExecuteEventHandler();

/// <summary>
/// 执行事件
/// </summary>
/// <param name="parameter">参数</param>
public delegate void ExecuteEventHandler<T>(T parameter);
