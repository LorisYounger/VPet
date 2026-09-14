using CommunityToolkit.Mvvm.ComponentModel;
using HanumanInstitute.MvvmDialogs;
using HKW.MVVM;

namespace VPet.Solution;

public partial class ViewModelBase : ObservableObjectEx
{
    public ViewModelBase(IDialogService dialogService)
    {
        DialogService = dialogService;
    }

    internal IDialogService DialogService { get; }
}

public partial class CloseableViewModel : ViewModelBase, IDisposable, IViewClosed
{
    public CloseableViewModel(IDialogService dialogService)
        : base(dialogService) { }

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
