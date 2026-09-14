using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HanumanInstitute.MvvmDialogs;
using HanumanInstitute.MvvmDialogs.FrameworkDialogs;
using HKW.HKWUtils;
using HKW.HKWUtils.Collections;
using HKW.HKWUtils.Extensions;
using HKW.HKWUtils.Observable;
using HKW.MVVM;
using LinePutScript.Localization.WPF;
using VPet.Solution.Models.SettingEditor;

namespace VPet.Solution.ViewModels.SettingEditor;

public partial class ModSettingViewModel : CloseableViewModel, ISubSettingViewModel
{
    public ModSettingViewModel(IDialogService dialogService)
        : base(dialogService)
    {
        this.WhenAnyValue(x => x.SearchMod)
            .Subscribe(_ => Mods?.Refresh())
            .DisposeWith(Disposables);
    }

    [ObservableProperty]
    public SettingModel Setting { get; set; }

    public ModSettingModel ModSetting => Setting.ModSetting;

    [ObservableProperty]
    public string SearchMod { get; set; } = string.Empty;

    [ObservableProperty]
    public FilteredListWrapper<
        ModModel,
        List<ModModel>,
        ObservableList<ModModel>
    > Mods { get; set; }

    [ObservableProperty]
    public ModModel CurrentMod { get; set; } = null!;

    [RelayCommand]
    private void ClearMods()
    {
        var result = DialogService.ShowMessageBox(
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
    private void ClearFailMods()
    {
        var result = DialogService.ShowMessageBox(
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
    private static void OpenSteamCommunity(ModModel parameter)
    {
        NativeUtils.OpenLink(
            "https://steamcommunity.com/sharedfiles/filedetails/?id=" + parameter.ItemID
        );
    }

    [RelayCommand]
    private void OpenModPath(ModModel parameter)
    {
        try
        {
            NativeUtils.OpenLink(parameter.ModPath);
        }
        catch
        {
            DialogService.ShowMessageBox(
                this,
                "未找到模组\n路径: {0}".Translate(parameter.ModPath)
            );
        }
    }

    partial class ModSettingViewModelObservableObjectHelper
    {
        partial void OnSettingChanged(SettingModel oldValue, SettingModel newValue)
        {
            if (newValue is not null)
            {
                _source.Mods = new(
                    newValue.ModSetting.Mods,
                    [],
                    x =>
                        x.Name.Contains(
                            _source.SearchMod,
                            StringComparison.CurrentCultureIgnoreCase
                        )
                );
            }
            _source.SearchMod = string.Empty;
        }
    }
}
