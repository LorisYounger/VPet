using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using HKW.HKWMapper;
using HKW.HKWUtils;
using HKW.MVVM;
using VPet_Simulator.Core;

namespace VPet.Solution.Models.SettingEditor;

[MapTarget(typeof(Setting))]
[MapTarget(typeof(InteractiveSettingModel))]
public partial class InteractiveSettingModel : ObservableObjectEx, ISubSettingModel
{
    [MapIgnoreProperty]
    public SubSettingModelType ModelType => SubSettingModelType.Interactive;

    /// <inheritdoc cref="Setting.VoiceVolume"/>
    [ObservableProperty]
    public double VoiceVolume { get; set; }

    /// <inheritdoc cref="Setting.EnableFunction"/>
    [ObservableProperty]
    public bool EnableFunction { get; set; }

    /// <inheritdoc cref="Setting.CalFunState"/>
    [ObservableProperty]
    public IGameSave.ModeType CalFunState { get; set; }

    public static ImmutableArray<IGameSave.ModeType> CalFunStates { get; } =
        EnumInfo<IGameSave.ModeType>.StaticValues;

    /// <inheritdoc cref="Setting.SaveTimes"/>
    [ObservableProperty]
    public int SaveTimes { get; set; }

    /// <inheritdoc cref="Setting.PressLength"/>
    [ObservableProperty]
    public int PressLength { get; set; }

    /// <inheritdoc cref="Setting.InteractionCycle"/>
    [ObservableProperty]
    public int InteractionCycle { get; set; }

    /// <inheritdoc cref="Setting.LogicInterval"/>
    [ObservableProperty]
    public double LogicInterval { get; set; }

    /// <inheritdoc cref="Setting.AllowMove"/>
    [ObservableProperty]
    public bool AllowMove { get; set; }

    /// <inheritdoc cref="Setting.SmartMove"/>
    [ObservableProperty]
    public bool SmartMove { get; set; }

    /// <inheritdoc cref="Setting.SmartMoveInterval"/>
    [ObservableProperty]
    [DefaultValue(1)]
    public int SmartMoveInterval { get; set; }

    /// <inheritdoc cref="Setting.AutoChangeWindow"/>
    [ObservableProperty]
    public bool AutoChangeWindow { get; set; }

    public static int[] SmartMoveIntervals { get; } = [1, 2, 5, 10, 20, 30, 40, 50, 60];

    /// <inheritdoc cref="Setting.PetGraph"/>
    [ObservableProperty]
    public string PetGraph { get; set; } = string.Empty;

    /// <inheritdoc cref="Setting.MusicCatch"/>
    [ObservableProperty]
    [MapProperty(typeof(Setting), typeof(PercentageConverter))]
    public int MusicCatch { get; set; }

    /// <inheritdoc cref="Setting.MusicMax"/>
    [ObservableProperty]
    [MapProperty(typeof(Setting), typeof(PercentageConverter))]
    public int MusicMax { get; set; }

    /// <inheritdoc cref="Setting.AutoBuy"/>
    [ObservableProperty]
    public bool AutoBuy { get; set; }

    /// <inheritdoc cref="Setting.AutoGift"/>
    [ObservableProperty]
    public bool AutoGift { get; set; }

    /// <inheritdoc cref="Setting.MoveAreaDefault"/>
    [ObservableProperty]
    public bool MoveAreaDefault { get; set; }

    /// <inheritdoc cref="Setting.MoveArea"/>
    [ObservableProperty]
    public System.Drawing.Rectangle MoveArea { get; set; }

    public void Load(Setting setting)
    {
        this.MapFrom(setting);
    }

    public void Save(Setting setting)
    {
        this.MapTo(setting);
    }
}

public class SecondToMinuteConverter : IMapConverter<int, double>
{
    public double Convert(object source, int value)
    {
        return value * 60;
    }

    public int ConvertBack(object source, double value)
    {
        if (value == 30d)
            return 1;
        else
            return System.Convert.ToInt32(value / 60);
    }
}

public class PercentageConverter : IMapConverter<int, double>
{
    public double Convert(object source, int value)
    {
        return value / 100d;
    }

    public int ConvertBack(object source, double value)
    {
        return System.Convert.ToInt32(value * 100);
    }
}
