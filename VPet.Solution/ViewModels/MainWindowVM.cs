using HKW.HKWUtils.Observable;
using LinePutScript.Localization.WPF;
using Panuon.WPF.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VPet.Solution.ViewModels;

public class MainWindowVM : ObservableClass<MainWindowVM>
{
    public MainWindowVM()
    {
        LocalizeCore.StoreTranslation = true;
        LocalizeCore.LoadDefaultCulture();
        CurrentCulture = LocalizeCore.CurrentCulture;
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
            Utils.OpenLink("https://www.bilibili.com/read/cv26510496/");
        else
            Utils.OpenLink("https://steamcommunity.com/games/1920960/announcements/detail/3681184905256253203");
    }

    #region Property
    public IEnumerable<string> AvailableCultures => LocalizeCore.AvailableCultures;
    #region CurrentCulture
    private string _currentCulture = string.Empty;
    public string CurrentCulture
    {
        get => _currentCulture;
        set
        {
            SetProperty(ref _currentCulture, value);
            LocalizeCore.LoadCulture(_currentCulture);
        }
    }
    #endregion
    #endregion

    #region Command
    public ObservableCommand FirstStartFailedCommand { get; } = new();
    public ObservableCommand OpenLocalTextCommand { get; } = new();
    #endregion
}
