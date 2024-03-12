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
using static VPet_Simulator.Core.GraphHelper;

namespace VPet_Simulator.Windows;
/// <summary>
/// winWorkMenu.xaml 的交互逻辑
/// </summary>
public partial class winWorkMenu : Window
{
    MainWindow mw;
    List<Work> ws;
    List<Work> ss;
    List<Work> ps;
    public winWorkMenu(MainWindow mw)
    {
        InitializeComponent();
        this.mw = mw;

        mw.Main.WorkList(out ws, out ss, out ps);

        foreach (var v in ws)
        {
            lbWork.Items.Add(v.NameTrans);
        }
        foreach (var v in ss)
        {
            lbStudy.Items.Add(v.NameTrans);
        }
        foreach (var v in ps)
        {
            lbPlay.Items.Add(v.NameTrans);
        }

    }
}
