using System.Collections.ObjectModel;
using System.Windows;
using LinePutScript;
using LinePutScript.Localization.WPF;

namespace VPet.Solution.Models.SettingEditor;

public class ModSettingModel : ObservableClass<ModSettingModel>
{
    public const string ModLineName = "onmod";
    public const string PassModLineName = "passmod";
    public const string MsgModLineName = "msgmod";
    public const string WorkShopLineName = "workshop";
    public static string ModDirectory = Path.Combine(Environment.CurrentDirectory, "mod");
    public static Dictionary<string, ModLoader> LocalMods { get; private set; } = null!;

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
            var modID = item.Name;
            if (LocalMods.TryGetValue(modID, out var loader) && loader.IsSuccesses)
            {
                var modModel = new ModModel(loader);
                modModel.IsMsg = setting[MsgModLineName].GetBool(modModel.ID);
                modModel.IsPass = setting[PassModLineName].Contains(modID);
                Mods.Add(modModel);
            }
            else
            {
                Mods.Add(
                    new()
                    {
                        Name = modID,
                        ModPath = "未知, 可能是{0}".Translate(Path.Combine(ModDirectory, modID))
                    }
                );
            }
        }
        foreach (var modPath in setting[WorkShopLineName])
        {
            var loader = new ModLoader(modPath.Name);
            if (loader.IsSuccesses)
            {
                var modModel = new ModModel(loader);
                modModel.IsMsg = setting[MsgModLineName].GetBool(modModel.ID);
                modModel.IsPass = setting[PassModLineName].Contains(modModel.ID.ToLower());
                Mods.Add(modModel);
            }
            else
            {
                Mods.Add(new() { Name = loader.Name, ModPath = loader.ModPath });
            }
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
        foreach (var mod in Mods)
        {
            if (mod.IsEnabled is false)
                continue;
            setting[ModLineName].Add(new Sub(mod.ID.ToLower()));
            setting[MsgModLineName].Add(new Sub(mod.ID, "True"));
            if (mod.IsPass)
                setting[PassModLineName].Add(new Sub(mod.ID.ToLower()));
        }
    }
    #endregion
}
