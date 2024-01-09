using HKW.HKWUtils.Observable;
using LinePutScript;
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
    public ObservableCommand ResetSettingCommand { get; } = new();
    public ObservableCommand<SettingModel> SaveSettingCommand { get; } = new();
    public ObservableCommand SaveAllSettingCommand { get; } = new();
    #endregion
    public SettingWindowVM()
    {
        Current = this;
        ShowSettings = _settings;

        foreach (var s in LoadSettings())
            _settings.Add(s);

        PropertyChanged += MainWindowVM_PropertyChanged;
        ResetSettingCommand.ExecuteCommand += ResetSettingCommand_ExecuteCommand;
        SaveSettingCommand.ExecuteCommand += SaveSettingCommand_ExecuteCommand;
    }

    private void SaveSettingCommand_ExecuteCommand(SettingModel parameter)
    {
        parameter.Save();
    }

    private void ResetSettingCommand_ExecuteCommand()
    {
        if (
            MessageBoxX.Show(
                SettingWindow.Instance,
                "确定重置吗",
                "",
                MessageBoxButton.YesNo,
                MessageBoxIcon.Warning
            )
            is not MessageBoxResult.Yes
        )
            return;
        CurrentSetting = _settings[_settings.IndexOf(CurrentSetting)] = new SettingModel(
            new Setting("")
        );
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

    public static IEnumerable<SettingModel> LoadSettings()
    {
        foreach (
            var file in Directory
                .EnumerateFiles(Environment.CurrentDirectory)
                .Where(
                    (s) =>
                    {
                        if (s.EndsWith(".lps") is false)
                            return false;
                        return Path.GetFileName(s).StartsWith("Setting");
                    }
                )
        )
        {
            var setting = new Setting(File.ReadAllText(file));
            yield return new SettingModel(setting)
            {
                Name = Path.GetFileNameWithoutExtension(file),
                FilePath = file
            };
        }
    }
}
