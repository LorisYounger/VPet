using LinePutScript;
using Microsoft.Win32;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace VPet_Simulator.Windows
{
    /// <summary>
    /// DIYViewer.xaml 的交互逻辑
    /// </summary>
    public partial class DIYViewer : UserControl
    {
        public DIYViewer()
        {
            InitializeComponent();
            TextName.ContextMenu = ContextMenu;
            TextContent.ContextMenu = ContextMenu;
        }
        public DIYViewer(Sub sub)
        {
            InitializeComponent();
            TextName.ContextMenu = ContextMenu;
            TextContent.ContextMenu = ContextMenu;
            TextName.Text = sub.Name;
            TextContent.Text = sub.Info;
        }
        private bool ReadKeyPress = false;
        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (ReadKeyPress)
            {
                bool isshift = false;
                string startxt = "";
                if (TextContent.Text.EndsWith(")"))
                {
                    isshift = true;
                    TextContent.Text = TextContent.Text.Substring(0, TextContent.Text.Length - 1);
                    startxt = TextContent.Text.Split('(')[0];
                }
                switch (e.Key)
                {
                    case Key.Back:
                        TextContent.AppendText("{BS}");
                        break;
                    case Key.CapsLock:
                    case Key.Delete:
                    case Key.Down:
                    case Key.Left:
                    case Key.Right:
                    case Key.Space:
                    case Key.Up:
                    case Key.End:
                    case Key.Enter:
                    case Key.Help:
                    case Key.Home:
                    case Key.Insert:
                    case Key.PageUp:
                    case Key.PageDown:
                    case Key.NumLock:
                    case Key.Tab:
                    case Key.F1:
                    case Key.F2:
                    case Key.F3:
                    case Key.F4:
                    case Key.F5:
                    case Key.F6:
                    case Key.F7:
                    case Key.F8:
                    case Key.F9:
                    case Key.F10:
                    case Key.F11:
                    case Key.F12:
                    case Key.F13:
                    case Key.F14:
                    case Key.F15:
                    case Key.F16:
                    case Key.Add:
                    case Key.Subtract:
                    case Key.Multiply:
                    case Key.Divide:
                        TextContent.AppendText($"{{{e.Key.ToString().ToUpper()}}}");
                        break;
                    case Key.Escape:
                        TextContent.AppendText("{ESC}");
                        break;
                    case Key.PrintScreen:
                        TextContent.AppendText("{PRTSC}");
                        break;
                    case Key.LeftCtrl:
                    case Key.RightCtrl:
                        if (!startxt.Contains("^"))
                            startxt += "^";
                        break;
                    case Key.LeftAlt:
                    case Key.RightAlt:
                    case Key.System:
                        if (!startxt.Contains("%"))
                            startxt += "%";
                        break;
                    case Key.RightShift:
                    case Key.LeftShift:
                        if (!startxt.Contains("+"))
                            startxt += "+";
                        break;
                    case Key.OemComma:
                        TextContent.AppendText(",");
                        break;
                    case Key.OemPeriod:
                        TextContent.AppendText(".");
                        break;
                    case Key.OemQuestion:
                        TextContent.AppendText("/");
                        break;
                    case Key.OemMinus:
                        TextContent.AppendText("-");
                        break;
                    case Key.OemPlus:
                        TextContent.AppendText("+");
                        break;
                    case Key.Oem3:
                        TextContent.AppendText("`");
                        break;
                    case Key.Oem5:
                        TextContent.AppendText("|");
                        break;
                    case Key.LWin:
                    case Key.RWin:
                        break;
                    case Key.D1:
                    case Key.D2:
                    case Key.D3:
                    case Key.D4:
                    case Key.D5:
                    case Key.D6:
                    case Key.D7:
                    case Key.D8:
                    case Key.D9:
                    case Key.D0:
                        TextContent.AppendText(e.Key.ToString().Substring(1));
                        break;
                    default:
                        TextContent.AppendText(e.Key.ToString());
                        break;
                }
                if (isshift)
                {
                    TextContent.Text = startxt + '(' + TextContent.Text.Split('(')[1] + ')';
                }
                else if (startxt.Length != 0)
                {
                    TextContent.Text = startxt + '(' + TextContent.Text + ')';
                }
                e.Handled = true;
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var bol = ((MenuItem)sender).IsChecked;
            ReadKeyPress = bol;
            TextContent.AcceptsReturn = bol;
            TextContent.AcceptsTab = bol;
        }
        public Sub ToSub()
        {
            return new Sub(TextName.Text, TextContent.Text);
        }

        private void SelectFilePath_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog sfd = new OpenFileDialog();
            sfd.Filter = "所有可执行文件|*.exe";
            if (sfd.ShowDialog() == true)
            {
                TextContent.Text = sfd.FileName;
            }
        }

        private void del_this_Click(object sender, RoutedEventArgs e)
        {
            ((StackPanel)this.Parent).Children.Remove(this);
        }

        private void Send_Top_Click(object sender, RoutedEventArgs e)
        {
            ((StackPanel)this.Parent).Children.Remove(this);
            ((StackPanel)this.Parent).Children.Insert(0, this);
        }

        private void Send_Botton_Click(object sender, RoutedEventArgs e)
        {
            ((StackPanel)this.Parent).Children.Remove(this);
            ((StackPanel)this.Parent).Children.Add(this);
        }
    }
}
