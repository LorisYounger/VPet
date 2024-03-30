using LinePutScript.Localization.WPF;
using System.Windows;
using VPet.Solution.Models.SettingEditor;

namespace VPet.Solution.ViewModels.SettingEditor;

public class CustomizedSettingPageVM : ObservableClass<CustomizedSettingPageVM>
{
    #region ObservableProperty
    #region CustomizedSetting

    private CustomizedSettingModel _customizedSetting;
    public CustomizedSettingModel CustomizedSetting
    {
        get => _customizedSetting;
        set => SetProperty(ref _customizedSetting, value);
    }
    #endregion

    #region SearchSetting
    private string _searchSetting;
    public string SearchLink
    {
        get => _searchSetting;
        set
        {
            SetProperty(ref _searchSetting, value);
            RefreshShowLinks(value);
        }
    }
    #endregion

    #region ShowLinks
    private IEnumerable<LinkModel> _showLinks;
    public IEnumerable<LinkModel> ShowLinks
    {
        get => _showLinks;
        set => SetProperty(ref _showLinks, value);
    }
    #endregion
    #endregion

    #region Command
    public ObservableCommand AddLinkCommand { get; } = new();
    public ObservableCommand<LinkModel> RemoveLinkCommand { get; } = new();
    public ObservableCommand ClearLinksCommand { get; } = new();
    #endregion
    public CustomizedSettingPageVM()
    {
        SettingWindowVM.Current.PropertyChangedX += Current_PropertyChangedX;
        AddLinkCommand.ExecuteCommand += AddLinkCommand_ExecuteCommand;
        RemoveLinkCommand.ExecuteCommand += RemoveLinkCommand_ExecuteCommand;
        ClearLinksCommand.ExecuteCommand += ClearLinksCommand_ExecuteCommand;
    }

    private void ClearLinksCommand_ExecuteCommand()
    {
        if (
            MessageBox.Show(
                "确定清空吗".Translate(),
                "",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            )
            is not MessageBoxResult.Yes
        )
            return;
        SearchLink = string.Empty;
        CustomizedSetting.Links.Clear();
    }

    private void AddLinkCommand_ExecuteCommand()
    {
        SearchLink = string.Empty;
        CustomizedSetting.Links.Add(new());
    }

    private void RemoveLinkCommand_ExecuteCommand(LinkModel parameter)
    {
        CustomizedSetting.Links.Remove(parameter);
    }

    private void Current_PropertyChangedX(SettingWindowVM sender, PropertyChangedXEventArgs e)
    {
        if (
            e.PropertyName == nameof(SettingWindowVM.CurrentSetting)
            && sender.CurrentSetting is not null
        )
        {
            CustomizedSetting = sender.CurrentSetting.CustomizedSetting;
            SearchLink = string.Empty;
        }
    }

    public void RefreshShowLinks(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            ShowLinks = CustomizedSetting.Links;
        else
            ShowLinks = CustomizedSetting.Links.Where(
                s => s.Name.Contains(SearchLink, StringComparison.OrdinalIgnoreCase)
            );
    }
}
