using VPet.Solution.Models.SettingEditor;

namespace VPet.Solution.ViewModels.SettingEditor;

public class DiagnosticSettingPageVM : ObservableClass<DiagnosticSettingPageVM>
{
    private DiagnosticSettingModel _diagnosticSetting;
    public DiagnosticSettingModel DiagnosticSetting
    {
        get => _diagnosticSetting;
        set => SetProperty(ref _diagnosticSetting, value);
    }

    public DiagnosticSettingPageVM()
    {
        SettingWindowVM.Current.PropertyChangedX += Current_PropertyChangedX;
    }

    private void Current_PropertyChangedX(SettingWindowVM sender, PropertyChangedXEventArgs e)
    {
        if (
            e.PropertyName == nameof(SettingWindowVM.CurrentSetting)
            && sender.CurrentSetting is not null
        )
        {
            DiagnosticSetting = sender.CurrentSetting.DiagnosticSetting;
        }
    }
}
