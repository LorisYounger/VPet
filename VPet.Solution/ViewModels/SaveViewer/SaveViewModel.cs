using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HanumanInstitute.MvvmDialogs;
using HKW.HKWUtils.Collections;
using HKW.MVVM;
using LinePutScript;
using VPet_Simulator.Windows.Interface;
using VPet.Solution.Models.SaveViewer;

namespace VPet.Solution.ViewModels.SaveViewer;

public partial class SaveViewModel : ViewModelBase
{
    public SaveViewModel(IDialogService dialogService)
        : base(dialogService)
    {
        Saves = new(
            [],
            [],
            x => x.Name.Contains(SearchSave, StringComparison.CurrentCultureIgnoreCase)
        );
        Statistics = new(
            [],
            [],
            x => x.Name.Contains(SearchStatistic, StringComparison.CurrentCultureIgnoreCase)
        );
        this.WhenAnyValue(x => x.SearchSave)
            .Skip(1)
            .Throttle(TimeSpan.FromSeconds(0.5), ObservableSchedulers.ThreadPool)
            .DistinctUntilChanged()
            .ObserveOn(ObservableSchedulers.Current)
            .Subscribe(_ => Saves?.Refresh());

        this.WhenAnyValue(x => x.SearchStatistic)
            .Throttle(TimeSpan.FromSeconds(0.5), ObservableSchedulers.ThreadPool)
            .DistinctUntilChanged()
            .ObserveOn(ObservableSchedulers.Current)
            .Subscribe(_ => Statistics?.Refresh());

        Saves.BatchUpdate(static l =>
        {
            var saveDirectory = Path.Combine(Environment.CurrentDirectory, "Saves");
            if (Directory.Exists(saveDirectory) is false)
                return;
            foreach (
                var file in Directory.EnumerateFiles(saveDirectory).Where(s => s.EndsWith(".lps"))
            )
            {
                var lps = new LPS(File.ReadAllText(file));
                var save = new GameSave_v2(lps);
                var saveModel = new SaveModel(file, save);
                l.Add(saveModel);
            }
        });
        CurrentSave = Saves.FirstOrDefault();
    }

    [ObservableProperty]
    public SaveModel? CurrentSave { get; set; }

    public FilteredListWrapper<
        SaveModel,
        List<SaveModel>,
        ObservableCollection<SaveModel>
    > Saves { get; }

    public FilteredListWrapper<
        StatisticModel,
        List<StatisticModel>,
        ObservableCollection<StatisticModel>
    > Statistics { get; }

    [ObservableProperty]
    public string SearchSave { get; set; } = string.Empty;

    [ObservableProperty]
    public string SearchStatistic { get; set; } = string.Empty;

    [RelayCommand]
    private static void OpenFileInExplorer(SaveModel parameter)
    {
        NativeUtils.OpenFileFromExplorer(parameter.FilePath);
    }

    [RelayCommand]
    private static void OpenFile(SaveModel parameter)
    {
        NativeUtils.OpenLink(parameter.FilePath);
    }

    partial class SaveViewModelObservableObjectHelper
    {
        partial void OnCurrentSaveChanged(SaveModel oldValue, SaveModel newValue)
        {
            if (newValue is null)
            {
                _source.Statistics.Clear();
            }
            else
            {
                _source.Statistics.BatchUpdate(l =>
                {
                    l.Clear();
                    l.AddRange(newValue.Statistics);
                });
            }
            _source.SearchStatistic = string.Empty;
        }
    }
}
