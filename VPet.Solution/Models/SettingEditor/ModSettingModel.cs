using System.Collections.ObjectModel;
using System.IO;
using LinePutScript;
using LinePutScript.Localization.WPF;
using ReactiveUI;

namespace VPet.Solution.Models.SettingEditor;

public partial class ModSettingModel : ReactiveObject, ISubSettingModel
{
    public SubSettingModelType ModelType => SubSettingModelType.Mod;
    public const string ModLineName = "onmod";
    public const string PassModLineName = "passmod";
    public const string MsgModLineName = "msgmod";
    public const string WorkShopLineName = "workshop";
    public static readonly string ModDirectory = Path.Combine(Environment.CurrentDirectory, "mod");
    public static Dictionary<string, ModLoader> LocalMods { get; private set; } = null!;

    public List<ModModel> Mods { get; } = [];
    public List<string> Errors { get; } = [];

    public ModSettingModel()
    {
        LocalMods ??= GetLocalMods();
    }

    private Dictionary<string, ModLoader> GetLocalMods()
    {
        var dic = new Dictionary<string, ModLoader>(StringComparer.OrdinalIgnoreCase);
        if (Directory.Exists(ModDirectory) is false)
            return dic;
        foreach (var dir in Directory.EnumerateDirectories(ModDirectory))
        {
            try
            {
                var loader = new ModLoader(dir);
                dic.TryAdd(loader.Name, loader);
            }
            catch (Exception ex)
            {
                Errors.Add("路径:\"{0}\" 异常:\"{1}\"".Translate(dir, ex.Message));
            }
        }
        return dic;
    }

    public void Load(Setting setting)
    {
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
                        ModPath = "未知, 可能是{0}".Translate(Path.Combine(ModDirectory, modID)),
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
                modModel.IsPass = setting[PassModLineName].Contains(modModel.ID.ToLowerInvariant());
                Mods.Add(modModel);
            }
            else
            {
                Mods.Add(new() { Name = loader.Name, ModPath = loader.ModPath });
            }
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
            setting[ModLineName].Add(new Sub(mod.ID.ToLowerInvariant()));
            setting[MsgModLineName].Add(new Sub(mod.ID, "True"));
            if (mod.IsPass)
                setting[PassModLineName].Add(new Sub(mod.ID.ToLowerInvariant()));
        }
    }
}
