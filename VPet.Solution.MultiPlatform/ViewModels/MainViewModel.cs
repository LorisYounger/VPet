using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HanumanInstitute.MvvmDialogs;
using HKW.HKWUtils;
using LinePutScript.Localization;
using VPet.Solution.MultiPlatform.Models;
using VPet.Solution.MultiPlatform.ViewModels;

namespace VPet.Solution.MultiPlatform.ViewModels;

public partial class MainViewModel : CloseableViewModel
{
    public MainViewModel()
    {
        EnumInfo.DefaultToString = x =>
            x.IsFlaggable
                ? string.Join(
                    ", ",
                    x.GetFlagInfos().Select(static i => $"{i.EnumType.Name}_{i.Value}".Translate())
                )
                : $"{x.EnumType.Name}_{x.Value}".Translate();

        LocalizeCore.StoreTranslation = true;
        // 本体语言在 mod/0000_core/lang, 必须先注册再加载默认语言
        ModLoader.LoadCoreLanguage();
        LocalizeCore.LoadDefaultCulture();
        if (LocalizeCore.CurrentCulture is null)
            LocalizeCore.LoadCulture("zh-Hans");
        CurrentCulture = LocalizeCore.CurrentCulture!;
    }

    #region Property
    public static string[] AvailableCultures => LocalizeCore.AvailableCultures;

    [ObservableProperty]
    public string CurrentCulture { get; set; }

    partial void OnCurrentCultureChanged(string oldValue, string newValue)
    {
        if (string.IsNullOrEmpty(newValue) is false)
            LocalizeCore.LoadCulture(newValue);
    }

    [ObservableProperty]
    public string BaseDirectory { get; set; } = VPetSolution.BaseDirectory;
    #endregion

    #region Command

    [RelayCommand]
    private async Task SetBaseDirectory()
    {
        var result = await DialogService.ShowOpenFolderDialogAsync(this, new());
        if (result is null)
            return;
        if (result.TryGetLocalPath() is { } localPath && Path.Exists(localPath) is false)
        {
            result.Dispose();
            await DialogService.ShowMessageBoxAsync(
                this,
                "无效的文件夹".Translate(),
                "错误".Translate(),
                icon: HanumanInstitute.MvvmDialogs.FrameworkDialogs.MessageBoxImage.Warning
            );
            return;
        }
        VPetSolution.BaseFolder = result;
        BaseDirectory = VPetSolution.BaseDirectory;
    }

    [RelayCommand]
    private void OpenSettingEditor()
    {
        var vm = DialogService.CreateViewModel<SettingViewModel>();
        DialogService.Show(this, vm);
    }

    [RelayCommand]
    private void OpenSaveViewer()
    {
        var vm = DialogService.CreateViewModel<SaveViewModel>();
        DialogService.Show(this, vm);
    }

    [RelayCommand]
    private async Task OutputLocalText()
    {
        var sb = new StringBuilder();
        foreach (var a in LocalizeCore.StoreTranslationList)
            sb.AppendLine(a.Replace("\r\n", "\\r\\n"));
        var result = await DialogService.ShowSaveFileDialogAsync(
            this,
            new() { DefaultExtension = "txt" }
        );
        if (result is null)
            return;
        await result.WriteAllTextAsync(sb.ToString());
    }
#pragma warning disable S1075
    [RelayCommand]
    private void FirstStartFailed()
    {
        if (LocalizeCore.CurrentCulture == "zh-Hans")
        {
            NativeUtils.Launch("https://www.bilibili.com/read/cv26510496/");
        }
        else
        {
            NativeUtils.Launch(
                "https://steamcommunity.com/games/1920960/announcements/detail/3681184905256253203"
            );
        }
    }

    [RelayCommand]
    private void WantOpenVpet()
    {
        if (LocalizeCore.CurrentCulture == "zh-Hant")
        {
            NativeUtils.Launch(
                "https://wiki.exlb.net/zh-hant/vpet/qa/startup#%E9%99%84%E4%B8%89_%E5%95%9F%E5%8B%95%E5%BE%8C%E5%87%BA%E7%8F%BE%E5%95%8F%E9%A1%8C%E8%A7%A3%E6%B1%BA%E5%B7%A5%E5%85%B7%E7%AA%97%E5%8F%A3_%E5%A6%82%E4%BD%95%E5%88%87%E6%8F%9B%E5%9B%9E%E6%A1%8C%E5%AF%B5"
            );
        }
        else if (
            LocalizeCore.CurrentCulture.Contains("zh", StringComparison.CurrentCultureIgnoreCase)
        )
        {
            NativeUtils.Launch(
                "https://wiki.exlb.net/vpet/qa/startup#%E9%99%84%E4%B8%89_%E5%90%AF%E5%8A%A8%E5%90%8E%E5%87%BA%E7%8E%B0%E9%97%AE%E9%A2%98%E8%A7%A3%E5%86%B3%E5%B7%A5%E5%85%B7%E7%AA%97%E5%8F%A3_%E5%A6%82%E4%BD%95%E5%88%87%E6%8D%A2%E5%9B%9E%E6%A1%8C%E5%AE%A0"
            );
        }
        else
        {
            NativeUtils.Launch(
                "https://wiki.exlb.net/en/vpet/qa/startup#appendix_3the_troubleshooting_tool_opens_at_startup_how_to_switch_back_to_the_desktop_pet"
            );
        }
    }
    #endregion
#pragma warning restore S1075
}
