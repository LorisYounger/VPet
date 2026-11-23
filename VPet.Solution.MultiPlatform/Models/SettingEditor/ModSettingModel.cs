using System.Collections.ObjectModel;
using HanumanInstitute.MvvmDialogs.FileSystem;
using HKW.MVVM;
using LinePutScript;
using LinePutScript.Localization;

namespace VPet.Solution.MultiPlatform.Models;

public partial class ModSettingModel : ObservableObjectEx, ISubSettingModel
{
    private static IDialogStorageFolder? _loadedBaseFolder;

    public SubSettingModelType ModelType => SubSettingModelType.Mod;
    public const string OnModLineName = "onmod";
    public const string PassModLineName = "passmod";
    public const string MsgModLineName = "msgmod";
    public const string WorkShopLineName = "workshop";
    public static Dictionary<string, ModLoader> LocalMods { get; private set; } =
        new(StringComparer.OrdinalIgnoreCase);

    public static List<ModLoader> LocalModList { get; } = [];

    public List<ModModel> Mods { get; } = [];

    public static ObservableCollection<string> Fonts { get; } = [];

    public static ObservableCollection<string> Themes { get; } = [];

    public static ObservableCollection<string> PetGraphs { get; } = [];

    public static List<string> StaticErrors { get; } = [];

    public List<string> Errors { get; } = [];

    /// <summary>
    /// 确保当前根目录下的本地模组已加载
    /// </summary>
    public static async Task EnsureLocalModsLoadedAsync()
    {
        var baseFolder = VPetSolution.BaseFolder;
        if (ReferenceEquals(_loadedBaseFolder, baseFolder))
            return;

        _loadedBaseFolder = baseFolder;
        LocalMods = new(StringComparer.OrdinalIgnoreCase);
        LocalModList.Clear();
        Fonts.Clear();
        Themes.Clear();
        PetGraphs.Clear();
        StaticErrors.Clear();

        if (baseFolder is null)
            return;

        using var modFolder = await baseFolder.TryGetFolderAsync("mod");
        if (modFolder is null)
            return;

        await foreach (var item in modFolder.GetItemsAsync())
        {
            using (item)
            {
                if (item is not IDialogStorageFolder dir)
                    continue;

                try
                {
                    var loader = await ModLoader.LoadAsync(dir);
                    LocalModList.Add(loader);
                    if (string.IsNullOrEmpty(loader.Name) is false)
                        LocalMods.TryAdd(loader.Name, loader);
                    if (loader.IsSuccesses is false)
                        continue;

                    foreach (var font in loader.Fonts)
                    {
                        var translated = font.Translate();
                        if (Fonts.Contains(translated) is false)
                            Fonts.Add(translated);
                    }
                    foreach (var theme in loader.Themes)
                    {
                        var translated = theme.Translate();
                        if (Themes.Contains(translated) is false)
                            Themes.Add(translated);
                    }
                    foreach (var petGraph in loader.PetGraphs)
                    {
                        var translated = petGraph.Translate();
                        if (PetGraphs.Contains(translated) is false)
                            PetGraphs.Add(translated);
                    }
                }
                catch (Exception ex)
                {
                    StaticErrors.Add(
                        "路径:\"{0}\" 异常:\"{1}\"".Translate(dir.GetDisplayPath(), ex.Message)
                    );
                }
            }
        }
    }

    public void Load(Setting setting)
    {
        Mods.Clear();
        Errors.Clear();
        Errors.AddRange(StaticErrors);

        foreach (var loader in LocalModList)
        {
            if (loader.IsSuccesses)
            {
                var modModel = new ModModel(loader);
                modModel.IsMsg = setting[MsgModLineName].GetBool(modModel.ID);
                modModel.IsPass = setting[PassModLineName].Contains(modModel.ID.ToLowerInvariant());
                if (modModel.ID.Equals("Core", StringComparison.OrdinalIgnoreCase))
                {
                    modModel.IsEnabled = true;
                }
                else
                {
                    modModel.IsEnabled = setting[OnModLineName]
                        .Contains(modModel.ID.ToLowerInvariant());
                }

                Mods.Add(modModel);
            }
            else
            {
                Mods.Add(new() { Name = loader.Name, ModPath = loader.ModPath });
            }
        }

        foreach (var modPath in setting[WorkShopLineName])
        {
            if (OperatingSystem.IsBrowser())
            {
                Mods.Add(new() { Name = Path.GetFileName(modPath.Name), ModPath = modPath.Name });
                continue;
            }

            var loader = new ModLoader(modPath.Name);
            if (loader.IsSuccesses)
            {
                var modModel = new ModModel(loader);
                modModel.IsMsg = setting[MsgModLineName].GetBool(modModel.ID);
                modModel.IsPass = setting[PassModLineName].Contains(modModel.ID.ToLowerInvariant());
                modModel.IsEnabled = setting[OnModLineName]
                    .Contains(modModel.ID.ToLowerInvariant());
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
        setting.Remove(OnModLineName);
        setting.Remove(PassModLineName);
        setting.Remove(MsgModLineName);
        foreach (var mod in Mods)
        {
            if (mod.IsEnabled is not true)
                continue;
            setting[OnModLineName].Add(new Sub(mod.ID.ToLowerInvariant()));
            setting[MsgModLineName].Add(new Sub(mod.ID, "True"));
            if (mod.IsPass)
                setting[PassModLineName].Add(new Sub(mod.ID.ToLowerInvariant()));
        }
    }
}
