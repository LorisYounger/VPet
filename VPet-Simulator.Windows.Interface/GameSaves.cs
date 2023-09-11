using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VPet_Simulator.Core;

namespace VPet_Simulator.Windows.Interface
{
    /// <summary>
    /// 游戏存档
    /// </summary>
    public class GameSaves
    {
        Dictionary<string, GameSave> Saves = new Dictionary<string, GameSave>();
    }
}
