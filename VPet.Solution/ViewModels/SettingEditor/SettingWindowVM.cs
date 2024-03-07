using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using HKW.HKWUtils.Observable;
using LinePutScript;
using LinePutScript.Localization.WPF;
using Panuon.WPF;
using VPet.Solution.Models;
using VPet.Solution.Models.SettingEditor;
using VPet.Solution.Views.SettingEditor;
using VPet_Simulator.Windows.Interface;

namespace VPet.Solution.ViewModels.SettingEditor;

public class SettingWindowVM : ObservableClass<SettingWindowVM>
{
    public static SettingWindowVM Current { get; private set; } = null!;
    #region Properties
    private SettingModel _currentSetting = null!;
    public SettingModel CurrentSetting
    {
        get => _currentSetting;
        set
        {
            if (_currentSetting?.IsChanged is true)
            {
                var result = MessageBox.Show(
                    "当前设置未保存 确定要保存吗".Translate(),
                    "",
                    MessageBoxButton.YesNoCancel
                );
                if (result is MessageBoxResult.Yes)
                {
                    _currentSetting.Save();
                }
                else if (result is MessageBoxResult.No)
                {
                    _currentSetting.IsChanged = false;
                }
                else if (result is MessageBoxResult.Cancel)
                {
                    return;
                }
            }
            SetProperty(ref _currentSetting!, value);
        }
    }

    public readonly ObservableCollection<SettingModel> Settings = new();

    #region ShowSettings
    private IEnumerable<SettingModel> _showSettings = null!;
    public IEnumerable<SettingModel> ShowSettings
    {
        get => _showSettings;
        set => SetProperty(ref _showSettings, value);
    }

    #endregion

    #region SearchSetting
    private string _searchSetting = string.Empty;
    public string SearchSetting
    {
        get => _searchSetting;
        set => SetProperty(ref _searchSetting, value);
    }
    #endregion

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
        LoadSettings();
        ShowSettings = Settings = new(Settings.OrderBy(m => m.Name));

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
        for (var i = 0; i < Settings.Count; i++)
            Settings[i] = new SettingModel();
    }

    private void OpenFileInExplorerCommand_ExecuteCommand(SettingModel parameter)
    {
        HKWUtils.OpenFileInExplorer(parameter.FilePath);
    }

    private void OpenFileCommand_ExecuteCommand(SettingModel parameter)
    {
        HKWUtils.OpenLink(parameter.FilePath);
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
        foreach (var setting in Settings)
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
        CurrentSetting = Settings[Settings.IndexOf(CurrentSetting)] = new SettingModel()
        {
            Name = CurrentSetting.Name,
            FilePath = CurrentSetting.FilePath
        };
        RefreshShowSettings(SearchSetting);
    }

    public void RefreshShowSettings(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            ShowSettings = Settings;
        else
            ShowSettings = Settings.Where(s =>
                s.Name.Contains(SearchSetting, StringComparison.OrdinalIgnoreCase)
            );
    }

    private void MainWindowVM_PropertyChanged(object? sender, PropertyChangedEventArgs e)
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
                Settings.Add(settingModel);
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
                    )
                    is not MessageBoxResult.Yes
                )
                    return;
                var setting = new SettingModel() { Name = fileName, FilePath = file };
                Settings.Add(setting);
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
}
