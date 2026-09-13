using System.ComponentModel;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using HKW.HKWMapper;
using HKW.MVVM;
using HKW.MVVM.SourceGenerator;
using LinePutScript.Localization.WPF;

namespace VPet.Solution.Models.SettingEditor;

[MapTo(typeof(ModLoader), ScrutinyMode = true)]
[MapFrom(typeof(ModLoader), ScrutinyMode = true)]
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
    [ModModelMapToModLoaderProperty(nameof(ModLoader.Intro))]
    [ModModelMapFromModLoaderProperty(nameof(ModLoader.Intro))]
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
    [ModModelMapToModLoaderProperty(nameof(ModLoader.Ver))]
    [ModModelMapFromModLoaderProperty(nameof(ModLoader.Ver))]
    public int ModVersion { get; set; }

    /// <summary>
    /// 游戏版本
    /// </summary>
    [ObservableProperty]
    [ModModelMapToModLoaderProperty(nameof(ModLoader.GameVer))]
    [ModModelMapFromModLoaderProperty(nameof(ModLoader.GameVer))]
    public int GameVersion { get; set; }

    /// <summary>
    /// 功能
    /// </summary>
    [ObservableProperty]
    public HashSet<string> Tags { get; set; } = null!;

    /// <summary>
    /// 图像
    /// </summary>
    [ObservableProperty]
    public BitmapImage Image { get; set; } = null!;

    [ObservableProperty]
    public ulong ItemID { get; set; }

    [ObservableProperty]
    public string ModPath { get; set; } = string.Empty;

    /// <summary>
    /// 启用状态
    /// <para>
    /// 已启用为 <see langword="true"/> 已禁用为 <see langword="false"/> 已失效为 <see langword="null"/>
    /// </para>
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
        ID = Name;
        Name = Name.Translate();
        Description = Description.Translate();
        this.MapFromModLoader(loader);
    }

    /// <summary>
    /// 是含有代码的模组
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
