using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HKW.HKWUtils.Collections;
using HKW.HKWUtils.Observable;
using HKW.MVVM;
using VPet.Solution.MultiPlatform.Models.SettingEditor;

namespace VPet.Solution.MultiPlatform.ViewModels.SettingEditor;

public partial class ModSettingViewModel : CloseableViewModel, ISubSettingViewModel
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

    public ModSettingModel ModSetting => Setting.ModSetting;

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
    private void ClearMods()
    {
        // TODO: DialogService 确认清除全部模组
        // var result = DialogService.ShowMessageBox(
        //     this,
        //     "确定清除全部模组吗".Translate(),
        //     "",
        //     MessageBoxButton.YesNo,
        //     MessageBoxImage.Warning
        // );
        // if (result is not true)
        //     return;
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
        // TODO: DialogService 确认清除全部失效模组
        // var result = DialogService.ShowMessageBox(
        //     this,
        //     "确定清除全部失效模组吗".Translate(),
        //     "",
        //     MessageBoxButton.YesNo,
        //     MessageBoxImage.Warning
        // );
        // if (result is not true)
        //     return;
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
        // TODO: OpenLink 打开 Steam 创意工坊页面
        // NativeUtils.OpenLink(
        //     "https://steamcommunity.com/sharedfiles/filedetails/?id=" + parameter.ItemID
        // );
    }

    [RelayCommand]
    private void OpenModPath(ModModel? parameter)
    {
        if (parameter is null)
            return;
        // TODO: OpenLink 打开模组所在文件夹, 失败时通过 DialogService 提示
        // try
        // {
        //     NativeUtils.OpenLink(parameter.ModPath);
        // }
        // catch
        // {
        //     DialogService.ShowMessageBox(
        //         this,
        //         "未找到模组\n路径: {0}".Translate(parameter.ModPath)
        //     );
        // }
    }
}
