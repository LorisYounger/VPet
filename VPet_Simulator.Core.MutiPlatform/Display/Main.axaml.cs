using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using VPet_Simulator.Core;
using VPet_Simulator.Core.MutiPlatform.Display;

namespace VPet_Simulator.Core.MutiPlatform.Controls;

public partial class Main : UserControl, IMainUiBase
{
    private readonly MessageBar _messageBar;
    private readonly ToolBar _toolBar;
    private readonly WorkTimer _workTimer;

    public Main()
    {
        InitializeComponent();
        _messageBar = this.FindControl<MessageBar>("MessageBarControl") ?? throw new InvalidOperationException("MessageBarControl not found.");
        _toolBar = this.FindControl<ToolBar>("ToolBarControl") ?? throw new InvalidOperationException("ToolBarControl not found.");
        _workTimer = this.FindControl<WorkTimer>("WorkTimerControl") ?? throw new InvalidOperationException("WorkTimerControl not found.");
    }

    public bool IsWorking { get; private set; }

    public IMessageBarBase? MsgBarBase => _messageBar;
    public IToolBarBase? ToolBarBase => _toolBar;
    public IWorkTimerBase? WorkTimerBase => _workTimer;

    bool IUiModuleBase.IsVisible
    {
        get => IsVisible;
        set => IsVisible = value;
    }

    object IUiModuleBase.View => this;

    public void Start()
    {
        IsWorking = true;
    }

    public void Stop()
    {
        IsWorking = false;
    }

    public void Dispose()
    {
        (_messageBar as IDisposable)?.Dispose();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
