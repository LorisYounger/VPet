//跨平台: 原文复制自 VPet-Simulator.Windows.Interface/Theme.cs; 本地化换命名空间, IFont 的 FontFamily 换成 Avalonia 的
using Avalonia.Media;
using VPet_Simulator.Core.MutiPlatform.Display;
using LinePutScript;
using LinePutScript.Localization;
using System.IO;

namespace VPet_Simulator.Windows.Interface
{
    /// <summary>
    /// 游戏主题
    /// </summary>
    public class Theme
    {
        private string? transname = null;
        /// <summary>
        /// 名字 (翻译)
        /// </summary>
        public string TranslateName
        {
            get
            {
                if (transname == null)
                {
                    transname = LocalizeCore.Translate(Name);
                }
                return transname;
            }
        }

        public string Name;
        public string xName;
        public string Image;
        public ImageResources Images;
        public LpsDocument ThemeColor;
        public Theme(LpsDocument lps)
        {
            xName = lps.First()!.Name;
            Name = lps.First()!.Info;
            Image = lps.First()!.Find("image")!.info;

            lps.RemoveAt(0);
            ThemeColor = lps;

            Images = new ImageResources();
        }
    }
    /// <summary>
    /// 字体
    /// </summary>
    public class IFont
    {
        /// <summary>
        /// 字体名字
        /// </summary>
        public string Name;
        private string? transname = null;
        /// <summary>
        /// 名字 (翻译)
        /// </summary>
        public string TranslateName
        {
            get
            {
                if (transname == null)
                {
                    transname = LocalizeCore.Translate(Name);
                }
                return transname;
            }
        }
        public string Path;
        public IFont(FileInfo path)
        {
            Name = path.Name.Substring(0, path.Name.Length - path.Extension.Length);
            //跨平台: Avalonia 不认 "目录\#字体名" 这种写法, ttf 要登记进字体集 (见 FontLoader), Path 记成登记后的写法
            FontLoader.Register(path);
            Path = FontLoader.CollectionKey + "#" + Name;
        }
        public FontFamily Font
        {
            get
            {
                return new FontFamily(Path);
            }
        }
    }
}
