using System.Collections.Immutable;
using System.IO;
using System.Runtime.InteropServices;
using HanumanInstitute.MvvmDialogs;
using HanumanInstitute.MvvmDialogs.FrameworkDialogs;
using HKW.HKWReactiveUI;
using HKW.HKWUtils;
using HKW.HKWUtils.Collections;
using HKW.HKWUtils.Extensions;
using HKW.HKWUtils.Observable;
using LinePutScript.Localization.WPF;
using ReactiveUI;
using ReactiveUI.Primitives;
using VPet.Solution.Models.SettingEditor;

namespace VPet.Solution.ViewModels.SettingEditor;

public partial class SettingViewModel : CloseableViewModel
{
    public SettingViewModel(IDialogService dialogService)
        : base(dialogService)
    {
        Settings = new(
            [],
            [],
            x => x.Name.Contains(SearchSetting, StringComparison.CurrentCultureIgnoreCase)
        );
        Settings.BatchUpdate(l =>
        {
            LoadSettings(l);
        });

        this.WhenAnyValue(x => x.SearchSetting)
            .Subscribe(_ => Settings.Refresh())
            .DisposeWith(Disposables);

        CurrentSetting = Settings.FirstOrDefault();

        _subSettingViewModelDictionary = new()
        {
            [SubSettingModelType.Graphics] =
                DialogService.CreateViewModel<GraphicsSettingViewModel>(),
            [SubSettingModelType.System] = DialogService.CreateViewModel<SystemSettingViewModel>(),
            [SubSettingModelType.Diagnostic] =
                DialogService.CreateViewModel<DiagnosticSettingViewModel>(),
            [SubSettingModelType.Interactive] =
                DialogService.CreateViewModel<InteractiveSettingViewModel>(),
            [SubSettingModelType.Customized] =
                DialogService.CreateViewModel<CustomizedSettingViewModel>(),
            [SubSettingModelType.Mod] = DialogService.CreateViewModel<ModSettingViewModel>(),
        };
    }

    public override void OnClosed()
    {
        base.OnClosed();
        Disposables.Dispose();
        foreach (var setting in Settings)
            setting.Disposables.Dispose();
    }

    #region Property
    [ReactiveProperty]
    public SettingModel? CurrentSetting { get; set; }

    public static ImmutableArray<EnumInfo<SubSettingModelType>> SubSettingTypes =>
        EnumInfo<SubSettingModelType>.StaticInfos;

    [ReactiveProperty]
    public EnumInfo<SubSettingModelType> CurrentSubSettingType { get; set; } =
        SubSettingModelType.Graphics.GetInfo();

    [NotifyPropertyChangeFrom(nameof(CurrentSetting), nameof(CurrentSubSettingType))]
    public ISubSettingViewModel? CurrentSubSettingViewModel => GetSubSettingViewModel();

    public FilteredListWrapper<
        SettingModel,
        List<SettingModel>,
        ObservableList<SettingModel>
    > Settings { get; }

    [ReactiveProperty]
    public string SearchSetting { get; set; } = string.Empty;
    #endregion
    #region Command
    [ReactiveCommand]
    private static void SaveSetting(SettingModel parameter)
    {
        parameter.Save();
    }

    [ReactiveCommand]
    private void ResetSetting(SettingModel parameter)
    {
        var result = DialogService.ShowMessageBox(
            this,
            "确定重置为默认设置吗\n名称: {0}\n路径: {1}".Translate(
                parameter.Name,
                parameter.FilePath
            ),
            "",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning
        );
        if (result is not true)
            return;
        CurrentSetting?.Reset();
    }

    [ReactiveCommand]
    private void SaveAllSetting()
    {
        foreach (var setting in Settings)
            setting.Save();
    }

    [ReactiveCommand]
    private void ResetAllSetting()
    {
        var result = DialogService.ShowMessageBox(
            this,
            "确定重置全部设置吗".Translate(),
            "",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning
        );
        if (result is not true)
            return;
        for (var i = 0; i < Settings.Count; i++)
        {
            var setting = Settings[i];
            Settings[i] = new SettingModel() { Name = setting.Name, FilePath = setting.FilePath };
        }
    }

    [ReactiveCommand]
    private static void OpenFileFromExplorer(SettingModel parameter)
    {
        NativeUtils.OpenFileFromExplorer(parameter.FilePath);
    }

    [ReactiveCommand]
    private static void OpenFile(SettingModel parameter)
    {
        NativeUtils.OpenLink(parameter.FilePath);
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
            catch (Exception ex)
            {
                var result = DialogService.ShowMessageBox(
                    this,
                    "设置载入失败\n[是]: 强制载入并重置\t[否]: 取消载入\n名称: {0}\n路径: {1}\n异常: {2}".Translate(
                        fileName,
                        file,
                        ex.ToString()
                    ),
                    "载入设置出错".Translate(),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );
                if (result is not true)
                    continue;
                var setting = new SettingModel() { Name = fileName, FilePath = file };
                settings.Add(setting);
                setting.Save();
            }
        }
    }

    private static IEnumerable<string> GetSettingFiles()
    {
        return Directory
            .EnumerateFiles(Environment.CurrentDirectory)
            .Where(
                (s) =>
                {
                    if (s.EndsWith(".lps") is false)
                        return false;
                    return Path.GetFileName(s).StartsWith(nameof(Setting));
                }
            );
    }

    private readonly Dictionary<
        SubSettingModelType,
        ISubSettingViewModel
    > _subSettingViewModelDictionary;

    private ISubSettingViewModel? GetSubSettingViewModel()
    {
        if (CurrentSetting is null)
            return null;
        var vm = _subSettingViewModelDictionary[CurrentSubSettingType];
        vm.Setting = CurrentSetting;
        return vm;
    }

    partial class SettingViewModelReactiveObjectHelper
    {
        partial void OnCurrentSettingChanging(
            SettingModel oldValue,
            SettingModel newValue,
            ref bool cancel
        )
        {
            if (oldValue is null || oldValue.IsChanged is false)
                return;
            var result = _source.DialogService.ShowMessageBox(
                _source,
                "当前设置未保存 确定要保存吗".Translate(),
                "",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning
            );
            if (result is true)
            {
                oldValue.Save();
            }
            else if (result is false)
            {
                oldValue.IsChanged = false;
            }
            else
            {
                cancel = true;
            }
        }
    }
}
