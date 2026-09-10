using System.ComponentModel;
using System.Windows;
using HKW.HKWMapper;
using HKW.HKWReactiveUI;
using HKW.HKWUtils;
using HKW.HKWUtils.Observable;
using LinePutScript.Localization.WPF;
using ReactiveUI;
using ReactiveUI.Primitives;

namespace VPet.Solution.Models.SettingEditor;

[MapTo(typeof(Setting), ScrutinyMode = true)]
[MapFrom(typeof(Setting), ScrutinyMode = true)]
[MapFrom(typeof(GraphicsSettingModel), ScrutinyMode = true)]
public partial class GraphicsSettingModel : ReactiveObject, ISubSettingModel
{
    public GraphicsSettingModel()
    {
        this.WhenAnyValue(x => x.IsBiggerScreen)
            .Subscribe(value => ZoomLevelMaximum = (value ? 8 : 3));
    }

    [MapIgnoreProperty]
    public SubSettingModelType ModelType => SubSettingModelType.Graphics;

    /// <inheritdoc cref="Setting.ZoomLevel"/>
    [ReactiveProperty]
    [DefaultValue(1)]
    public double ZoomLevel { get; set; } = 1;

    [ReactiveProperty]
    [DefaultValue(0.5)]
    [MapIgnoreProperty]
    public double ZoomLevelMinimum { get; set; } = 0.5;

    [ReactiveProperty]
    [DefaultValue(3)]
    [MapIgnoreProperty]
    public double ZoomLevelMaximum { get; set; } = 3;

    /// <inheritdoc cref="Setting.Resolution"/>
    [ReactiveProperty]
    [DefaultValue(1000)]
    public int Resolution { get; set; } = 1000;

    [ReactiveProperty]
    [DefaultValue(1920)]
    [MapIgnoreProperty]
    public int ResolutionMaximum { get; set; } = 1920;

    [ReactiveProperty]
    [DefaultValue(200)]
    [MapIgnoreProperty]
    public int ResolutionMinimum { get; set; } = 200;

    /// <inheritdoc cref="Setting.IsBiggerScreen"/>
    [ReactiveProperty]
    public bool IsBiggerScreen { get; set; }

    /// <inheritdoc cref="Setting.TopMost"/>
    [ReactiveProperty]
    public bool TopMost { get; set; }

    /// <inheritdoc cref="Setting.HitThrough"/>
    [ReactiveProperty]
    public bool HitThrough { get; set; }

    /// <inheritdoc cref="Setting.Language"/>
    [ReactiveProperty]
    public string Language { get; set; }

    public static string[] Languages => LocalizeCore.AvailableCultures;

    /// <inheritdoc cref="Setting.Font"/>
    [ReactiveProperty]
    public string Font { get; set; }

    /// <inheritdoc cref="Setting.Theme"/>
    [ReactiveProperty]
    public string Theme { get; set; }

    /// <inheritdoc cref="Setting.StartUPBoot"/>
    [ReactiveProperty]
    public bool StartUPBoot { get; set; }

    /// <inheritdoc cref="Setting.StartUPBootSteam"/>
    [ReactiveProperty]
    public bool StartUPBootSteam { get; set; }

    /// <inheritdoc cref="Setting.StartRecordLast"/>
    [ReactiveProperty]
    [DefaultValue(true)]
    public bool StartRecordLast { get; set; } = true;

    /// <inheritdoc cref="Setting.StartRecordPoint"/>
    [ReactiveProperty]
    [GraphicsSettingModelMapFromGraphicsSettingModelProperty(
        typeof(ObservablePointToObservablePointConverter)
    )]
    [GraphicsSettingModelMapFromSettingProperty(typeof(ObservablePointToPointConverter))]
    [GraphicsSettingModelMapToSettingProperty(typeof(ObservablePointToPointConverter))]
    public ObservablePoint<double> StartRecordPoint { get; set; } = new();

    /// <inheritdoc cref="Setting.HideFromTaskControl"/>
    [ReactiveProperty]
    public bool HideFromTaskControl { get; set; }

    /// <inheritdoc cref="Setting.MessageBarOutside"/>
    [ReactiveProperty]
    public bool MessageBarOutside { get; set; }

    /// <inheritdoc cref="Setting.PetHelper"/>
    [ReactiveProperty]
    public bool PetHelper { get; set; }

    /// <inheritdoc cref="Setting.PetHelpLeft"/>
    [ReactiveProperty]
    public double PetHelpLeft { get; set; }

    /// <inheritdoc cref="Setting.PetHelpTop"/>
    [ReactiveProperty]
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
