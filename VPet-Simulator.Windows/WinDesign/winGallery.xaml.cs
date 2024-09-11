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

namespace VPet_Simulator.Windows;
/// <summary>
/// winGallery.xaml 的交互逻辑
/// </summary>
public partial class winGallery : Window
{
    MainWindow mw;
    public winGallery(MainWindow mw)
    {
        InitializeComponent();
        this.mw = mw;
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        mw.winGallery = null;
    }
}
