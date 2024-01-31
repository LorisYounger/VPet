using HKW.HKWUtils.Observable;
using LinePutScript;
using LinePutScript.Localization.WPF;
using Panuon.WPF.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using VPet.Solution.Models;
using VPet.Solution.Models.SettingEditor;
using VPet.Solution.Views.SettingEditor;
using VPet_Simulator.Windows.Interface;

namespace VPet.Solution.ViewModels.SettingEditor;

public class SettingWindowVM : ObservableClass<SettingWindowVM>
{
    public static SettingWindowVM Current { get; private set; }

    #region Properties
    private SettingModel _currentSettings;
    public SettingModel CurrentSetting
    {
        get => _currentSettings;
        set => SetProperty(ref _currentSettings, value);
    }

    private readonly ObservableCollection<SettingModel> _settings = new();

    private IEnumerable<SettingModel> _showSettings;
    public IEnumerable<SettingModel> ShowSettings
    {
        get => _showSettings;
        set => SetProperty(ref _showSettings, value);
    }

    private string _searchSetting;
    public string SearchSetting
    {
        get => _searchSetting;
        set => SetProperty(ref _searchSetting, value);
    }

    #endregion

    #region Command
    /// <summary>
    /// 打开文件
    /// </summary>
    public ObservableCommand<SettingModel> OpenFileCommand { get; } = new();

    /// <summary>
    /// 从资源管理器打开
    /// </summary>
    public ObservableCommand<SettingModel> OpenFileInExplorerCommand { get; } = new();

    /// <summary>
    /// 重置
    /// </summary>
    public ObservableCommand<SettingModel> ResetSettingCommand { get; } = new();

    /// <summary>
    /// 保存
    /// </summary>
    public ObservableCommand<SettingModel> SaveSettingCommand { get; } = new();

    /// <summary>
    /// 保存全部
    /// </summary>
    public ObservableCommand SaveAllSettingCommand { get; } = new();

    /// <summary>
    /// 重置全部
    /// </summary>
    public ObservableCommand ResetAllSettingCommand { get; } = new();
    #endregion
    public SettingWindowVM()
    {
        Current = this;
        ShowSettings = _settings;
        LoadSettings();

        PropertyChanged += MainWindowVM_PropertyChanged;
        OpenFileCommand.ExecuteCommand += OpenFileCommand_ExecuteCommand;
        OpenFileInExplorerCommand.ExecuteCommand += OpenFileInExplorerCommand_ExecuteCommand;
        ResetSettingCommand.ExecuteCommand += ResetSettingCommand_ExecuteCommand;
        SaveSettingCommand.ExecuteCommand += SaveSettingCommand_ExecuteCommand;
        SaveAllSettingCommand.ExecuteCommand += SaveAllSettingCommand_ExecuteCommand;
        ResetAllSettingCommand.ExecuteCommand += ResetAllSettingCommand_ExecuteCommand;
    }

    private void ResetAllSettingCommand_ExecuteCommand()
    {
        if (
            MessageBox.Show(
                SettingWindow.Instance,
                "确定全部重置吗".Translate(),
                "",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            )
            is not MessageBoxResult.Yes
        )
            return;
        for (var i = 0; i < _settings.Count; i++)
            _settings[i] = new SettingModel();
    }

    private void OpenFileInExplorerCommand_ExecuteCommand(SettingModel parameter)
    {
        Utils.OpenFileInExplorer(parameter.FilePath);
    }

    private void OpenFileCommand_ExecuteCommand(SettingModel parameter)
    {
        Utils.OpenLink(parameter.FilePath);
    }

    private void SaveAllSettingCommand_ExecuteCommand()
    {
        if (
            MessageBox.Show(
                SettingWindow.Instance,
                "确定全部保存吗".Translate(),
                "",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            )
            is not MessageBoxResult.Yes
        )
            return;
        foreach (var setting in _settings)
            setting.Save();
    }

    private void SaveSettingCommand_ExecuteCommand(SettingModel parameter)
    {
        parameter.Save();
    }

    private void ResetSettingCommand_ExecuteCommand(SettingModel parameter)
    {
        if (
            MessageBox.Show(
                SettingWindow.Instance,
                "确定重置设置吗\n名称: {0}\n路径: {1}".Translate(parameter.Name, parameter.FilePath),
                "",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            )
            is not MessageBoxResult.Yes
        )
            return;
        CurrentSetting = _settings[_settings.IndexOf(CurrentSetting)] = new SettingModel()
        {
            Name = CurrentSetting.Name,
            FilePath = CurrentSetting.FilePath
        };
        RefreshShowSettings(SearchSetting);
    }

    public void RefreshShowSettings(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            ShowSettings = _settings;
        else
            ShowSettings = _settings.Where(
                s => s.Name.Contains(SearchSetting, StringComparison.OrdinalIgnoreCase)
            );
    }

    private void MainWindowVM_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SearchSetting))
        {
            RefreshShowSettings(SearchSetting);
        }
    }

    private void LoadSettings()
    {
        foreach (var file in GetSettingFiles())
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            try
            {
                var setting = new Setting(File.ReadAllText(file));
                var settingModel = new SettingModel(setting) { Name = fileName, FilePath = file };
                _settings.Add(settingModel);
            }
            catch (Exception ex)
            {
                if (
                    MessageBox.Show(
                        "设置载入失败, 是否强制载入并重置\n[是]: 载入并重置\t[否]: 取消载入\n名称: {0}\n路径: {1}\n异常: {2}".Translate(
                            fileName,
                            file,
                            ex.ToString()
                        ),
                        "载入设置出错".Translate(),
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning
                    ) is MessageBoxResult.Yes
                )
                    _settings.Add(new SettingModel() { Name = fileName, FilePath = file });
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
                    return Path.GetFileName(s).StartsWith("Setting");
                }
            );
    }
}
