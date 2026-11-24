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

public partial class ModSettingViewModel : DisposableViewModel, ISubSettingViewModel
{
    public ModSettingViewModel()
    {
        this.WhenAnyValue(x => x.SearchMod)
            .Throttle(TimeSpan.FromSeconds(0.5), ObservableSchedulers.ThreadPool)
            .DistinctUntilChanged()
            .ObserveOn(ObservableSchedulers.Current)
            .Subscribe(_ => Mods?.Refresh())
            .DisposeWith(Disposables);
    }

    [ObservableProperty]
    public SettingModel Setting { get; set; } = null!;

    partial void OnSettingChanged(SettingModel oldValue, SettingModel newValue)
    {
        if (newValue is not null)
        {
            Mods = new(newValue.ModSetting.Mods, [], x =>
                x.Name.Contains(SearchMod, StringComparison.CurrentCultureIgnoreCase)
            );
        }
        SearchMod = string.Empty;
    }

    [NotifyPropertyChangeFrom(nameof(Setting))]
    public ModSettingModel ModSetting => Setting?.ModSetting!;

    [ObservableProperty]
    public string SearchMod { get; set; } = string.Empty;

    [ObservableProperty]
    public FilteredListWrapper<
        ModModel,
        List<ModModel>,
        ObservableList<ModModel>
    > Mods { get; set; } = null!;

    [ObservableProperty]
    public ModModel? CurrentMod { get; set; }

    [RelayCommand]
    private async Task ClearMods()
    {
        var result = await DialogService.ShowMessageBoxAsync(
            this,
            "确定清除全部模组吗".Translate(),
            "",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning
        );
        if (result is not true)
            return;
        Mods.BatchUpdate(l =>
        {
            l.Clear();
            SearchMod = string.Empty;
        });
        Setting.IsChanged = true;
    }

    [RelayCommand]
    private async Task ClearFailMods()
    {
        var result = await DialogService.ShowMessageBoxAsync(
            this,
            "确定清除全部失效模组吗".Translate(),
            "",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning
        );
        if (result is not true)
            return;
        Mods.BatchUpdate(l =>
        {
            for (var i = 0; i < l.Count; i++)
            {
                if (l[i].IsEnabled is null)
                    l.RemoveAt(i--);
            }
            SearchMod = string.Empty;
        });
        Setting.IsChanged = true;
    }

    [RelayCommand]
    private static void OpenSteamCommunity(ModModel? parameter)
    {
        if (parameter is null)
            return;
        NativeUtils.Launch(
            "https://steamcommunity.com/sharedfiles/filedetails/?id=" + parameter.ItemID
        );
    }

    [RelayCommand]
    private async Task OpenModPath(ModModel? parameter)
    {
        if (parameter is null)
            return;
        if (await NativeUtils.LaunchAsync(parameter.ModPath) is false)
        {
            await DialogService.ShowMessageBoxAsync(
                this,
                "未找到模组\n路径: {0}".Translate(parameter.ModPath)
            );
        }
    }
}
