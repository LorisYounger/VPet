using Avalonia.Controls;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;

namespace VPet.Solution.MultiPlatform.Views;

public partial class SaveView : UserControl
{
    public SaveView()
    {
        InitializeComponent();
        SaveRootGrid.PropertyChanged += (_, e) =>
        {
            if (e.Property == ResponsiveHelper.IsCompactProperty && e.NewValue is true)
                SaveSplitView.IsPaneOpen = false;
        };
    }

    private void ToggleSavePaneButton_Click(object? sender, RoutedEventArgs e)
    {
        SaveSplitView.IsPaneOpen = SaveSplitView.IsPaneOpen is false;
    }

    private void Saves_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ResponsiveHelper.GetIsCompact(SaveRootGrid))
            SaveSplitView.IsPaneOpen = false;
    }

    private void SaveNavView_SelectionChanged(
        object? sender,
        FANavigationViewSelectionChangedEventArgs e
    )
    {
        if (e.SelectedItem is not FANavigationViewItem item || item.Tag is not string tag)
            return;

        SaveDataContent.IsVisible = tag == "Data";
        SaveStatisticContent.IsVisible = tag == "Statistic";
    }
}
