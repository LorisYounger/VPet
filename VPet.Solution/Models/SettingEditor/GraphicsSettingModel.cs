using System.ComponentModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using HKW.HKWMapper;
using HKW.HKWUtils;
using HKW.HKWUtils.Observable;
using HKW.MVVM;
using LinePutScript.Localization.WPF;

namespace VPet.Solution.Models.SettingEditor;

[MapTo(typeof(Setting), ScrutinyMode = true)]
[MapFrom(typeof(Setting), ScrutinyMode = true)]
[MapFrom(typeof(GraphicsSettingModel), ScrutinyMode = true)]
public partial class GraphicsSettingModel : ObservableObjectEx, ISubSettingModel
{
    public GraphicsSettingModel()
    {
        this.WhenAnyValue(x => x.IsBiggerScreen)
            .Subscribe(value => ZoomLevelMaximum = (value ? 8 : 3));
    }

    [MapIgnoreProperty]
    public SubSettingModelType ModelType => SubSettingModelType.Graphics;

    /// <inheritdoc cref="Setting.ZoomLevel"/>
    [ObservableProperty]
    [DefaultValue(1)]
    public double ZoomLevel { get; set; } = 1;

    [ObservableProperty]
    [DefaultValue(0.5)]
    [MapIgnoreProperty]
    public double ZoomLevelMinimum { get; set; } = 0.5;

    [ObservableProperty]
    [DefaultValue(3)]
    [MapIgnoreProperty]
    public double ZoomLevelMaximum { get; set; } = 3;

    /// <inheritdoc cref="Setting.Resolution"/>
    [ObservableProperty]
    [DefaultValue(1000)]
    public int Resolution { get; set; } = 1000;

    [ObservableProperty]
    [DefaultValue(1920)]
    [MapIgnoreProperty]
    public int ResolutionMaximum { get; set; } = 1920;

    [ObservableProperty]
    [DefaultValue(200)]
    [MapIgnoreProperty]
    public int ResolutionMinimum { get; set; } = 200;

    /// <inheritdoc cref="Setting.IsBiggerScreen"/>
    [ObservableProperty]
    public bool IsBiggerScreen { get; set; }

    /// <inheritdoc cref="Setting.TopMost"/>
    [ObservableProperty]
    public bool TopMost { get; set; }

    /// <inheritdoc cref="Setting.HitThrough"/>
    [ObservableProperty]
    public bool HitThrough { get; set; }

    /// <inheritdoc cref="Setting.Language"/>
    [ObservableProperty]
    public string Language { get; set; }

    public static string[] Languages => LocalizeCore.AvailableCultures;

    /// <inheritdoc cref="Setting.Font"/>
    [ObservableProperty]
    public string Font { get; set; }

    /// <inheritdoc cref="Setting.Theme"/>
    [ObservableProperty]
    public string Theme { get; set; }

    /// <inheritdoc cref="Setting.StartUPBoot"/>
    [ObservableProperty]
    public bool StartUPBoot { get; set; }

    /// <inheritdoc cref="Setting.StartUPBootSteam"/>
    [ObservableProperty]
    public bool StartUPBootSteam { get; set; }

    /// <inheritdoc cref="Setting.StartRecordLast"/>
    [ObservableProperty]
    [DefaultValue(true)]
    public bool StartRecordLast { get; set; } = true;

    /// <inheritdoc cref="Setting.StartRecordPoint"/>
    [ObservableProperty]
    [GraphicsSettingModelMapFromGraphicsSettingModelProperty(
        typeof(ObservablePointToObservablePointConverter)
    )]
    [GraphicsSettingModelMapFromSettingProperty(typeof(ObservablePointToPointConverter))]
    [GraphicsSettingModelMapToSettingProperty(typeof(ObservablePointToPointConverter))]
    public ObservablePoint<double> StartRecordPoint { get; set; } = new();

    /// <inheritdoc cref="Setting.HideFromTaskControl"/>
    [ObservableProperty]
    public bool HideFromTaskControl { get; set; }

    /// <inheritdoc cref="Setting.MessageBarOutside"/>
    [ObservableProperty]
    public bool MessageBarOutside { get; set; }

    /// <inheritdoc cref="Setting.PetHelper"/>
    [ObservableProperty]
    public bool PetHelper { get; set; }

    /// <inheritdoc cref="Setting.PetHelpLeft"/>
    [ObservableProperty]
    public double PetHelpLeft { get; set; }

    /// <inheritdoc cref="Setting.PetHelpTop"/>
    [ObservableProperty]
    public double PetHelpTop { get; set; }

    public void Load(Setting setting)
    {
        this.MapFromSetting(setting);
    }

    public void Save(Setting setting)
    {
        this.MapToSetting(setting);
    }
}

public class ObservablePointToPointConverter : MapConverter<ObservablePoint<double>, Point>
{
    public override Point Convert(object source, ObservablePoint<double> value)
    {
        return new(value.X, value.Y);
    }

    public override ObservablePoint<double> ConvertBack(object source, Point value)
    {
        return new(value.X, value.Y);
    }
}

public class ObservablePointToObservablePointConverter
    : MapConverter<ObservablePoint<double>, ObservablePoint<double>>
{
    public override ObservablePoint<double> Convert(object source, ObservablePoint<double> value)
    {
        return new(value.X, value.Y);
    }

    public override ObservablePoint<double> ConvertBack(
        object source,
        ObservablePoint<double> value
    )
    {
        return new(value.X, value.Y);
    }
}
