using LinePutScript.Converter;
using LinePutScript.Dictionary;
using LinePutScript.Localization.WPF;
using LinePutScript;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;

namespace VPet_Simulator.Windows;
public class MPMOD
{
    public static void LoadImage(MPFriends mw, DirectoryInfo di, string pre = "")
    {
        //加载其他放在文件夹的图片
        foreach (FileInfo fi in di.EnumerateFiles("*.png"))
        {
            mw.ImageSources.AddSource(pre + fi.Name.ToLower().Substring(0, fi.Name.Length - 4), fi.FullName);
        }
        //加载其他放在文件夹中文件夹的图片
        foreach (DirectoryInfo fordi in di.EnumerateDirectories())
        {
            LoadImage(mw, fordi, pre + fordi.Name + "_");
        }
        //加载标志好的图片和图片设置
        foreach (FileInfo fi in di.EnumerateFiles("*.lps"))
        {
            var tmp = new LpsDocument(File.ReadAllText(fi.FullName));
            if (fi.Name.ToLower().StartsWith("set_"))
                foreach (var line in tmp)
                    mw.ImageSources.ImageSetting.AddorReplaceLine(line);
            else
                mw.ImageSources.AddImages(tmp, di.FullName);
        }
    }
    public string Name { get; set; }
    public MPMOD(DirectoryInfo directory, MPFriends mw)
    {
#if !DEBUG
            try
            {
#endif
        var Path = directory;
        LpsDocument modlps = new LpsDocument(File.ReadAllText(directory.FullName + @"\info.lps"));
        Name = modlps.FindLine("vupmod").Info;

        //MOD未加载时支持翻译
        foreach (var line in modlps.FindAllLine("lang"))
        {
            List<ILine> ls = new List<ILine>();
            foreach (var sub in line)
            {
                ls.Add(new Line(sub.Name, sub.info));
            }
            LocalizeCore.AddCulture(line.info, ls);
        }
        if (!IsOnMOD(mw))
        {          
            return;
        }

        foreach (DirectoryInfo di in Path.EnumerateDirectories())
        {
            switch (di.Name.ToLower())
            {               
                case "pet":
                    //宠物模型                           
                    foreach (FileInfo fi in di.EnumerateFiles("*.lps"))
                    {
                        LpsDocument lps = new LpsDocument(File.ReadAllText(fi.FullName));
                        if (lps.First().Name.ToLower() == "pet")
                        {
                            var name = lps.First().Info;
                            var p = mw.Pets.FirstOrDefault(x => x.Name == name);
                            if (p == null)
                            {
                                p = new PetLoader(lps, di);                               
                                mw.Pets.Add(p);
                            }
                            else
                            {        
                                var dis = new DirectoryInfo(di.FullName + "\\" + lps.First()["path"].Info);
                                p.path.Add(di.FullName + "\\" + lps.First()["path"].Info);
                                p.Config.Set(lps);
                            }
                        }
                    }
                    break;
                case "food":
                    foreach (FileInfo fi in di.EnumerateFiles("*.lps"))
                    {
                        var tmp = new LpsDocument(File.ReadAllText(fi.FullName));
                        foreach (ILine li in tmp)
                        {
                            if (li.Name != "food")
                                continue;
                            string tmps = li.Find("name").info;
                            mw.Foods.RemoveAll(x => x.Name == tmps);
                            mw.Foods.Add(LPSConvert.DeserializeObject<Food>(li));
                        }
                    }
                    break;
                case "image":
                    LoadImage(mw, di);
                    break;
                case "lang":
                    foreach (FileInfo fi in di.EnumerateFiles("*.lps"))
                    {
                        LocalizeCore.AddCulture(fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length), new LPS_D(File.ReadAllText(fi.FullName)));
                    }
                    foreach (DirectoryInfo dis in di.EnumerateDirectories())
                    {
                        foreach (FileInfo fi in dis.EnumerateFiles("*.lps"))
                        {
                            LocalizeCore.AddCulture(dis.Name, new LPS_D(File.ReadAllText(fi.FullName)));
                        }
                    }
                    break;
            }
        }
    }
    public bool IsOnMOD(MPFriends mw) => mw.IsOnMod(Name);
}