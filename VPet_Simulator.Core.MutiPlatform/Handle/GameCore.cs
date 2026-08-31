using Avalonia;
using System;
using VPet_Simulator.Core;
using VPet_Simulator.Core.MutiPlatform.Graph;

namespace VPet_Simulator.Core.MutiPlatform;

public class GameCore : GameCoreBase<TouchArea>
{
    public GraphCore? Graph;
}

public class TouchArea : TouchAreaBase
{
    public Point Locate
    {
        get => new(LocateX, LocateY);
        set
        {
            LocateX = value.X;
            LocateY = value.Y;
        }
    }

    public Size Size
    {
        get => new(Width, Height);
        set
        {
            Width = value.Width;
            Height = value.Height;
        }
    }

    public TouchArea(Point locate, Size size, Func<bool> doAction, bool isPress = false)
        : base(locate.X, locate.Y, size.Width, size.Height, doAction, isPress)
    {
    }

    public bool Touch(Point point) => Touch(point.X, point.Y);
}
