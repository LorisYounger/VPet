using System;

namespace VPet_Simulator.Core
{
    public interface ITouchArea
    {
        Func<bool> DoAction { get; }
        bool IsPress { get; }
        bool Touch(double x, double y);
    }

    public abstract class TouchAreaBase : ITouchArea
    {
        protected TouchAreaBase(double locateX, double locateY, double width, double height, Func<bool> doAction, bool isPress = false)
        {
            LocateX = locateX;
            LocateY = locateY;
            Width = width;
            Height = height;
            DoAction = doAction;
            IsPress = isPress;
        }

        public double LocateX { get; set; }
        public double LocateY { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public Func<bool> DoAction { get; }
        public bool IsPress { get; set; }

        public bool Touch(double x, double y)
        {
            double inx = x - LocateX;
            double iny = y - LocateY;
            return inx >= 0 && inx <= Width && iny >= 0 && iny <= Height;
        }
    }
}
