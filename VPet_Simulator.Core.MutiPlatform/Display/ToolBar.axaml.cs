using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using VPet_Simulator.Core;

namespace VPet_Simulator.Core.MutiPlatform.Controls;

public partial class ToolBar : UserControl, IToolBarBase
{
    public ToolBar()
    {
        InitializeComponent();
        IsVisible = false;
    }

    bool IUiModuleBase.IsVisible
    {
        get => IsVisible;
        set => IsVisible = value;
    }

    object IUiModuleBase.View => this;

    public void LoadWork()
    {
    }

    public void LoadDIY()
    {
    }

    public void Show()
    {
        IsVisible = true;
    }

    public void Dispose()
    {
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
