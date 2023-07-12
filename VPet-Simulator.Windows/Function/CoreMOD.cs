using LinePutScript;
using LinePutScript.Converter;
using LinePutScript.Dictionary;
using LinePutScript.Localization.WPF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;

namespace VPet_Simulator.Windows
{
    public class CoreMOD
    {
        public static List<string> LoadedDLL { get; } = new List<string>()
        {
            "ChatGPT.API.Framework.dll","Panuon.WPF.dll","steam_api.dll","Panuon.WPF.UI.dll","steam_api64.dll",
            "LinePutScript.dll","Newtonsoft.Json.dll","Facepunch.Steamworks.Win32.dll", "Facepunch.Steamworks.Win64.dll",
            "VPet-Simulator.Core.dll","VPet-Simulator.Windows.Interface.dll","LinePutScript.Localization.WPF.dll"
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
        public HashSet<string> Tag = new HashSet<string>();
        public bool SuccessLoad = true;
        public DateTime CacheDate;
        public static string INTtoVER(int ver) => $"{ver / 100}.{ver % 100:00}";
        public static void LoadImage(MainWindow mw, DirectoryInfo di)
        {
            //加载其他放在文件夹的图片
            foreach (FileInfo fi in di.EnumerateFiles("*.png"))
            {
                mw.ImageSources.AddSource(fi.Name.ToLower().Substring(0, fi.Name.Length - 4), fi.FullName);
            }
            //加载其他放在文件夹中文件夹的图片
            foreach (DirectoryInfo fordi in di.EnumerateDirectories())
            {
                LoadImage(mw, fordi);
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
        public CoreMOD(DirectoryInfo directory, MainWindow mw)
        {
            Path = directory;
            LpsDocument modlps = new LpsDocument(File.ReadAllText(directory.FullName + @"\info.lps"));
            Name = modlps.FindLine("vupmod").Info;
            NowLoading = Name;
            Intro = modlps.FindLine("intro").Info;
            GameVer = modlps.FindSub("gamever").InfoToInt;
            Ver = modlps.FindSub("ver").InfoToInt;
            Author = modlps.FindSub("author").Info.Split('[').First();
            if (modlps.FindLine("authorid") != null)
                AuthorID = modlps.FindLine("authorid").InfoToInt64;
            else
                AuthorID = 0;
            if (modlps.FindLine("itemid") != null)
                ItemID = Convert.ToUInt64(modlps.FindLine("itemid").info);
            else
                ItemID = 0;
            CacheDate = modlps.GetDateTime("cachedate", DateTime.MinValue);
            if (!IsOnMOD(mw))
            {
                //Content = "该模组已停用".Translate();
                return;
            }

            foreach (DirectoryInfo di in Path.EnumerateDirectories())
            {
                switch (di.Name.ToLower())
                {
                    case "pet":
                        //宠物模型
                        Tag.Add("pet");
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
                    case "food":
                        Tag.Add("food");
                        foreach (FileInfo fi in di.EnumerateFiles("*.lps"))
                        {
                            var tmp = new LpsDocument(File.ReadAllText(fi.FullName));
                            foreach (ILine li in tmp)
                            {
                                string tmps = li.Find("name").info;
                                mw.Foods.RemoveAll(x => x.Name == tmps);
                                mw.Foods.Add(LPSConvert.DeserializeObject<Food>(li));
                            }
                        }
                        break;
                    case "image":
                        Tag.Add("image");
                        LoadImage(mw, di);
                        break;
                    case "text":
                        Tag.Add("text");
                        foreach (FileInfo fi in di.EnumerateFiles("*.lps"))
                        {
                            var tmp = new LpsDocument(File.ReadAllText(fi.FullName));
                            foreach (ILine li in tmp)
                            {
                                switch (li.Name.ToLower())
                                {
                                    case "lowfoodtext":
                                        mw.LowFoodText.Add(LPSConvert.DeserializeObject<LowText>(li));
                                        break;
                                    case "lowdrinktext":
                                        mw.LowDrinkText.Add(LPSConvert.DeserializeObject<LowText>(li));
                                        break;
                                }
                            }
                        }
                        break;
                    case "lang":
                        Tag.Add("lang");
                        foreach (DirectoryInfo dis in di.EnumerateDirectories())
                        {
                            foreach (FileInfo fi in dis.EnumerateFiles("*.lps"))
                            {
                                LocalizeCore.AddCulture(dis.Name, new LPS_D(File.ReadAllText(fi.FullName)));
                            }
                        }

                        if (mw.Set.Language == "null")
                        {
                            LocalizeCore.LoadDefaultCulture();
                        }
                        else
                            LocalizeCore.LoadCulture(mw.Set.Language);
                        break;
                    case "plugin":
                        Tag.Add("plugin");
                        SuccessLoad = true;
                        foreach (FileInfo tmpfi in di.EnumerateFiles("*.dll"))
                        {
                            try
                            {
                                var path = tmpfi.Name;
                                if (LoadedDLL.Contains(path))
                                    continue;
                                LoadedDLL.Add(path);
                                Assembly dll = Assembly.LoadFrom(tmpfi.FullName);
                                var certificate = dll.GetModules()?.First()?.GetSignerCertificate();
                                if (certificate != null)
                                {
                                    if (certificate.Subject == "CN=\"Shenzhen Lingban Computer Technology Co., Ltd.\", O=\"Shenzhen Lingban Computer Technology Co., Ltd.\", L=Shenzhen, S=Guangdong Province, C=CN, SERIALNUMBER=91440300MA5H8REU3K, OID.2.5.4.15=Private Organization, OID.1.3.6.1.4.1.311.60.2.1.1=Shenzhen, OID.1.3.6.1.4.1.311.60.2.1.2=Guangdong Province, OID.1.3.6.1.4.1.311.60.2.1.3=CN"
                                        && certificate.Issuer == "CN=DigiCert Trusted G4 Code Signing RSA4096 SHA384 2021 CA1, O=\"DigiCert, Inc.\", C=US")
                                    {//LBGame 信任的证书
                                        if (!Author.Contains("["))
                                            Author += "[认证]".Translate();
                                    }
                                    else if (certificate.Subject != "CN=Microsoft Corporation, O=Microsoft Corporation, L=Redmond, S=Washington, C=US" && !IsPassMOD(mw))
                                    {//不是通过模组,不加载
                                        SuccessLoad = false;
                                        continue;
                                    }
                                    else if (!Author.Contains("["))
                                    {
                                        Author += "[签名]".Translate();
                                        Intro += $"Subject:{certificate.Subject}\nIssuer:{certificate.Subject}";
                                    }
                                }
                                else
                                {
                                    if (!IsPassMOD(mw))
                                    {//不是通过模组,不加载
                                        continue;
                                    }
                                }
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
                                SuccessLoad = false;
                            }
                        }
                        break;
                }
            }
        }
        public bool IsOnMOD(MainWindow mw) => mw.Set.IsOnMod(Name);
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
    public static class ExtensionSetting
    {
        public static bool IsOnMod(this Setting t, string ModName)
        {
            if (ModName == "Core")
                return true;
            var line = t.FindLine("onmod");
            if (line == null)
                return false;
            return line.Find(ModName.ToLower()) != null;
        }
        public static bool IsPassMOD(this Setting t, string ModName)
        {
            var line = t.FindLine("passmod");
            if (line == null)
                return false;
            return line.Find(ModName.ToLower()) != null;
        }
        public static bool IsMSGMOD(this Setting t, string ModName)
        {
            var line = t.FindorAddLine("msgmod");
            if (line.GetBool(ModName))
                return false;
            line.SetBool(ModName, true);
            return true;
        }
        public static void OnMod(this Setting t, string ModName)
        {
            if (string.IsNullOrWhiteSpace(ModName))
                return;
            t.FindorAddLine("onmod").AddorReplaceSub(new Sub(ModName.ToLower()));
        }
        public static void OnModRemove(this Setting t, string ModName)
        {
            t.FindorAddLine("onmod").Remove(ModName.ToLower());
        }
        public static void PassMod(this Setting t, string ModName)
        {
            t.FindorAddLine("passmod").AddorReplaceSub(new Sub(ModName.ToLower()));
        }
        public static void PassModRemove(this Setting t, string ModName)
        {
            t.FindorAddLine("passmod").Remove(ModName.ToLower());
        }
    }
}
