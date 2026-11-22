using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using HKW.Mapper;
using HKW.MVVM;
using HKW.MVVM.SourceGenerator;
using LinePutScript.Localization;

namespace VPet.Solution.MultiPlatform.Models;

[MapTarget(typeof(ModLoader))]
public partial class ModModel : ObservableObjectEx
{
    [ObservableProperty]
    [MapIgnoreProperty]
    public string ID { get; set; } = string.Empty;

    /// <summary>
    /// 名称
    /// </summary>
    [ObservableProperty]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 描述
    /// </summary>
    [ObservableProperty]
    [MapProperty(typeof(ModLoader), nameof(ModLoader.Intro))]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 作者
    /// </summary>
    [ObservableProperty]
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// 模组版本
    /// </summary>
    [ObservableProperty]
    [MapProperty(typeof(ModLoader), nameof(ModLoader.Ver))]
    public int ModVersion { get; set; }

    /// <summary>
    /// 游戏版本
    /// </summary>
    [ObservableProperty]
    [MapProperty(typeof(ModLoader), nameof(ModLoader.GameVer))]
    public int GameVersion { get; set; }

    /// <summary>
    /// 功能标签
    /// </summary>
    [ObservableProperty]
    public HashSet<string> Tags { get; set; } = [];

    /// <summary>
    /// 图像
    /// </summary>
    [ObservableProperty]
    [MapProperty(typeof(ModLoader), MapType = MapPropertyType.Reference)]
    public Bitmap? Image { get; set; }

    [ObservableProperty]
    public ulong ItemID { get; set; }

    [ObservableProperty]
    public string ModPath { get; set; } = string.Empty;

    /// <summary>
    /// 启用状态: 已启用为 true, 已禁用为 false, 已失效为 null
    /// </summary>
    [ObservableProperty]
    [MapIgnoreProperty]
    public bool? IsEnabled { get; set; } = true;

    /// <summary>
    /// 是通过检查的代码模组
    /// </summary>
    [ObservableProperty]
    [MapIgnoreProperty]
    public bool IsPass { get; set; }

    /// <summary>
    /// 是含有代码的模组
    /// </summary>
    [ObservableProperty]
    [MapIgnoreProperty]
    public bool IsMsg { get; set; }

    public ModModel()
    {
        IsEnabled = null;
    }

    public ModModel(ModLoader loader)
    {
        this.MapFrom(loader);
        ID = loader.Name;
        Name = loader.Name.Translate();
        Description = loader.Intro.Translate();
    }

    /// <summary>
    /// 状态文本
    /// </summary>
    [NotifyPropertyChangeFrom(nameof(IsEnabled))]
    [MapIgnoreProperty]
    public string State
    {
        get
        {
            if (IsEnabled is true)
                return "已启用".Translate();
            else if (IsEnabled is false)
                return "已禁用".Translate();
            else
                return "已损坏".Translate();
        }
    }
}
