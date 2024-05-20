using System.Windows;
using LinePutScript.Localization.WPF;
using VPet.Solution.Models.SettingEditor;

namespace VPet.Solution.ViewModels.SettingEditor;

public class ModSettingPageVM : ObservableClass<ModSettingPageVM>
{
    #region ObservableProperty
    private ModSettingModel _modSetting;
    public ModSettingModel ModSetting
    {
        get => _modSetting;
        set => SetProperty(ref _modSetting, value);
    }

    #region ShowMods
    private IEnumerable<ModModel> _showMods;
    public IEnumerable<ModModel> ShowMods
    {
        get => _showMods;
        set => SetProperty(ref _showMods, value);
    }
    #endregion

    #region SearchMod
    private string _searchMod;
    public string SearchMod
    {
        get => _searchMod;
        set
        {
            SetProperty(ref _searchMod, value);
            RefreshShowMods(value);
        }
    }
    #endregion

    #region CurrentModMoel
    private ModModel _currentModModel = null!;
    public ModModel CurrentModModel
    {
        get => _currentModModel;
        set
        {
            if (_currentModModel is not null)
                _currentModModel.PropertyChangingX -= CurrentModModel_PropertyChangingX;
            SetProperty(ref _currentModModel!, value);
            if (_currentModModel is not null)
                _currentModModel.PropertyChangingX += CurrentModModel_PropertyChangingX;
        }
    }

    private void CurrentModModel_PropertyChangingX(ModModel sender, PropertyChangingXEventArgs e)
    {
        if (e.PropertyName == nameof(ModModel.IsPass) && e.NewValue is true)
        {
            if (
                MessageBox.Show(
                    "是否启用 {0} 的代码插件?\n一经启用,该插件将会允许访问该系统(包括外部系统)的所有数据\n如果您不确定,请先使用杀毒软件查杀检查".Translate(
                        sender.Name
                    ),
                    "启用 {0} 的代码插件?".Translate(sender.Name),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                ) is MessageBoxResult.Yes
            )
            {
                sender.IsEnabled = true;
            }
            else
                e.Cancel = true;
        }
    }
    #endregion


    #endregion

    #region Command
    //public ObservableCommand AddModCommand { get; } = new();
    //public ObservableCommand<ModModel> RemoveModCommand { get; } = new();
    public ObservableCommand ClearFailModsCommand { get; } = new();
    public ObservableCommand ClearModsCommand { get; } = new();
    public ObservableCommand<ModModel> OpenModPathCommand { get; } = new();
    public ObservableCommand<ModModel> OpenSteamCommunityCommand { get; } = new();
    #endregion


    public ModSettingPageVM()
    {
        SettingWindowVM.Current.PropertyChangedX += Current_PropertyChangedX;
        ClearFailModsCommand.ExecuteCommand += ClearFailModsCommand_ExecuteCommand;
        ClearModsCommand.ExecuteCommand += ClearModsCommand_ExecuteCommand;
        OpenModPathCommand.ExecuteCommand += OpenModPathCommand_ExecuteCommand;
        OpenSteamCommunityCommand.ExecuteCommand += OpenSteamCommunityCommand_ExecuteCommand;
    }

    private void ClearModsCommand_ExecuteCommand()
    {
        if (
            MessageBox.Show("确定清除全部模组吗", "", MessageBoxButton.YesNo, MessageBoxImage.Warning)
            is not MessageBoxResult.Yes
        )
            return;
        ModSetting.Mods.Clear();
        SearchMod = string.Empty;
    }

    private void ClearFailModsCommand_ExecuteCommand()
    {
        if (
            MessageBox.Show("确定清除全部失效模组吗", "", MessageBoxButton.YesNo, MessageBoxImage.Warning)
            is not MessageBoxResult.Yes
        )
            return;
        for (var i = 0; i < ModSetting.Mods.Count; i++)
        {
            if (ModSetting.Mods[i].IsEnabled is null)
                ModSetting.Mods.RemoveAt(i--);
        }
        SearchMod = string.Empty;
    }

    private void OpenSteamCommunityCommand_ExecuteCommand(ModModel parameter)
    {
        HKWUtils.OpenLink(
            "https://steamcommunity.com/sharedfiles/filedetails/?id=" + parameter.ItemId
        );
    }

    private void OpenModPathCommand_ExecuteCommand(ModModel parameter)
    {
        try
        {
            HKWUtils.OpenLink(parameter.ModPath);
        }
        catch
        {
            MessageBox.Show("未在路径\n{0}\n中找到模组".Translate(parameter.ModPath));
        }
    }

    private void Current_PropertyChangedX(SettingWindowVM sender, PropertyChangedXEventArgs e)
    {
        if (
            e.PropertyName == nameof(SettingWindowVM.CurrentSetting)
            && sender.CurrentSetting is not null
        )
        {
            ModSetting = sender.CurrentSetting.ModSetting;
            SearchMod = string.Empty;
        }
    }

    public void RefreshShowMods(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            ShowMods = ModSetting.Mods;
        else
            ShowMods = ModSetting.Mods.Where(s =>
                s.Name.Contains(SearchMod, StringComparison.OrdinalIgnoreCase)
            );
    }
}
