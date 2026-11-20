using System.Collections.Immutable;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HKW.HKWUtils;
using HKW.HKWUtils.Collections;
using HKW.HKWUtils.Extensions;
using HKW.HKWUtils.Observable;
using HKW.MVVM;
using HKW.MVVM.SourceGenerator;
using LinePutScript.Localization;
using VPet.Solution.MultiPlatform.Models.SettingEditor;

namespace VPet.Solution.MultiPlatform.ViewModels.SettingEditor;

public partial class SettingViewModel : CloseableViewModel
{
    public SettingViewModel()
    {
        Settings = new([], [], x =>
            x.Name.Contains(SearchSetting, StringComparison.CurrentCultureIgnoreCase)
        );
        Settings.BatchUpdate(l =>
        {
            LoadSettings(l);
        });

        this.WhenAnyValue(x => x.SearchSetting)
            .Subscribe(_ => Settings.Refresh())
            .DisposeWith(Disposables);

        CurrentSetting = Settings.FirstOrDefault();
    }

    public override void OnClosed()
    {
        base.OnClosed();
        Disposables.Dispose();
        foreach (var setting in Settings)
            setting.Disposables.Dispose();
    }

    #region Property
    [ObservableProperty]
    public SettingModel? CurrentSetting { get; set; }

    partial void OnCurrentSettingChanging(
        SettingModel? oldValue,
        SettingModel? newValue,
        ref bool cancel
    )
    {
        if (oldValue is null || oldValue.IsChanged is false)
            return;
        // TODO: DialogService 确认是否保存未保存的设置
        // var result = DialogService.ShowMessageBox(
        //     this,
        //     "当前设置未保存 确定要保存吗".Translate(),
        //     "",
        //     MessageBoxButton.YesNoCancel,
        //     MessageBoxImage.Warning
        // );
        // if (result is true)
        // {
        //     oldValue.Save();
        // }
        // else if (result is false)
        // {
        //     oldValue.IsChanged = false;
        // }
        // else
        // {
        //     cancel = true;
        // }
    }

    public static ImmutableArray<EnumInfo<SubSettingModelType>> SubSettingTypes =>
        EnumInfo<SubSettingModelType>.StaticInfos;

    [ObservableProperty]
    public EnumInfo<SubSettingModelType> CurrentSubSettingType { get; set; } =
        SubSettingModelType.Graphics.GetInfo();

    [NotifyPropertyChangeFrom(nameof(CurrentSetting), nameof(CurrentSubSettingType))]
    public ISubSettingViewModel? CurrentSubSettingViewModel => GetSubSettingViewModel();

    public FilteredListWrapper<
        SettingModel,
        List<SettingModel>,
        ObservableList<SettingModel>
    > Settings { get; }

    [ObservableProperty]
    public string SearchSetting { get; set; } = string.Empty;
    #endregion

    #region Command
    [RelayCommand]
    private static void SaveSetting(SettingModel? parameter)
    {
        parameter?.Save();
    }

    [RelayCommand]
    private void ResetSetting(SettingModel? parameter)
    {
        if (parameter is null)
            return;
        // TODO: DialogService 确认重置为默认设置
        // var result = DialogService.ShowMessageBox(
        //     this,
        //     "确定重置为默认设置吗\n名称: {0}\n路径: {1}".Translate(
        //         parameter.Name,
        //         parameter.FilePath
        //     ),
        //     "",
        //     MessageBoxButton.YesNo,
        //     MessageBoxImage.Warning
        // );
        // if (result is not true)
        //     return;
        CurrentSetting?.Reset();
    }

    [RelayCommand]
    private void SaveAllSetting()
    {
        foreach (var setting in Settings)
            setting.Save();
    }

    [RelayCommand]
    private void ResetAllSetting()
    {
        // TODO: DialogService 确认重置全部设置
        // var result = DialogService.ShowMessageBox(
        //     this,
        //     "确定重置全部设置吗".Translate(),
        //     "",
        //     MessageBoxButton.YesNo,
        //     MessageBoxImage.Warning
        // );
        // if (result is not true)
        //     return;
        for (var i = 0; i < Settings.Count; i++)
        {
            var setting = Settings[i];
            setting.Disposables.Dispose();
            Settings[i] = new SettingModel() { Name = setting.Name, FilePath = setting.FilePath };
        }
    }

