using System.Text;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HanumanInstitute.MvvmDialogs;
using HKW.HKWUtils;
using LinePutScript;
using LinePutScript.Localization.WPF;
using Panuon.WPF.UI;
using VPet.Solution.Models.SettingEditor;
using VPet.Solution.ViewModels.SaveViewer;
using VPet.Solution.ViewModels.SettingEditor;

namespace VPet.Solution.ViewModels;

public partial class MainViewModel : CloseableViewModel
{
    public MainViewModel(IDialogService dialogService)
        : base(dialogService)
    {
        EnumInfo.DefaultToString = x =>
            x.IsFlaggable
                ? string.Join(
                    ", ",
                    x.GetFlagInfos().Select(static i => $"{i.EnumType.Name}_{i.Value}".Translate())
                )
                : $"{x.EnumType.Name}_{x.Value}".Translate();

        LocalizeCore.StoreTranslation = true;
        LocalizeCore.LoadDefaultCulture();
    }

    #region Property
    public string[] AvailableCultures => LocalizeCore.AvailableCultures;

    [ObservableProperty]
    public string CurrentCulture { get; set; } = string.Empty;
    #endregion

    #region Command


    [RelayCommand]
    private void OpenSettingEditor()
    {
        DialogService.ShowInstance<SettingViewModel>(this, null);
    }

    [RelayCommand]
    private void OpenSaveViewer()
    {
        DialogService.ShowInstance<SaveViewModel>(this, null);
    }

    [RelayCommand]
    private void OpenLocalText()
    {
        var sb = new StringBuilder();
        foreach (var a in LocalizeCore.StoreTranslationList)
            sb.AppendLine(a.Replace("\r\n", "\\r\\n"));
        DialogService.ShowMessageBox(this, sb.ToString());
    }

    [RelayCommand]
    private void FirstStartFailed()
    {
        if (LocalizeCore.CurrentCulture == "zh-Hans")
            NativeUtils.OpenLink("https://www.bilibili.com/read/cv26510496/");
        else
            NativeUtils.OpenLink(
                "https://steamcommunity.com/games/1920960/announcements/detail/3681184905256253203"
            );
    }
    #endregion
}
