using System.Collections.ObjectModel;

namespace VPet.Solution.Models.SettingEditor;

public class CustomizedSettingModel : ObservableClass<CustomizedSettingModel>
{
    public const string TargetName = "diy";

    #region Links
    private ObservableCollection<LinkModel> _links = new();
    public ObservableCollection<LinkModel> Links
    {
        get => _links;
        set => SetProperty(ref _links, value);
    }
    #endregion
}

public class LinkModel : ObservableClass<LinkModel>
{
    #region Name
    private string _name;

    /// <summary>
    /// 名称
    /// </summary>
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }
    #endregion


    #region Link
    private string _link;

    /// <summary>
    /// 链接
    /// </summary>
    public string Link
    {
        get => _link;
        set => SetProperty(ref _link, value);
    }
    #endregion

    public LinkModel() { }

    public LinkModel(string name, string link)
    {
        Name = name;
        Link = link;
    }
}