    [RelayCommand]
    private static void OpenFileFromExplorer(SettingModel? parameter)
    {
        if (parameter is null)
            return;
        // TODO: 跨平台从文件管理器打开设置文件
        // NativeUtils.OpenFileFromExplorer(parameter.FilePath);
    }

    [RelayCommand]
    private static void OpenFile(SettingModel? parameter)
    {
        if (parameter is null)
            return;
        // TODO: OpenLink 打开设置文件
        // NativeUtils.OpenLink(parameter.FilePath);
    }
    #endregion

    private void LoadSettings(List<SettingModel> settings)
    {
        foreach (var file in GetSettingFiles())
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            try
            {
                var setting = new Setting(File.ReadAllText(file));
                var settingModel = new SettingModel(setting) { Name = fileName, FilePath = file };
                settings.Add(settingModel);
            }
            catch (Exception)
            {
                // TODO: DialogService 载入设置出错时提示是否强制载入并重置
                // var result = DialogService.ShowMessageBox(
                //     this,
                //     "设置载入失败\n[是]: 强制载入并重置\t[否]: 取消载入\n名称: {0}\n路径: {1}\n异常: {2}".Translate(
                //         fileName,
                //         file,
                //         ex.ToString()
                //     ),
                //     "载入设置出错".Translate(),
                //     MessageBoxButton.YesNo,
                //     MessageBoxImage.Warning
                // );
                // if (result is not true)
                //     continue;
                // var setting = new SettingModel() { Name = fileName, FilePath = file };
                // settings.Add(setting);
                // setting.Save();
            }
        }
    }

    private static IEnumerable<string> GetSettingFiles()
    {
        return Directory
            .EnumerateFiles(Environment.CurrentDirectory)
            .Where(s =>
            {
                if (s.EndsWith(".lps") is false)
                    return false;
                return Path.GetFileName(s).StartsWith(nameof(Setting));
            });
    }

    private Dictionary<SubSettingModelType, ISubSettingViewModel> SubSettingViewModelDictionary =>
        field ??= CreateSubSettingViewModels();

    private Dictionary<SubSettingModelType, ISubSettingViewModel> CreateSubSettingViewModels()
    {
        // TODO: 通过 DialogService.CreateViewModel<T>() 创建子视图模型
        // return new()
        // {
        //     [SubSettingModelType.Graphics] =
        //         DialogService.CreateViewModel<GraphicsSettingViewModel>(),
        //     [SubSettingModelType.System] = DialogService.CreateViewModel<SystemSettingViewModel>(),
        //     [SubSettingModelType.Diagnostic] =
        //         DialogService.CreateViewModel<DiagnosticSettingViewModel>(),
        //     [SubSettingModelType.Interactive] =
        //         DialogService.CreateViewModel<InteractiveSettingViewModel>(),
        //     [SubSettingModelType.Customized] =
        //         DialogService.CreateViewModel<CustomizedSettingViewModel>(),
        //     [SubSettingModelType.Mod] = DialogService.CreateViewModel<ModSettingViewModel>(),
        // };
        return new()
        {
            [SubSettingModelType.Graphics] = new GraphicsSettingViewModel()
            {
                DialogService = DialogService,
            },
            [SubSettingModelType.System] = new SystemSettingViewModel()
            {
                DialogService = DialogService,
            },
            [SubSettingModelType.Diagnostic] = new DiagnosticSettingViewModel()
            {
                DialogService = DialogService,
            },
            [SubSettingModelType.Interactive] = new InteractiveSettingViewModel()
            {
                DialogService = DialogService,
            },
            [SubSettingModelType.Customized] = new CustomizedSettingViewModel()
            {
                DialogService = DialogService,
            },
            [SubSettingModelType.Mod] = new ModSettingViewModel() { DialogService = DialogService },
        };
    }

    private ISubSettingViewModel? GetSubSettingViewModel()
    {
        if (CurrentSetting is null)
            return null;
        var vm = SubSettingViewModelDictionary[CurrentSubSettingType];
        vm.Setting = CurrentSetting;
        return vm;
    }
}
