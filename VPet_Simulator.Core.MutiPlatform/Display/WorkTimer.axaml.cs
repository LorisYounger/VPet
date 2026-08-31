using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using VPet_Simulator.Core;

namespace VPet_Simulator.Core.MutiPlatform.Controls;

public partial class WorkTimer : UserControl, IWorkTimerBase
{
    private readonly TextBlock _nowText;
    private readonly TextBlock _numberText;
    private readonly TextBlock _unitText;

    public WorkTimer()
    {
        InitializeComponent();
        _nowText = this.FindControl<TextBlock>("NowText") ?? throw new InvalidOperationException("NowText not found.");
        _numberText = this.FindControl<TextBlock>("NumberText") ?? throw new InvalidOperationException("NumberText not found.");
        _unitText = this.FindControl<TextBlock>("UnitText") ?? throw new InvalidOperationException("UnitText not found.");
        IsVisible = false;
    }

    public int DisplayType { get; set; }
    public double GetCount { get; set; }
    public DateTime StartTime { get; set; }

    bool IUiModuleBase.IsVisible
    {
        get => IsVisible;
        set => IsVisible = value;
    }

    object IUiModuleBase.View => this;

    public void ShowTimeSpan(TimeSpan ts)
    {
        if (ts.TotalSeconds < 90)
        {
            _numberText.Text = ts.TotalSeconds.ToString("f1");
            _unitText.Text = "sec";
        }
        else if (ts.TotalMinutes < 90)
        {
            _numberText.Text = ts.TotalMinutes.ToString("f1");
            _unitText.Text = "min";
        }
        else
        {
            _numberText.Text = ts.TotalHours.ToString("f1");
            _unitText.Text = "hour";
        }
    }

    public void Start(string workName)
    {
        _nowText.Text = workName;
        StartTime = DateTime.Now;
        IsVisible = true;
    }

    public void Stop()
    {
        _nowText.Text = "Idle";
        _numberText.Text = "0";
        _unitText.Text = "sec";
        IsVisible = false;
    }

    public void Dispose()
    {
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
