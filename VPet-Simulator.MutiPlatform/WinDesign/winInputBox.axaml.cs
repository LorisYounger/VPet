using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using VPet_Simulator.Core.MutiPlatform.Display.Shell;

namespace VPet_Simulator.MutiPlatform;
/// <summary>
/// winInputBox.xaml 的交互逻辑
/// </summary>
public partial class winInputBox : VPetWindow
{
    MainWindow mw;
    public winInputBox(MainWindow mainw, string title, string text, string defaulttext, bool AllowMutiLine = false, bool CanHide = false, bool TextCenter = true)
    {
        InitializeComponent();
        mw = mainw;
        Text.Text = text;
        Title = title;
        TextBoxInput.AcceptsReturn = AllowMutiLine;
        TextBoxInput.Text = defaulttext;

        if (!TextCenter)
        {
            Text.TextAlignment = TextAlignment.Left;
        }
    }
    //跨平台: Avalonia 没有 PreviewKeyDown, 用 KeyDown; 不接受回车时 TextBox 不吃 Enter, 事件照样到这里
    private void TextBoxInput_PreviewKeyDown(object? sender, KeyEventArgs e)
    {
        if (!TextBoxInput.AcceptsReturn && e.Key == Key.Enter)
        {
            ReturnYes = true;
            Close();
        }
    }
    public bool ReturnYes = false;
    private void ButtonYes_Click(object? sender, RoutedEventArgs e)
    {
        ReturnYes = true;
        Close();
    }
    Action<string>? ENDAction;
    private void Window_Closing(object? sender, WindowClosingEventArgs e)
    {
        if (!ReturnYes)
            TextBoxInput.Text = "";
    }
    public static winInputBox Show(MainWindow mainw, string title, string text, string defaulttext, Action<string> ENDAction, bool AllowMutiLine = false, bool TextCenter = true, bool CanHide = false)
    {
        winInputBox msgbox = new winInputBox(mainw, title, text, defaulttext, AllowMutiLine, CanHide, TextCenter);
        msgbox.ENDAction = ENDAction;
        mainw.Windows.Add(msgbox);
        msgbox.ShowDialog();
        return msgbox;
    }

    private void Window_Closed(object? sender, EventArgs e)
    {
        ENDAction?.Invoke(TextBoxInput.Text ?? "");
        mw.Windows.Remove(this);
    }
}
