using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using VPet.Solution.MultiPlatform.ViewModels;

namespace VPet.Solution.MultiPlatform.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        SyncThemeVariantSelection();
#if DEBUG
        if (
            BottomToolbar.Children.OfType<Button>().Any(b => b.Name == "DebugOutputLocalText")
            is false
        )
        {
            BottomToolbar.Children.Add(
                new Button()
                {
                    Name = "DebugOutputLocalText",
                    Margin = new Thickness(4),
                    Classes = { "Link" },
                    Content = "导出翻译文本".Translate(),
                    Command = ((MainViewModel)DataContext!).OutputLocalTextCommand,
                }
            );
        }
#endif
        base.OnLoaded(e);
    }

    private void SyncThemeVariantSelection()
    {
        var current = Application.Current?.RequestedThemeVariant;
        var targetTag = "Default";
        if (current == Avalonia.Styling.ThemeVariant.Light)
            targetTag = "Light";
        else if (current == Avalonia.Styling.ThemeVariant.Dark)
            targetTag = "Dark";

        var matched = ThemeVariant
            .Items.OfType<ComboBoxItem>()
            .FirstOrDefault(item => Equals(item.Tag, targetTag));
        if (matched is not null && Equals(ThemeVariant.SelectedItem, matched) is false)
        {
            ThemeVariant.SelectedItem = matched;
        }
    }

    private void ThemeVariant_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (Application.Current is null)
            return;
        var item = e.AddedItems.OfType<ComboBoxItem>().FirstOrDefault();
        if (item is null)
            return;
        Application.Current.RequestedThemeVariant = item.Tag switch
        {
            "Light" => Avalonia.Styling.ThemeVariant.Light,
            "Dark" => Avalonia.Styling.ThemeVariant.Dark,
            _ => Avalonia.Styling.ThemeVariant.Default,
        };
    }
}
