using HKW.HKWUtils.Observable;
using LinePutScript;
using LinePutScript.Localization.WPF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VPet.Solution.Views.SettingEditor;
using VPet_Simulator.Windows.Interface;

namespace VPet.Solution.Models.SettingEditor;

public class ModSettingModel : ObservableClass<ModSettingModel>
{
    public const string ModLineName = "onmod";
    public const string PassModLineName = "passmod";
    public const string MsgModLineName = "msgmod";
    public const string WorkShopLineName = "workshop";
    public static string ModDirectory = Path.Combine(Environment.CurrentDirectory, "mod");
    public static Dictionary<string, ModLoader> LocalMods { get; private set; } = null;

    #region Mods
    private ObservableCollection<ModModel> _mods = new();
    public ObservableCollection<ModModel> Mods
    {
        get => _mods;
        set => SetProperty(ref _mods, value);
    }

    public ModSettingModel(Setting setting)
    {
        LocalMods ??= GetLocalMods();
        foreach (var item in setting[ModLineName])
        {
            var modName = item.Name;
            if (LocalMods.TryGetValue(modName, out var loader) && loader.IsSuccesses)
            {
                var modModel = new ModModel(loader);
                modModel.IsPass = setting[PassModLineName].Contains(modName);
                modModel.IsMsg = setting[MsgModLineName].Contains(modModel.Name);
                Mods.Add(modModel);
            }
            else
            {
                Mods.Add(
                    new()
                    {
                        Name = modName,
                        ModPath = "未知, 可能是{0}".Translate(Path.Combine(ModDirectory, modName))
                    }
                );
            }
        }
        foreach (var modPath in setting[WorkShopLineName])
        {
            var loader = new ModLoader(modPath.Name);
            if (loader.IsSuccesses is false)
            {
                Mods.Add(new() { Name = loader.Name, ModPath = loader.ModPath });
                return;
            }
            var modModel = new ModModel(loader);
            modModel.IsPass = setting[PassModLineName].Contains(modModel.Name);
            modModel.IsMsg = setting[MsgModLineName].Contains(modModel.Name);
            Mods.Add(modModel);
        }
    }

    private static Dictionary<string, ModLoader> GetLocalMods()
    {
        var dic = new Dictionary<string, ModLoader>(StringComparer.OrdinalIgnoreCase);
        foreach (var dir in Directory.EnumerateDirectories(ModDirectory))
        {
            try
            {
                var loader = new ModLoader(dir);
                dic.TryAdd(loader.Name, loader);
            }
            catch (Exception ex)
            {
                MessageBox.Show("模组载入错误\n路径:{0}\n异常:{1}".Translate(dir, ex));
            }
        }
        return dic;
    }

    public void Close()
    {
        foreach (var modLoader in LocalMods)
        {
            modLoader.Value.Image.CloseStream();
        }
    }

    public void Save(Setting setting)
    {
        setting.Remove(ModLineName);
        setting.Remove(PassModLineName);
        setting.Remove(MsgModLineName);
        if (Mods.Any() is false)
            return;
        foreach (var mod in Mods)
        {
            setting[ModLineName].Add(new Sub(mod.Name.ToLower()));
            setting[MsgModLineName].Add(new Sub(mod.Name, "True"));
            if (mod.IsPass)
                setting[PassModLineName].Add(new Sub(mod.Name.ToLower()));
        }
    }
    #endregion
}

public class ModModel : ObservableClass<ModModel>
{
    #region Name
    private string _name;

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
    private string _description;

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
    private string _author;

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
    private HashSet<string> _tags;

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
    private BitmapImage _image;

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

    private string _modPath;

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
    /// 已启用
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
    private string _state;
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
        object sender,
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
        Name = Name.Translate();
        Description = Description.Translate();
        LocalizeCore.BindingNotify.PropertyChanged += BindingNotify_PropertyChanged;
    }

    private void BindingNotify_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        Name = Name.Translate();
        Description = Description.Translate();
    }

    public void RefreshState()
    {
        if (IsEnabled is true)
            State = "已启用".Translate();
        else if (IsEnabled is false)
            State = "已关闭".Translate();
        else
            State = "已损坏".Translate();
    }
}
