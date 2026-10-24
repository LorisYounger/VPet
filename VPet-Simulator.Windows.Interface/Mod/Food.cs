using System.Linq;
namespace VPet_Simulator.Windows.Interface;

public partial class Food
{
    public void LoadImageSource(IMainWindow imw)
        {
            ImageSource = imw.ImageSources.FindImage("food_" + (Image ?? Name), "food");
            Star = imw.Set["betterbuy"]["star"].GetInfos().Contains(Name);
            LoadEatTimeSource(imw);
            CalRealPrice(imw);
        }
}
