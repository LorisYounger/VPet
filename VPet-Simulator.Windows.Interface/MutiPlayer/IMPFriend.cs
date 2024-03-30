using System.Windows.Media;
using VPet_Simulator.Core;
using static VPet_Simulator.Core.GraphInfo;
using static VPet_Simulator.Windows.Interface.MPMessage;

namespace VPet_Simulator.Windows.Interface;
/// <summary>
/// 好友的宠物(图形)模块接口
/// </summary>
public interface IMPFriend
{
    /// <summary>
    /// 访客表id
    /// </summary>
    ulong LobbyID { get; }
    /// <summary>
    /// 好友id
    /// </summary>
    ulong FriendID { get; }
    /// <summary>
    /// 桌宠数据核心
    /// </summary>
    GameCore Core { get; }
    /// <summary>
    /// 图像资源集
    /// </summary>
    ImageResources ImageSources { get; }

    /// <summary>
    /// 当前宠物图形名称
    /// </summary>
    string SetPetGraph { get; }
    /// <summary>
    /// 桌宠主要部件
    /// </summary>
    Main Main { get; }

    /// <summary>
    /// 智能化显示后续过度动画
    /// </summary>
    void DisplayAuto(GraphInfo gi);

    /// <summary>
    /// 根据好友数据显示动画
    /// </summary>
    bool DisplayGraph(GraphInfo gi);
    /// <summary>
    /// 显示好友之间聊天消息
    /// </summary>
    /// <param name="msg">聊天内容</param>
    void DisplayMessage(Chat msg);

    /// <summary>
    /// 判断是否在忙碌 (被提起等, 不可进行互动)
    /// </summary>
    /// <returns></returns>
    public bool InConvenience();

    /// <summary>
    /// 判断是否在忙碌 (被提起等, 不可进行互动)
    /// </summary>
    public static bool InConvenience(Main Main)
    {
        if (Main.DisplayType.Type == GraphType.StartUP || Main.DisplayType.Type == GraphType.Raised_Dynamic || Main.DisplayType.Type == GraphType.Raised_Static)
        {
            return true;
        }
        return false;
    }
    /// <summary>
    /// 显示吃东西(夹层)动画
    /// </summary>
    /// <param name="graphName">夹层动画名</param>
    /// <param name="imageSource">被夹在中间的图片</param>
    void DisplayFoodAnimation(string graphName, ImageSource imageSource);
    /// <summary>
    /// 是否不允许交互
    /// </summary>
    bool NOTouch { get; }
}
