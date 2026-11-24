using System.ComponentModel;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Input;
using HanumanInstitute.MvvmDialogs;
using HKW.MVVM;
using HKW.MVVM.SourceGenerator;

namespace VPet.Solution.MultiPlatform;

public partial class ViewModelBase : ObservableObjectEx
{
    [DIProperty]
    public required IDialogService DialogService { get; init; }

    /// <summary>
    /// 是否为单视图生命周期 (Web / Android)
    /// </summary>
    public bool IsSingleView =>
        Application.Current?.ApplicationLifetime is ISingleViewApplicationLifetime;
}

public partial class DisposableViewModel : ViewModelBase, IDisposable
{
    protected MultipleDisposable Disposables { get; } = [];

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (Disposables.IsDisposed)
            return;
        if (disposing)
        {
            Disposables.Dispose();
        }
    }
}

public partial class CloseableViewModel : DisposableViewModel, IViewClosing, ICloseable
{
    /// <summary>
    /// 执行关闭事件
    /// </summary>
    public event EventHandler? RequestClose;

    /// <summary>
    /// 关闭
    /// </summary>
    public virtual void Close()
    {
        RequestClose?.Invoke(this, EventArgs.Empty);
        Dispose();
    }

    /// <summary>
    /// 返回上一页 (关闭前触发未保存校验)
    /// </summary>
    [RelayCommand]
    public async Task NavigateBack()
    {
        var args = new CancelEventArgs(false);
        await OnClosingAsync(args);
        if (args.Cancel)
            return;
        Close();
    }

    public void OnClosing(CancelEventArgs e)
    {
        // Avalonia 框架为异步, 同步的 OnClosing 方法无法停止 UI 响应
        // 先将 Cancel 设为 true 拦截退出, 框架随后会自动调用 OnClosingAsync
        e.Cancel = true;
    }

    public virtual async Task OnClosingAsync(CancelEventArgs e)
    {
        e.Cancel = false;
    }
}
