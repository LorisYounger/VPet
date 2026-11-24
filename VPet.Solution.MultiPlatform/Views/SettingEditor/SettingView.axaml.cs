using Avalonia.Controls;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;

namespace VPet.Solution.MultiPlatform.Views;

public partial class SettingView : UserControl
{
    public SettingView()
    {
        InitializeComponent();
        RootGrid.PropertyChanged += (_, e) =>
        {
            if (e.Property == ResponsiveHelper.IsCompactProperty && e.NewValue is true)
                MainSplitView.IsPaneOpen = false;
        };
    }

    private void TogglePaneButton_Click(object? sender, RoutedEventArgs e)
    {
        MainSplitView.IsPaneOpen = MainSplitView.IsPaneOpen is false;
    }

    private void Settings_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ResponsiveHelper.GetIsCompact(RootGrid))
            MainSplitView.IsPaneOpen = false;
    }

    private void SubNavView_SelectionChanged(
        object? sender,
        FANavigationViewSelectionChangedEventArgs e
    )
    {
        if (e.SelectedItem is not FANavigationViewItem item || item.Tag is not string tag)
            return;

        GraphicsView.IsVisible = tag == "Graphics";
        SystemView.IsVisible = tag == "System";
        DiagnosticView.IsVisible = tag == "Diagnostic";
        InteractiveView.IsVisible = tag == "Interactive";
        CustomizedView.IsVisible = tag == "Customized";
        ModView.IsVisible = tag == "Mod";
    }
}
