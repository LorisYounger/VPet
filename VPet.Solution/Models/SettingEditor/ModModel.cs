using System.ComponentModel;
using System.Windows.Media.Imaging;
using LinePutScript.Localization.WPF;

namespace VPet.Solution.Models.SettingEditor;

public class ModModel : ObservableClass<ModModel>
{
    #region ID
    private string _id = string.Empty;
    public string ID
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }
    #endregion

    #region Name
    private string _name = string.Empty;

    /// <summary>
    /// 名称
    /// </summary>
    [ReflectionProperty(nameof(ModLoader.Name))]
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }
    #endregion

    #region Description
    private string _description = string.Empty;

    /// <summary>
    /// 描述
    /// </summary>
    [ReflectionProperty(nameof(ModLoader.Intro))]
    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }
    #endregion

    #region Author
    private string _author = string.Empty;

    /// <summary>
    /// 作者
    /// </summary>
    [ReflectionProperty(nameof(ModLoader.Author))]
    public string Author
    {
        get => _author;
        set => SetProperty(ref _author, value);
    }
    #endregion

    #region ModVersion
    private int _modVersion;

    /// <summary>
    /// 模组版本
    /// </summary>
    [ReflectionProperty(nameof(ModLoader.Ver))]
    public int ModVersion
    {
        get => _modVersion;
        set => SetProperty(ref _modVersion, value);
    }
    #endregion

    #region GameVersion
    private int _gameVersion;

    /// <summary>
    /// 游戏版本
    /// </summary>
    [ReflectionProperty(nameof(ModLoader.GameVer))]
    public int GameVersion
    {
        get => _gameVersion;
        set => SetProperty(ref _gameVersion, value);
    }
    #endregion

    #region Tags
    private HashSet<string> _tags = null!;

    /// <summary>
    /// 功能
    /// </summary>
    [ReflectionProperty(nameof(ModLoader.Tags))]
    public HashSet<string> Tags
    {
        get => _tags;
        set => SetProperty(ref _tags, value);
    }
    #endregion

    #region Image
    private BitmapImage _image = null!;

    /// <summary>
    /// 图像
    /// </summary>
    [ReflectionProperty(nameof(ModLoader.Image))]
    public BitmapImage Image
    {
        get => _image;
        set => SetProperty(ref _image, value);
    }
    #endregion

    #region ItemId
    private ulong _itemId;

    [ReflectionProperty(nameof(ModLoader.ItemID))]
    public ulong ItemId
    {
        get => _itemId;
        set => SetProperty(ref _itemId, value);
    }
    #endregion


    #region ModPath

    private string _modPath = string.Empty;

    [ReflectionProperty(nameof(ModLoader.ModPath))]
    public string ModPath
    {
        get => _modPath;
        set => SetProperty(ref _modPath, value);
    }
    #endregion

    #region IsEnabled
    private bool? _isEnabled = true;

    /// <summary>
    /// 启用状态
    /// <para>已启用为 <see langword="true"/> 已禁用为 <see langword="false"/> 已失效为 <see langword="null"/></para>
    /// </summary>
    public bool? IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }
    #endregion

    #region IsPass
    private bool _isPass;

    /// <summary>
    /// 是通过检查的代码模组
    /// </summary>
    public bool IsPass
    {
        get => _isPass;
        set => SetProperty(ref _isPass, value);
    }
    #endregion

    #region IsMsg
    private bool _isMsg;

    /// <summary>
    /// 是含有代码的模组
    /// </summary>
    public bool IsMsg
    {
        get => _isMsg;
        set => SetProperty(ref _isMsg, value);
    }
    #endregion

    #region State
    private string _state = string.Empty;
    public string State
    {
        get => _state;
        set => SetProperty(ref _state, value);
    }
    #endregion


    public ModModel()
    {
        IsEnabled = null;
        RefreshState();
    }

    private void ModModel_PropertyChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs e
    )
    {
        if (e.PropertyName == nameof(IsEnabled))
        {
            RefreshState();
        }
    }

    public ModModel(ModLoader loader)
    {
        PropertyChanged += ModModel_PropertyChanged;
        ReflectionUtils.SetValue(loader, this);
        RefreshState();
        ID = Name;
        Name = Name.Translate();
        Description = Description.Translate();
        LocalizeCore.BindingNotify.PropertyChanged += BindingNotify_PropertyChanged;
    }

    private void BindingNotify_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Name = Name.Translate();
        Description = Description.Translate();
    }

    public void RefreshState()
    {
        if (IsEnabled is true)
            State = "已启用".Translate();
        else if (IsEnabled is false)
            State = "已禁用".Translate();
        else
            State = "已损坏".Translate();
    }
}
