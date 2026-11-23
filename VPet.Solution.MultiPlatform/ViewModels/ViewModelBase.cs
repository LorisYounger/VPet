using System.ComponentModel;
using HanumanInstitute.MvvmDialogs;
using HKW.MVVM;
using HKW.MVVM.SourceGenerator;

namespace VPet.Solution.MultiPlatform;

public partial class ViewModelBase : ObservableObjectEx
{
    [DIProperty]
    public required IDialogService DialogService { get; init; }
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
