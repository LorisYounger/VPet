using VPet.Solution.Models.SaveViewer;

namespace VPet.Solution.ViewModels.SaveViewer;

public class SaveStatisticPageVM : ObservableClass<SaveStatisticPageVM>
{
    #region Properties
    #region Save
    private SaveModel _save;
    public SaveModel Save
    {
        get => _save;
        set => SetProperty(ref _save, value);
    }
    #endregion

    #region ShowStatistics
    private IEnumerable<StatisticDataModel> _showStatistics;
    public IEnumerable<StatisticDataModel> ShowStatistics
    {
        get => _showStatistics;
        set => SetProperty(ref _showStatistics, value);
    }
    #endregion

    #region SearchStatistic
    private string _searchStatistic;
    public string SearchStatistic
    {
        get => _searchStatistic;
        set
        {
            SetProperty(ref _searchStatistic, value);
            RefreshShowStatistics(value);
        }
    }
    #endregion
    #endregion


    public SaveStatisticPageVM()
    {
        SaveWindowVM.Current.PropertyChangedX += Current_PropertyChangedX;
    }

    private void Current_PropertyChangedX(SaveWindowVM sender, PropertyChangedXEventArgs e)
    {
        if (e.PropertyName == nameof(SaveWindowVM.CurrentSave) && sender.CurrentSave is not null)
        {
            Save = sender.CurrentSave;
            ShowStatistics = Save.Statistics;
        }
    }

    public void RefreshShowStatistics(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            ShowStatistics = Save.Statistics;
        else
            ShowStatistics = Save.Statistics.Where(
                s => s.Name.Contains(SearchStatistic, StringComparison.OrdinalIgnoreCase)
            );
    }
}
