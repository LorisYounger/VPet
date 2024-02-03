using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;

namespace HKW.HKWUtils.Observable;

/// <summary>
/// 具有参数的可观察命令
/// </summary>
[DebuggerDisplay("\\{ObservableCommand, CanExecute = {IsCanExecute.Value}\\}")]
public class ObservableCommand<T> : ObservableClass<ObservableCommand<T>>, ICommand
{
    bool _isCanExecute = true;

    /// <summary>
    /// 能执行的属性
    /// </summary>
    public bool IsCanExecute
    {
        get => _isCanExecute;
        set => SetProperty(ref _isCanExecute, value);
    }

    bool _currentCanExecute = true;

    /// <summary>
    /// 当前可执行状态
    /// <para>
    /// 在执行异步事件时会强制为 <see langword="false"/>, 但异步结束后会恢复为 <see cref="IsCanExecute"/> 的值
    /// </para>
    /// </summary>
    public bool CurrentCanExecute
    {
        get => _currentCanExecute;
        private set => SetProperty(ref _currentCanExecute, value);
    }

    /// <inheritdoc/>
    public ObservableCommand()
    {
        PropertyChanged += OnCanExecuteChanged;
    }

    private void OnCanExecuteChanged(object? sender, PropertyChangedEventArgs e)
    {
        CanExecuteChanged?.Invoke(this, new());
    }

    #region ICommand
    /// <summary>
    /// 能否被执行
    /// </summary>
    /// <param name="parameter">参数</param>
    /// <returns>能被执行为 <see langword="true"/> 否则为 <see langword="false"/></returns>
    public bool CanExecute(object? parameter)
    {
        return CurrentCanExecute && IsCanExecute;
    }

    /// <summary>
    /// 执行方法
    /// </summary>
    /// <param name="parameter">参数</param>
    public async void Execute(object? parameter)
    {
        if (IsCanExecute is not true)
            return;
        ExecuteCommand?.Invoke((T)parameter!);
        await ExecuteAsync((T)parameter!);
    }

    /// <summary>
    /// 执行异步方法, 会在等待中修改 <see cref="CurrentCanExecute"/>, 完成后恢复
    /// <para>
    /// 若要在执行此方法时触发 <see cref="ExecuteCommand"/> 事件, 请将 <paramref name="runAlone"/> 设置为 <see langword="true"/>
    /// </para>
    /// </summary>
    /// <param name="parameter">参数</param>
    /// <param name="runAlone">设置为 <see langword="true"/> 时触发 <see cref="ExecuteCommand"/> 事件</param>
    /// <returns>任务</returns>
    public async Task ExecuteAsync(T parameter, bool runAlone = false)
    {
        if (IsCanExecute is not true)
            return;
        if (runAlone)
            ExecuteCommand?.Invoke(parameter);
        if (ExecuteAsyncCommand is null)
            return;
        CurrentCanExecute = false;
        foreach (
            var asyncEvent in ExecuteAsyncCommand
                .GetInvocationList()
                .Cast<ExecuteAsyncEventHandler<T>>()
        )
            await asyncEvent.Invoke(parameter);
        CurrentCanExecute = true;
    }
    #endregion

    #region Event
    /// <summary>
    /// 能否执行属性改变后事件
    /// </summary>
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// 执行事件
    /// </summary>
    public event ExecuteEventHandler<T>? ExecuteCommand;

    /// <summary>
    /// 异步执行事件
    /// </summary>
    public event ExecuteAsyncEventHandler<T>? ExecuteAsyncCommand;
    #endregion
}
