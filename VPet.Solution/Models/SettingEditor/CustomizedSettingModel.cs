using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using HKW.HKWMapper;
using HKW.MVVM;
using LinePutScript;

namespace VPet.Solution.Models.SettingEditor;

public partial class CustomizedSettingModel : ObservableObjectEx, ISubSettingModel
{
    public SubSettingModelType ModelType => SubSettingModelType.Customized;
    public const string TargetName = "diy";

    public List<LinkModel> Links { get; } = [];

    public void Load(Setting setting)
    {
        if (setting[TargetName] is ILine line && line.Count > 0)
        {
            foreach (var sub in line)
                Links.Add(new(sub.Name, sub.Info));
        }
        else
        {
            setting.Remove(TargetName);
        }
    }

    public void Save(Setting setting)
    {
        setting.Remove(TargetName);
        foreach (var link in Links)
            setting[TargetName].Add(new Sub(link.Name, link.Link));
    }
}

public partial class LinkModel : ObservableObjectEx
{
    [ObservableProperty]
    /// <summary>
    /// 名称
    /// </summary>
    public string Name { get; set; }

    [ObservableProperty]
    /// <summary>
    /// 链接
    /// </summary>
    public string Link { get; set; }

    public LinkModel() { }

    public LinkModel(string name, string link)
    {
        Name = name;
        Link = link;
    }
}
