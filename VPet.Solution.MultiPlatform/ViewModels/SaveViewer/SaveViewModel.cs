using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HanumanInstitute.MvvmDialogs.FileSystem;
using HKW.HKWUtils.Collections;
using HKW.MVVM;
using LinePutScript;
using VPet_Simulator.Windows.Interface;
using VPet.Solution.MultiPlatform.Models;

namespace VPet.Solution.MultiPlatform.ViewModels;

public partial class SaveViewModel : CloseableViewModel
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
            .Subscribe(_ => Saves?.Refresh())
            .DisposeWith(Disposables);

        this.WhenAnyValue(x => x.SearchStatistic)
            .Throttle(TimeSpan.FromSeconds(0.5), ObservableSchedulers.ThreadPool)
            .DistinctUntilChanged()
            .ObserveOn(ObservableSchedulers.Current)
            .Subscribe(_ => Statistics?.Refresh())
            .DisposeWith(Disposables);

        _ = InitializeSavesAsync();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var save in Saves)
                save.StorageFile?.Dispose();
        }
        base.Dispose(disposing);
    }

    private async Task InitializeSavesAsync()
    {
        var loaded = new List<SaveModel>();
        await LoadSavesAsync(loaded);
        Saves.BatchUpdate(l =>
        {
            l.Clear();
            l.AddRange(loaded);
        });
        CurrentSave = Saves.FirstOrDefault();
    }

    private static async Task LoadSavesAsync(List<SaveModel> saves)
    {
        var baseFolder = VPetSolution.BaseFolder;
        if (baseFolder is null)
            return;

        using var saveFolder = await baseFolder.TryGetFolderAsync("Saves");
        if (saveFolder is null)
            return;

        await foreach (var item in saveFolder.GetItemsAsync())
        {
            if (
                item is not IDialogStorageFile file
                || file.Name.EndsWith(".lps", StringComparison.OrdinalIgnoreCase) is false
            )
            {
                item.Dispose();
                continue;
            }

            try
            {
                var text = await file.ReadAllTextAsync();
                var lps = new LPS(text);
                var save = new GameSave_v2(lps);
                var props = await file.GetBasicPropertiesAsync();
                var dateSaved = props.DateModified?.LocalDateTime ?? DateTime.MinValue;
                var saveModel = new SaveModel(file.GetDisplayPath(), save, dateSaved)
                {
                    StorageFile = file,
                };
                saves.Add(saveModel);
            }
            catch
            {
                file.Dispose();
                throw;
            }
        }
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
    private static async Task OpenFile(SaveModel? parameter)
    {
        if (parameter is null)
            return;
        if (parameter.StorageFile is not null)
            await NativeUtils.LaunchAsync(parameter.StorageFile);
        else
            await NativeUtils.LaunchAsync(parameter.FilePath);
    }
}
