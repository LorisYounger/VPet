using HKW.HKWUtils.Observable;
using LinePutScript;
using LinePutScript.Localization.WPF;
using Panuon.WPF.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using VPet.Solution.Models;
using VPet.Solution.Models.SaveViewer;
using VPet.Solution.Models.SettingEditor;
using VPet.Solution.Views.SettingEditor;
using VPet_Simulator.Windows.Interface;

namespace VPet.Solution.ViewModels.SaveViewer;

public class SaveWindowVM : ObservableClass<SaveWindowVM>
{
    public static SaveWindowVM Current { get; private set; }

    #region Properties
    private SaveModel _currentSave;
    public SaveModel CurrentSave
    {
        get => _currentSave;
        set => SetProperty(ref _currentSave, value);
    }

    private readonly ObservableCollection<SaveModel> _saves = new();

    private IEnumerable<SaveModel> _showSaves;
    public IEnumerable<SaveModel> ShowSaves
    {
        get => _showSaves;
        set => SetProperty(ref _showSaves, value);
    }

    private string _searchSave;
    public string SearchSave
    {
        get => _searchSave;
        set => SetProperty(ref _searchSave, value);
    }

    #endregion

    #region Command
    /// <summary>
    /// 打开文件
    /// </summary>
    public ObservableCommand<SaveModel> OpenFileCommand { get; } = new();

    /// <summary>
    /// 从资源管理器打开
    /// </summary>
    public ObservableCommand<SaveModel> OpenFileInExplorerCommand { get; } = new();
    #endregion
    public SaveWindowVM()
    {
        Current = this;
        ShowSaves = _saves;
        LoadSaves();

        PropertyChanged += SaveWindowVM_PropertyChanged;
        OpenFileCommand.ExecuteCommand += OpenFileCommand_ExecuteCommand;
        OpenFileInExplorerCommand.ExecuteCommand += OpenFileInExplorerCommand_ExecuteCommand;
    }

    private void OpenFileInExplorerCommand_ExecuteCommand(SaveModel parameter)
    {
        Utils.OpenFileInExplorer(parameter.FilePath);
    }

    private void OpenFileCommand_ExecuteCommand(SaveModel parameter)
    {
        Utils.OpenLink(parameter.FilePath);
    }

    public void RefreshShowSaves(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            ShowSaves = _saves;
        else
            ShowSaves = _saves.Where(
                s => s.Name.Contains(SearchSave, StringComparison.OrdinalIgnoreCase)
            );
    }

    private void SaveWindowVM_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SearchSave))
        {
            RefreshShowSaves(SearchSave);
        }
    }

    private void LoadSaves()
    {
        var saveDirectory = Path.Combine(Environment.CurrentDirectory, "Saves");
        if (Directory.Exists(saveDirectory) is false)
            return;
        foreach (var file in Directory.EnumerateFiles(saveDirectory).Where(s => s.EndsWith(".lps")))
        {
            var lps = new LPS(File.ReadAllText(file));
            var save = new GameSave_v2(lps);
            var saveModel = new SaveModel(file, save);
            _saves.Add(saveModel);
        }
    }
}
