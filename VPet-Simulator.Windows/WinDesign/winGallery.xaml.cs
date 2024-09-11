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

        //每次打开的时候都检查下是否解锁, 并自动解锁
        //这个解锁条件可以塞到 保存前的检查里面
        mw.CheckGalleryUnlock();

        //Note:这个是给不二一的示例: 不用可以删了

        //锁定的图片
        var lockphoto = mw.Photos.FindAll(x => x.IsUnlock == false);
        foreach (var item in lockphoto)
        {

        }
        //解锁的图片
        var unlockphoto = mw.Photos.FindAll(x => x.IsUnlock == true);
        foreach (var item in unlockphoto)
        {

        }
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        mw.winGallery = null;
    }
}
