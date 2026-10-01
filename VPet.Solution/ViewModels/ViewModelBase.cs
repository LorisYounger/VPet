using CommunityToolkit.Mvvm.ComponentModel;
using HanumanInstitute.MvvmDialogs;
using HKW.MVVM;
using HKW.MVVM.SourceGenerator;

namespace VPet.Solution;

public partial class ViewModelBase : ObservableObjectEx
{
    [DIProperty]
    public required IDialogService DialogService { get; init; }
}

public partial class CloseableViewModel : ViewModelBase, IDisposable, IViewClosed
{
    /// <summary>
    /// 一次性关闭事件
    /// </summary>
    public event EventHandler? Closed;

    public virtual void OnClosed()
    {
        Closed?.Invoke(this, EventArgs.Empty);
        Closed = null;
    }

    internal MultipleDisposable Disposables { get; } = [];

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
