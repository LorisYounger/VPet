using System.Collections.Immutable;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HanumanInstitute.MvvmDialogs;
using HanumanInstitute.MvvmDialogs.FileSystem;
using HanumanInstitute.MvvmDialogs.FrameworkDialogs;
using HKW.HKWUtils;
using HKW.HKWUtils.Collections;
using HKW.HKWUtils.Extensions;
using HKW.HKWUtils.Observable;
using HKW.MVVM;
using HKW.MVVM.SourceGenerator;
using LinePutScript.Localization;
using VPet.Solution.MultiPlatform.Models;

namespace VPet.Solution.MultiPlatform.ViewModels;

public partial class SettingViewModel : CloseableViewModel
{
    public SettingViewModel()
    {
        Settings = new([], [], x =>
            x.Name.Contains(SearchSetting, StringComparison.CurrentCultureIgnoreCase)
        );

        this.WhenAnyValue(x => x.SearchSetting)
            .Subscribe(_ => Settings.Refresh())
            .DisposeWith(Disposables);

        _ = InitializeSettingsAsync();
    }

    [DIMethod]
    internal void InitializeSubSettingViewModels()
    {
        SubSettingViewModels = CreateSubSettingViewModels();
    }

    private async Task InitializeSettingsAsync()
    {
        var loaded = new List<SettingModel>();
        await LoadSettingsAsync(loaded);
        Settings.BatchUpdate(l =>
        {
            l.Clear();
            l.AddRange(loaded);
        });

        CurrentSetting = Settings.FirstOrDefault();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var setting in Settings)
            {
                setting.Disposables.Dispose();
                setting.StorageFile?.Dispose();
            }
        }
        base.Dispose(disposing);
    }

    #region Property
    [ObservableProperty]
    public SettingModel? CurrentSetting { get; set; }

    private bool _isConfirmingSettingChange;

    partial void OnCurrentSettingChanging(
        SettingModel? oldValue,
        SettingModel? newValue,
        ref bool cancel
    )
    {
        if (_isConfirmingSettingChange)
            return;

        if (oldValue is null || oldValue.IsChanged is false)
            return;

        // 先同步拦截属性变更
        cancel = true;
        _ = ConfirmCurrentSettingChangeAsync(oldValue, newValue);
    }

    private async Task ConfirmCurrentSettingChangeAsync(
        SettingModel oldValue,
        SettingModel? newValue
    )
    {
        _isConfirmingSettingChange = true;
        try
        {
            var result = await DialogService.ShowMessageBoxAsync(
                this,
                "当前设置未保存 确定要保存吗".Translate(),
                "",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning
            );

            if (result is true)
            {
                await oldValue.SaveAsync();
                CurrentSetting = newValue;
            }
            else if (result is false)
            {
                oldValue.Reload(); // 若仅清除标记不回滚内存值, 可保留 oldValue.IsChanged = false;
                CurrentSetting = newValue;
            }
            // result is null (Cancel): 保持原样即可, 因为之前已经 cancel = true 拦截了切换
        }
        finally
        {
            _isConfirmingSettingChange = false;
        }
    }

    partial void OnCurrentSettingChanged(SettingModel? oldValue, SettingModel? newValue)
    {
        foreach (var vm in SubSettingViewModels.Values)
            vm.Setting = newValue!;
    }

    public static ImmutableArray<EnumInfo<SubSettingModelType>> SubSettingTypes =>
        EnumInfo<SubSettingModelType>.StaticInfos;

    [ObservableProperty]
    public EnumInfo<SubSettingModelType> CurrentSubSettingType { get; set; } =
        SubSettingModelType.Graphics.GetInfo();

    public FilteredListWrapper<
        SettingModel,
        ObservableList<SettingModel>,
        ObservableList<SettingModel>
    > Settings { get; }

    [ObservableProperty]
    public string SearchSetting { get; set; } = string.Empty;
    #endregion

    #region Command
    [RelayCommand]
    private static async Task SaveSetting(SettingModel? parameter)
    {
        if (parameter is not null)
            await parameter.SaveAsync();
    }

    [RelayCommand]
    private async Task ResetSetting(SettingModel? parameter)
    {
        if (parameter is null)
            return;
        var result = await DialogService.ShowMessageBoxAsync(
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

    [RelayCommand]
    private async Task SaveAllSetting()
    {
        foreach (var setting in Settings)
            await setting.SaveAsync();
    }

    [RelayCommand]
    private async Task ResetAllSetting()
    {
        var result = await DialogService.ShowMessageBoxAsync(
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
            setting.Disposables.Dispose();
            Settings[i] = new SettingModel()
            {
                Name = setting.Name,
                FilePath = setting.FilePath,
                StorageFile = setting.StorageFile,
                IsChanged = true,
            };
        }
    }

    [RelayCommand]
    private static void OpenFileFromExplorer(SettingModel? parameter)
    {
        if (parameter is null)
            return;
        NativeUtils.RevealInFileExplorer(parameter.FilePath);
    }

    [RelayCommand]
    private static async Task OpenFile(SettingModel? parameter)
    {
        if (parameter is null)
            return;
        if (parameter.StorageFile is not null)
            await NativeUtils.LaunchAsync(parameter.StorageFile);
        else
            await NativeUtils.LaunchAsync(parameter.FilePath);
    }
    #endregion

    private async Task LoadSettingsAsync(List<SettingModel> settings)
    {
        var baseFolder = VPetSolution.BaseFolder;
        if (baseFolder is null)
            return;

        await ModSettingModel.EnsureLocalModsLoadedAsync();

        await foreach (var item in baseFolder.GetItemsAsync())
        {
            if (
                item is not IDialogStorageFile file
                || file.Name.EndsWith(".lps", StringComparison.OrdinalIgnoreCase) is false
                || file.Name.StartsWith(nameof(Setting), StringComparison.Ordinal) is false
            )
            {
                item.Dispose();
                continue;
            }

            var fileName = Path.GetFileNameWithoutExtension(file.Name);
            var displayPath = file.GetDisplayPath();
            try
            {
                var content = await file.ReadAllTextAsync();
                var setting = new Setting(content);
                var settingModel = new SettingModel(setting)
                {
                    Name = fileName,
                    FilePath = displayPath,
                    StorageFile = file,
                };
                settings.Add(settingModel);
            }
            catch (Exception ex)
            {
                var result = await DialogService.ShowMessageBoxAsync(
                    this,
                    "设置载入失败\n[是]: 强制载入并重置\t[否]: 取消载入\n名称: {0}\n路径: {1}\n异常: {2}".Translate(
                        fileName,
                        displayPath,
                        ex.ToString()
                    ),
                    "载入设置出错".Translate(),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );
                if (result is not true)
                {
                    file.Dispose();
                    continue;
                }
                var setting = new SettingModel()
                {
                    Name = fileName,
                    FilePath = displayPath,
                    StorageFile = file,
                };
                settings.Add(setting);
                await setting.SaveAsync();
            }
        }
    }

    public Dictionary<SubSettingModelType, ISubSettingViewModel> SubSettingViewModels
    {
        get;
        private set;
    }

    private Dictionary<SubSettingModelType, ISubSettingViewModel> CreateSubSettingViewModels()
    {
        return new()
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

    public override async Task OnClosingAsync(CancelEventArgs e)
    {
        var changedSettings = Settings.Where(s => s.IsChanged).ToList();
        if (changedSettings.Count == 0)
        {
            e.Cancel = false;
            return;
        }

        var result = await DialogService.ShowMessageBoxAsync(
            this,
            "当前设置未保存 确定要保存吗".Translate(),
            "",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Warning
        );

        if (result is true)
        {
            foreach (var setting in changedSettings)
                await setting.SaveAsync();
            e.Cancel = false;
        }
        else if (result is false)
        {
            e.Cancel = false;
        }
        else
        {
            e.Cancel = true;
        }
    }
}
