using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HKW.HKWUtils.Observable;
using LinePutScript.Localization.WPF;
using Panuon.WPF.UI;
using VPet.Solution.Models.SettingEditor;
using VPet.Solution.ViewModels.SettingEditor;
using VPet_Simulator.Windows.Interface;

namespace VPet.Solution.ViewModels;

public class MainWindowVM : ObservableClass<MainWindowVM>
{
    private readonly SettingModel? _mainSetting;

    public MainWindowVM()
    {
        LocalizeCore.StoreTranslation = true;
        LocalizeCore.LoadDefaultCulture();
        _mainSetting = SettingWindowVM.Current.ShowSettings.FirstOrDefault(m =>
            m.Name == nameof(Setting)
        );
        if (string.IsNullOrWhiteSpace(_mainSetting?.GraphicsSetting?.Language))
            CurrentCulture = LocalizeCore.CurrentCulture;
        else
            CurrentCulture = _mainSetting.GraphicsSetting.Language;

        FirstStartFailedCommand.ExecuteCommand += FirstStartFailedCommand_ExecuteCommand;
        OpenLocalTextCommand.ExecuteCommand += OpenLocalTextCommand_ExecuteCommand;
    }

    private void OpenLocalTextCommand_ExecuteCommand()
    {
        var sb = new StringBuilder();
        foreach (var a in LocalizeCore.StoreTranslationList)
            sb.AppendLine(a.Replace("\r\n", "\\r\\n"));
        MessageBoxX.Show(sb.ToString());
    }

    private void FirstStartFailedCommand_ExecuteCommand()
    {
        if (LocalizeCore.CurrentCulture == "zh-Hans")
            HKWUtils.OpenLink("https://www.bilibili.com/read/cv26510496/");
        else
            HKWUtils.OpenLink(
                "https://steamcommunity.com/games/1920960/announcements/detail/3681184905256253203"
            );
    }

    #region Property
    public static IEnumerable<string> AvailableCultures => LocalizeCore.AvailableCultures;
    #region CurrentCulture
    private string _currentCulture = string.Empty;
    public string CurrentCulture
    {
        get => _currentCulture;
        set
        {
            SetProperty(ref _currentCulture, value);
            LocalizeCore.LoadCulture(_currentCulture);
            if (
                _mainSetting is not null
                && _mainSetting.GraphicsSetting.Language != _currentCulture
            )
            {
                _mainSetting.GraphicsSetting.Language = _currentCulture;
                _mainSetting.Save();
            }
        }
    }
    #endregion
    #endregion

    #region Command
    public ObservableCommand FirstStartFailedCommand { get; } = new();
    public ObservableCommand OpenLocalTextCommand { get; } = new();
    #endregion
}
