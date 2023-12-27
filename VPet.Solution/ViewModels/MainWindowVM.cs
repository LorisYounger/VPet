using HKW.HKWUtils.Observable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VPet_Simulator.Windows.Interface;

namespace VPet.Solution.ViewModels;

public class MainWindowVM : ObservableClass<MainWindowVM>
{
    public MainWindowVM() { }

    public static void LoadSettings(string path)
    {
        foreach (var file in Directory.EnumerateFiles(path))
        {
            var setting = new Setting(path);
        }
    }
}
