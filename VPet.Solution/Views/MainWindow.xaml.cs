using HKW.HKWUtils;
using LinePutScript.Localization.WPF;
using Panuon.WPF.UI;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using VPet.Solution.ViewModels;
using VPet.Solution.Views.SaveViewer;
using VPet.Solution.Views.SettingEditor;

namespace VPet.Solution.Views;

/// <summary>
/// MainWindow.xaml 的交互逻辑
/// </summary>
public partial class MainWindow : WindowX
{
    public MainWindowVM ViewModel => (MainWindowVM)DataContext;

    public SettingWindow SettingWindow { get; } = new();
    public SaveWindow SaveWindow { get; } = new();

    public MainWindow()
    {
        if (App.IsDone)
        {
            Close();
            return;
        }
        InitializeComponent();
        this.SetViewModel<MainWindowVM>();
        LocalizeCore.StoreTranslation = true;
        LocalizeCore.LoadDefaultCulture();
        ComboBox_Langs.ItemsSource = LocalizeCore.AvailableCultures;
        ComboBox_Langs.SelectedItem = LocalizeCore.CurrentCulture;
        Closed += MainWindow_Closed;
    }

    private void MainWindow_Closed(object sender, EventArgs e)
    {
        SettingWindow.CloseX();
        SaveWindow.CloseX();
    }

    private void Button_OpenSettingEditor_Click(object sender, RoutedEventArgs e)
    {
        SettingWindow.ShowOrActivate();
    }

    private void Button_OpenSaveViewer_Click(object sender, RoutedEventArgs e)
    {
        SaveWindow.ShowOrActivate();
    }

    private void Button_OpenLocalText_Click(object sender, RoutedEventArgs e)
    {
        var sb = new StringBuilder();
        foreach (var a in LocalizeCore.StoreTranslationList)
            sb.AppendLine(a.Replace("\r\n", "\\r\\n"));
        MessageBoxX.Show(sb.ToString());
    }

    private void ComboBox_Langs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        LocalizeCore.LoadCulture((string)ComboBox_Langs.SelectedItem);
    }
}
