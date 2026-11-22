using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HKW.HKWUtils.Collections;
using HKW.MVVM;
using LinePutScript;
using VPet_Simulator.Windows.Interface;
using VPet.Solution.MultiPlatform.Models;

namespace VPet.Solution.MultiPlatform.ViewModels;

public partial class SaveViewModel : ViewModelBase
{
    public SaveViewModel()
    {
        Saves = new([], [], x =>
            x.Name.Contains(SearchSave, StringComparison.CurrentCultureIgnoreCase)
        );
        Statistics = new([], [], x =>
            x.Name.Contains(SearchStatistic, StringComparison.CurrentCultureIgnoreCase)
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

    partial void OnCurrentSaveChanged(SaveModel? oldValue, SaveModel? newValue)
    {
        if (newValue is null)
        {
            Statistics.Clear();
        }
        else
        {
            Statistics.BatchUpdate(l =>
            {
                l.Clear();
                l.AddRange(newValue.Statistics);
            });
        }
        SearchStatistic = string.Empty;
    }

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
    private static void OpenFileInExplorer(SaveModel? parameter)
    {
        if (parameter is null)
            return;
        // TODO: 跨平台从文件管理器打开文件
        // NativeUtils.OpenFileFromExplorer(parameter.FilePath);
    }

    [RelayCommand]
    private static void OpenFile(SaveModel? parameter)
    {
        if (parameter is null)
            return;
        // TODO: OpenLink 打开存档文件
        // NativeUtils.OpenLink(parameter.FilePath);
    }
}
