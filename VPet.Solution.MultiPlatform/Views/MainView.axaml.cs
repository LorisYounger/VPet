using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
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
#if DEBUG
        BottomToolbar.Children.Add(
            new Button()
            {
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Content = "导出翻译文本".Translate(),
                Command = ((MainViewModel)DataContext!).OutputLocalTextCommand,
            }
        );
#endif
        base.OnLoaded(e);
    }

    private void ThemeVariant_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var item = e.AddedItems.OfType<ComboBoxItem>().FirstOrDefault();
        if (item is null)
            return;
        if (item.Tag is "Default")
        {
            Application.Current?.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Default;
        }
        else
        {
            Application
                .Current
                ?.RequestedThemeVariant = item.Tag is "Light"
                    ? Avalonia.Styling.ThemeVariant.Light
                    : Avalonia.Styling.ThemeVariant.Dark;
        }
    }
}
