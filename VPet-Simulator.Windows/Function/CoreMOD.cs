using LinePutScript;
using Steamworks.Ugc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;
using static VPet_Simulator.Core.GraphCore;

namespace VPet_Simulator.Windows
{
    public class CoreMOD
    {
        public static List<string> LoadedDLL { get; } = new List<string>()
        {
            "ChatGPT.API.Framework.dll","Panuon.WPF.dll","steam_api.dll","Panuon.WPF.UI.dll","steam_api64.dll",
            "LinePutScript.dll","Newtonsoft.Json.dll","Facepunch.Steamworks.Win32.dll", "Facepunch.Steamworks.Win64.dll",
            "VPet-Simulator.Core.dll","VPet-Simulator.Windows.Interface.dll"
        };
        public static string NowLoading = null;
        public string Name;
        public string Author;
        /// <summary>
        /// 如果是上传至Steam,则为SteamUserID
        /// </summary>
        public long AuthorID;
        /// <summary>
        /// 上传至Steam的ItemID
        /// </summary>
        public ulong ItemID;
        public string Intro;
        public DirectoryInfo Path;
        public int GameVer;
        public int Ver;
        public string Content = "";
        public bool SuccessLoad = true;
        public static string INTtoVER(int ver) => $"{ver / 100}.{ver % 100:00}";
        public CoreMOD(DirectoryInfo directory, MainWindow mw)
        {
            Path = directory;
            LpsDocument modlps = new LpsDocument(File.ReadAllText(directory.FullName + @"\info.lps"));
            Name = modlps.FindLine("vupmod").Info;
            NowLoading = Name;
            Intro = modlps.FindLine("intro").Info;
            GameVer = modlps.FindSub("gamever").InfoToInt;
            Ver = modlps.FindSub("ver").InfoToInt;
            Author = modlps.FindSub("author").Info;
            if (modlps.FindLine("authorid") != null)
                AuthorID = modlps.FindLine("authorid").InfoToInt64;
            else
                AuthorID = 0;
            if (modlps.FindLine("itemid") != null)
                ItemID = Convert.ToUInt64(modlps.FindLine("itemid").info);
            else
                ItemID = 0;
            if (IsBanMOD(mw))
            {
                Content = "该模组已停用";
                return;
            }

            foreach (DirectoryInfo di in Path.EnumerateDirectories())
            {
                switch (di.Name.ToLower())
                {
                    case "pet":
                        //宠物模型
                        Content += "宠物形象\n";
                        foreach (FileInfo fi in di.EnumerateFiles("*.lps"))
                        {
                            LpsDocument lps = new LpsDocument(File.ReadAllText(fi.FullName));
                            if (lps.First().Name.ToLower() == "pet")
                            {
                                var name = lps.First().Info;
                                var p = mw.Pets.FirstOrDefault(x => x.Name == name);
                                if (p == null)
                                    mw.Pets.Add(new PetLoader(lps, di));
                                else
                                {
                                    p.path.Add(di.FullName + "\\" + lps.First()["path"].Info);
                                    p.Config.Set(lps);
                                }
                            }
                        }
                        break;
                    case "plugin":
                        Content += "代码插件\n";
                        SuccessLoad = false;
                        if (!IsPassMOD(mw))
                        {//不是通过模组,不加载
                            break;
                        }

                        foreach (FileInfo tmpfi in di.EnumerateFiles("*.dll"))
                        {
                            try
                            {
                                var path = tmpfi.Name;
                                if (LoadedDLL.Contains(path))
                                    continue;
                                LoadedDLL.Add(path);
                                Assembly dll = Assembly.LoadFrom(tmpfi.FullName);
                                var v = dll.GetExportedTypes();
                                foreach (Type exportedType in v)
                                {
                                    if (exportedType.BaseType == typeof(MainPlugin))
                                    {
                                        mw.Plugins.Add((MainPlugin)Activator.CreateInstance(exportedType, mw));
                                    }
                                }
                            }
                            catch
                            {

                            }
                        }

                        SuccessLoad = true;
                        break;
                }
            }
        }
        public bool IsBanMOD(MainWindow mw) => mw.Set.IsBanMod(Name);
        public bool IsPassMOD(MainWindow mw) => mw.Set.IsPassMOD(Name);

        public void WriteFile()
        {
            LpsDocument modlps = new LpsDocument(File.ReadAllText(Path.FullName + @"\info.lps"));
            modlps.FindLine("vupmod").Info = Name;
            modlps.FindLine("intro").Info = Intro;
            modlps.FindSub("gamever").InfoToInt = GameVer;
            modlps.FindSub("ver").InfoToInt = Ver;
            modlps.FindSub("author").Info = Author;
            modlps.FindorAddLine("authorid").InfoToInt64 = AuthorID;
            modlps.FindorAddLine("itemid").info = ItemID.ToString();
            File.WriteAllText(Path.FullName + @"\info.lps", modlps.ToString());
        }
    }
}
