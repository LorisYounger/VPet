using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static System.Net.Mime.MediaTypeNames;
using VPet_Simulator.Core;

namespace VPet_Simulator.Windows;
/// <summary>
/// winInputBox.xaml 的交互逻辑
/// </summary>
public partial class winInputBox : Window
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
    private void TextBoxInput_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!TextBoxInput.AcceptsReturn && e.Key == Key.Enter)
        {
            ReturnYes = true;
            Close();
        }
    }
    public bool ReturnYes = false;
    private void ButtonYes_Click(object sender, RoutedEventArgs e)
    {
        ReturnYes = true;
        Close();
    }
    Action<string> ENDAction;
    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
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

    private void Window_Closed(object sender, EventArgs e)
    {
        ENDAction?.Invoke(TextBoxInput.Text);
        mw.Windows.Remove(this);
    }
}
