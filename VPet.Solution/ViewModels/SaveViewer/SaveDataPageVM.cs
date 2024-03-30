using VPet.Solution.Models.SaveViewer;

namespace VPet.Solution.ViewModels.SaveViewer;

public class SaveDataPageVM : ObservableClass<SaveDataPageVM>
{
    private SaveModel _save;
    public SaveModel Save
    {
        get => _save;
        set => SetProperty(ref _save, value);
    }

    public SaveDataPageVM()
    {
        SaveWindowVM.Current.PropertyChangedX += Current_PropertyChangedX;
    }

    private void Current_PropertyChangedX(SaveWindowVM sender, PropertyChangedXEventArgs e)
    {
        if (e.PropertyName == nameof(SaveWindowVM.CurrentSave) && sender.CurrentSave is not null)
        {
            Save = sender.CurrentSave;
        }
    }
}
