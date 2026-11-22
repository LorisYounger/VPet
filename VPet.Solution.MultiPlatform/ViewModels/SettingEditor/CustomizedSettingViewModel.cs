using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HKW.HKWUtils.Collections;
using HKW.HKWUtils.Observable;
using HKW.MVVM;
using VPet.Solution.MultiPlatform.Models;

namespace VPet.Solution.MultiPlatform.ViewModels;

public partial class CustomizedSettingViewModel : CloseableViewModel, ISubSettingViewModel
{
    public CustomizedSettingViewModel()
    {
        this.WhenAnyValue(x => x.SearchLink)
            .Throttle(TimeSpan.FromSeconds(0.5), ObservableSchedulers.ThreadPool)
            .DistinctUntilChanged()
            .ObserveOn(ObservableSchedulers.Current)
            .Subscribe(_ => Links?.Refresh())
            .DisposeWith(Disposables);
    }

    [ObservableProperty]
    public SettingModel Setting { get; set; } = null!;

    partial void OnSettingChanged(SettingModel oldValue, SettingModel newValue)
    {
        if (newValue is not null)
        {
            Links = new(newValue.CustomizedSetting.Links, [], x =>
                x.Name.Contains(SearchLink, StringComparison.CurrentCultureIgnoreCase)
            );
        }
        SearchLink = string.Empty;
    }

    public CustomizedSettingModel CustomizedSetting => Setting.CustomizedSetting;

    [ObservableProperty]
    public string SearchLink { get; set; } = string.Empty;

    [ObservableProperty]
    public FilteredListWrapper<
        LinkModel,
        List<LinkModel>,
        ObservableList<LinkModel>
    > Links { get; set; } = null!;

    [RelayCommand]
    private void ClearLinks()
    {
        // TODO: DialogService 确认清空全部自定义链接
        // var result = DialogService.ShowMessageBox(
        //     this,
        //     "确定清空吗".Translate(),
        //     "",
        //     MessageBoxButton.YesNo,
        //     MessageBoxImage.Warning
        // );
        // if (result is not true)
        //     return;

        SearchLink = string.Empty;
        Links.Clear();
        Setting.IsChanged = true;
    }

    [RelayCommand]
    private void AddLink()
    {
        SearchLink = string.Empty;
        Links.Add(new());
        Setting.IsChanged = true;
    }

    [RelayCommand]
    private void RemoveLink(LinkModel? parameter)
    {
        if (parameter is null)
            return;
        Links.Remove(parameter);
    }

    [RelayCommand]
    private void SendKey()
    {
        if (Setting.GraphicsSetting.Language.StartsWith("zh"))
        {
            // TODO: OpenLink 打开中文 SendKeys 文档
            // NativeUtils.OpenLink("https://www.exlb.net/SendKeys");
        }
        else if (Setting.GraphicsSetting.Language == "null")
        {
            // TODO: OpenLink 打开英文 SendKeys 文档
            // NativeUtils.OpenLink(
            //     "https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.sendkeys?view=windowsdesktop-7.0#remarks"
            // );
        }
        else
        {
            // TODO: OpenLink 打开对应语言 SendKeys 文档
            // NativeUtils.OpenLink(
            //     $"https://learn.microsoft.com/{Setting.GraphicsSetting.Language}/dotnet/api/system.windows.forms.sendkeys?view=windowsdesktop-7.0#remarks"
            // );
        }
    }
}
