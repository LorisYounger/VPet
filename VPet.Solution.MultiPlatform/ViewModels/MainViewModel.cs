using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HKW.HKWUtils;
using LinePutScript.Localization;
using VPet.Solution.MultiPlatform.Models;
using VPet.Solution.MultiPlatform.ViewModels.SaveViewer;
using VPet.Solution.MultiPlatform.ViewModels.SettingEditor;

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
        if (LocalizeCore.CurrentCulture == "null")
            LocalizeCore.LoadCulture("zh-Hans");
        CurrentCulture = LocalizeCore.CurrentCulture;
    }

    #region Property
    public string[] AvailableCultures => LocalizeCore.AvailableCultures;

    [ObservableProperty]
    public string CurrentCulture { get; set; } = string.Empty;

    partial void OnCurrentCultureChanged(string oldValue, string newValue)
    {
        if (string.IsNullOrEmpty(newValue) is false)
            LocalizeCore.LoadCulture(newValue);
    }
    #endregion

    #region Command
    [RelayCommand]
    private void OpenSettingEditor()
    {
        // TODO: DialogService 打开设置编辑器窗口
        // DialogService.ShowInstance<SettingViewModel>(this, null);
    }

    [RelayCommand]
    private void OpenSaveViewer()
    {
        // TODO: DialogService 打开存档查看器窗口
        // DialogService.ShowInstance<SaveViewModel>(this, null);
    }

    [RelayCommand]
    private void OpenLocalText()
    {
        var sb = new StringBuilder();
        foreach (var a in LocalizeCore.StoreTranslationList)
            sb.AppendLine(a.Replace("\r\n", "\\r\\n"));
        // TODO: DialogService 显示翻译文本消息框
        // DialogService.ShowMessageBox(this, sb.ToString());
    }

    [RelayCommand]
    private void FirstStartFailed()
    {
        if (LocalizeCore.CurrentCulture == "zh-Hans")
        {
            // TODO: OpenLink 打开 Bilibili 教程链接
            // NativeUtils.OpenLink("https://www.bilibili.com/read/cv26510496/");
        }
        else
        {
            // TODO: OpenLink 打开 Steam 公告链接
            // NativeUtils.OpenLink(
            //     "https://steamcommunity.com/games/1920960/announcements/detail/3681184905256253203"
            // );
        }
    }

    [RelayCommand]
    private void WantOpenVpet()
    {
        if (LocalizeCore.CurrentCulture == "zh-Hant")
        {
            // TODO: OpenLink 打开繁体中文 Wiki 链接
            // NativeUtils.OpenLink("https://wiki.exlb.net/zh-hant/vpet/qa/startup#%E9%99%84%E4%B8%89_%E5%95%9F%E5%8B%95%E5%BE%8C%E5%87%BA%E7%8F%BE%E5%95%8F%E9%A1%8C%E8%A7%A3%E6%B1%BA%E5%B7%A5%E5%85%B7%E7%AA%97%E5%8F%A3_%E5%A6%82%E4%BD%95%E5%88%87%E6%8F%9B%E5%9B%9E%E6%A1%8C%E5%AF%B5");
        }
        else if (LocalizeCore.CurrentCulture.ToLowerInvariant().Contains("zh"))
        {
            // TODO: OpenLink 打开简体中文 Wiki 链接
            // NativeUtils.OpenLink("https://wiki.exlb.net/vpet/qa/startup#%E9%99%84%E4%B8%89_%E5%90%AF%E5%8A%A8%E5%90%8E%E5%87%BA%E7%8E%B0%E9%97%AE%E9%A2%98%E8%A7%A3%E5%86%B3%E5%B7%A5%E5%85%B7%E7%AA%97%E5%8F%A3_%E5%A6%82%E4%BD%95%E5%88%87%E6%8D%A2%E5%9B%9E%E6%A1%8C%E5%AE%A0");
        }
        else
        {
            // TODO: OpenLink 打开英文 Wiki 链接
            // NativeUtils.OpenLink(
            //     "https://wiki.exlb.net/en/vpet/qa/startup#appendix_3the_troubleshooting_tool_opens_at_startup_how_to_switch_back_to_the_desktop_pet"
            // );
        }
    }
    #endregion
}
