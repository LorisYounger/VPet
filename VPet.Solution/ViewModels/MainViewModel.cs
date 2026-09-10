using System.Text;
using System.Windows.Input;
using HanumanInstitute.MvvmDialogs;
using HKW.HKWReactiveUI;
using HKW.HKWUtils;
using LinePutScript;
using LinePutScript.Localization.WPF;
using Panuon.WPF.UI;
using ReactiveUI;
using ReactiveUI.Builder;
using ReactiveUI.Primitives;
using VPet.Solution.Models.SettingEditor;
using VPet.Solution.ViewModels.SettingEditor;

namespace VPet.Solution.ViewModels;

public partial class MainViewModel : CloseableViewModel
{
    private readonly SettingModel? _mainSetting;

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
        //_mainSetting = SettingViewModel.Current.ShowSettings.FirstOrDefault(m =>
        //    m.Name == nameof(Setting)
        //);
        if (string.IsNullOrWhiteSpace(_mainSetting?.GraphicsSetting?.Language))
            CurrentCulture = LocalizeCore.CurrentCulture;
        else
            CurrentCulture = _mainSetting.GraphicsSetting.Language;
    }

    #region Property
    public static string[] AvailableCultures => LocalizeCore.AvailableCultures;

    //{ get; set; }

    [ReactiveProperty]
    public string CurrentCulture { get; set; }
    #endregion

    #region Command


    [ReactiveCommand()]
    private void OpenSetting()
    {
        DialogService.ShowInstance<SettingViewModel>(this, null);
    }

    [ReactiveCommand]
    private void OpenLocalText()
    {
        var sb = new StringBuilder();
        foreach (var a in LocalizeCore.StoreTranslationList)
            sb.AppendLine(a.Replace("\r\n", "\\r\\n"));
        DialogService.ShowMessageBox(this, sb.ToString());
    }

    [ReactiveCommand]
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

    public override void OnClosed()
    {
        //DialogService.CloseInstance<SettingViewModel>();
        base.OnClosed();
    }

    partial class MainViewModelReactiveObjectHelper
    {
        partial void OnCurrentCultureChanged(string oldValue, string newValue)
        {
            LocalizeCore.LoadCulture(newValue);
            if (
                _source._mainSetting is not null
                && _source._mainSetting.GraphicsSetting.Language != newValue
            )
            {
                _source._mainSetting.GraphicsSetting.Language = newValue;
                _source._mainSetting.Save();
            }
        }
    }
}
