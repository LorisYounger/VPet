using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HanumanInstitute.MvvmDialogs;
using HanumanInstitute.MvvmDialogs.FrameworkDialogs;
using HKW.HKWUtils.Collections;
using HKW.HKWUtils.Observable;
using HKW.MVVM;
using LinePutScript.Localization.WPF;
using VPet.Solution.Models.SettingEditor;

namespace VPet.Solution.ViewModels.SettingEditor;

public partial class CustomizedSettingViewModel : CloseableViewModel, ISubSettingViewModel
{
    public CustomizedSettingViewModel(IDialogService dialogService)
        : base(dialogService)
    {
        this.WhenAnyValue(x => x.SearchLink)
            .Subscribe(_ => Links?.Refresh())
            .DisposeWith(Disposables);
    }

    [ObservableProperty]
    public SettingModel Setting { get; set; } = null!;

    public CustomizedSettingModel CustomizedSetting => Setting.CustomizedSetting;

    [ObservableProperty]
    public string SearchLink { get; set; } = string.Empty;

    [ObservableProperty]
    public FilteredListWrapper<
        LinkModel,
        List<LinkModel>,
        ObservableList<LinkModel>
    > Links { get; set; } = null!;

    [RelayCommand]
    private void ClearLinks()
    {
        var result = DialogService.ShowMessageBox(
            this,
            "确定清空吗".Translate(),
            "",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning
        );
        if (result is not true)
            return;

        SearchLink = string.Empty;
        Links.Clear();
        Setting.IsChanged = true;
    }

    [RelayCommand]
    private void AddLink()
    {
        SearchLink = string.Empty;
        Links.Add(new());
        Setting.IsChanged = true;
    }

    [RelayCommand]
    private void RemoveLink(LinkModel parameter)
    {
        Links.Remove(parameter);
    }

    partial class CustomizedSettingViewModelObservableObjectHelper
    {
        partial void OnSettingChanged(SettingModel oldValue, SettingModel newValue)
        {
            if (newValue is not null)
            {
                _source.Links = new(
                    newValue.CustomizedSetting.Links,
                    [],
                    x =>
                        x.Name.Contains(
                            _source.SearchLink,
                            StringComparison.CurrentCultureIgnoreCase
                        )
                );
            }
            _source.SearchLink = string.Empty;
        }
    }
}
