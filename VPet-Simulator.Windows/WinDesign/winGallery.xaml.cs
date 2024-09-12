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
using VPet_Simulator.Windows.Interface;

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

        //获取锁定的图片
        var lockphoto = mw.Photos.FindAll(x => x.IsUnlock == false);
        foreach (var item in lockphoto)
        {
            
        }
        //获取解锁的图片
        var unlockphoto = mw.Photos.FindAll(x => x.IsUnlock == true);
        foreach (var item in unlockphoto)
        {
            
        }

        //补充策划案未写出的内容:

        //玩家可以查看解锁和未解锁的图片

        //图片获取: item.GetImage(mw) (全分辨率)
        //图片获取: Photo.ConvertToThumbnail(item.GetImage(mw),宽,高) (缩略图,宽高以最小比例为准)
        //转换成黑白(用于未解锁): Photo.ConvertToBlackWhite(item.GetImage(mw)) Photo.ConvertToGrayScale

        //图片有标题和描述, 描述可以只在解锁后显示, 解锁前可以显示解锁条件
        //解锁条件 item.UnlockAble.CheckReason(mw)

        //未解锁的图片有部分支持花钱解锁,可能需要做上相关功能
        //解锁的图片支持多选后导出

        //逻辑啥的可以空出来我写
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        mw.winGallery = null;
    }
}
