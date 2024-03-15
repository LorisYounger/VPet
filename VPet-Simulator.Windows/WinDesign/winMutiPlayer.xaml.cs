using Steamworks;
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

namespace VPet_Simulator.Windows.WinDesign;
/// <summary>
/// winMutiPlayer.xaml 的交互逻辑
/// </summary>
public partial class winMutiPlayer : Window
{
    Steamworks.Data.Lobby lb;
    MainWindow mw;
    public winMutiPlayer(MainWindow mw, ulong? lobbyid = null)
    {
        InitializeComponent();
        this.mw = mw;
        if (lobbyid == null)
            CreateLobby();
        else
            JoinLobby(lobbyid);
    }
    public async void JoinLobby(ulong? lobbyid)
    {
        lb = (await SteamMatchmaking.JoinLobbyAsync((SteamId)lobbyid)).Value;
    }
    public async void CreateLobby()
    {
        lb = (await SteamMatchmaking.CreateLobbyAsync()).Value;
        lb.SetJoinable(true);
        lb.SetPublic();
        
    }
    public void ShowLobbyInfo()
    {
        hostName.Text = lb.Owner.Name;
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        lb.Leave();
        mw.winMutiPlayer = null;
    }
}
