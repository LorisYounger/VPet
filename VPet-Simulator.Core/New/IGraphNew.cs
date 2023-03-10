using System.IO;

namespace VPet_Simulator.Core.New
{
    public interface IGraphNew
    {
        void Order(Stream stream);
        void Clear();
    }
}
