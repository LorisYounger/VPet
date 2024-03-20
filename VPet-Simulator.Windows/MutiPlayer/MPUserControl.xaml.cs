using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using VPet_Simulator.Windows.Interface;

namespace VPet_Simulator.Windows;
/// <summary>
/// MPUserControl.xaml 的交互逻辑
/// </summary>
public partial class MPUserControl : Border
{
    public Friend friend => mpf.friend;
    winMutiPlayer wmp;
    public MPFriends mpf;
    Lobby lb => mpf.lb;
    public MPUserControl(winMutiPlayer wmp, MPFriends mpf)
    {
        InitializeComponent();
        this.wmp = wmp;
        this.mpf = mpf;
        Task.Run(LoadInfo);
    }
    public void LoadInfo()
    {
        //加载lobby传过来的数据       
        while (!mpf.Loaded)
        {
            Thread.Sleep(500);
        }
        Dispatcher.Invoke(async () =>
        {
            rPetName.Text = mpf.Core.Save.Name;
            hostName.Text = friend.Name;
            var img = await friend.GetMediumAvatarAsync();
            uimg.Source = winMutiPlayer.ConvertToImageSource(img);
            info.Text = "Lv " + mpf.Core.Save.Level;
            if (lb.Owner.IsMe)
                Kick.Visibility = Visibility.Visible;
        });
    }

    private void btn_ReSetLocal(object sender, RoutedEventArgs e)
    {
        mpf.ReSetLocal();
    }


    private void Kick_Click(object sender, RoutedEventArgs e)
    {
        lb.SetData("kick", friend.Id.Value.ToString());
    }
}
