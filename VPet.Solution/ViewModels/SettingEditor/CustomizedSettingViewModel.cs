using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HanumanInstitute.MvvmDialogs;
using HanumanInstitute.MvvmDialogs.FrameworkDialogs;
using HKW.HKWUtils.Collections;
using HKW.HKWUtils.Observable;
using HKW.MVVM;
using LinePutScript.Localization.WPF;
using VPet_Simulator.Windows.Interface;
using VPet.Solution.Models.SettingEditor;

namespace VPet.Solution.ViewModels.SettingEditor;

public partial class CustomizedSettingViewModel : CloseableViewModel, ISubSettingViewModel
{
    public CustomizedSettingViewModel(IDialogService dialogService)
        : base(dialogService)
    {
        this.WhenAnyValue(x => x.SearchLink)
            .Throttle(TimeSpan.FromSeconds(0.5), ObservableSchedulers.ThreadPool)
            .DistinctUntilChanged()
            .ObserveOn(ObservableSchedulers.Current)
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

    [RelayCommand]
    private void SendKey()
    {
        if (Setting.GraphicsSetting.Language.StartsWith("zh"))
            ExtensionFunction.StartURL("https://www.exlb.net/SendKeys");
        else if (Setting.GraphicsSetting.Language == "null")
            ExtensionFunction.StartURL(
                "https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.sendkeys?view=windowsdesktop-7.0#remarks"
            );
        else
            ExtensionFunction.StartURL(
                $"https://learn.microsoft.com/{Setting.GraphicsSetting.Language}/dotnet/api/system.windows.forms.sendkeys?view=windowsdesktop-7.0#remarks"
            );
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
