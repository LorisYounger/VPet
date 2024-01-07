using HKW.HKWUtils.Observable;
using LinePutScript;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VPet.Solution.Models;
using VPet.Solution.Models.SettingEditor;
using VPet_Simulator.Windows.Interface;

namespace VPet.Solution.ViewModels.SettingEditor;

public class SettingWindowVM : ObservableClass<SettingWindowVM>
{
    public static SettingWindowVM Current { get; private set; }

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

    public SettingWindowVM()
    {
        Current = this;
        ShowSettings = _settings;

        foreach (var s in LoadSettings())
            _settings.Add(s);

        PropertyChanged += MainWindowVM_PropertyChanged;
    }

    private void MainWindowVM_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SearchSetting))
        {
            if (string.IsNullOrWhiteSpace(SearchSetting))
                ShowSettings = _settings;
            else
                ShowSettings = _settings.Where(
                    s => s.Name.Contains(SearchSetting, StringComparison.OrdinalIgnoreCase)
                );
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
