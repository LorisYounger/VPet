using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HanumanInstitute.MvvmDialogs;
using HanumanInstitute.MvvmDialogs.FrameworkDialogs;
using HKW.HKWUtils.Collections;
using HKW.HKWUtils.Observable;
using HKW.MVVM;
using HKW.MVVM.SourceGenerator;
using VPet.Solution.MultiPlatform.Models;

namespace VPet.Solution.MultiPlatform.ViewModels;

public partial class CustomizedSettingViewModel : DisposableViewModel, ISubSettingViewModel
{
    public CustomizedSettingViewModel()
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

    partial void OnSettingChanged(SettingModel oldValue, SettingModel newValue)
    {
        if (newValue is not null)
        {
            Links = new(newValue.CustomizedSetting.Links, [], x =>
                x.Name.Contains(SearchLink, StringComparison.CurrentCultureIgnoreCase)
            );
        }
        SearchLink = string.Empty;
    }

    [NotifyPropertyChangeFrom(nameof(Setting))]
    public CustomizedSettingModel CustomizedSetting => Setting?.CustomizedSetting!;

    [ObservableProperty]
    public string SearchLink { get; set; } = string.Empty;

    [ObservableProperty]
    public FilteredListWrapper<
        LinkModel,
        List<LinkModel>,
        ObservableList<LinkModel>
    > Links { get; set; } = null!;

    [RelayCommand]
    private async Task ClearLinks()
    {
        var result = await DialogService.ShowMessageBoxAsync(
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
    private void RemoveLink(LinkModel? parameter)
    {
        if (parameter is null)
            return;
        Links.Remove(parameter);
    }

    [RelayCommand]
    private void SendKey()
    {
        if (Setting.GraphicsSetting.Language.StartsWith("zh"))
        {
            NativeUtils.Launch("https://www.exlb.net/SendKeys");
        }
        else if (Setting.GraphicsSetting.Language == "null")
        {
            NativeUtils.Launch(
                "https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.sendkeys?view=windowsdesktop-7.0#remarks"
            );
        }
        else
        {
            NativeUtils.Launch(
                $"https://learn.microsoft.com/{Setting.GraphicsSetting.Language}/dotnet/api/system.windows.forms.sendkeys?view=windowsdesktop-7.0#remarks"
            );
        }
    }
}
