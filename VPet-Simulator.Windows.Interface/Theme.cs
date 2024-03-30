using LinePutScript;
using LinePutScript.Localization.WPF;
using System.IO;
using System.Windows.Media;

namespace VPet_Simulator.Windows.Interface
{
    /// <summary>
    /// 游戏主题
    /// </summary>
    public class Theme
    {
        private string transname = null;
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
            xName = lps.First().Name;
            Name = lps.First().Info;
            Image = lps.First().Find("image").info;

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
        private string transname = null;
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
            Path = path.Directory.FullName + @"\#" + Name;
        }
        public FontFamily Font
        {
            get
            {//file:///D:\Documents\Visual Studio 2022\Projects\VPet\VPet-Simulator.Windows\Res\#凤凰点阵体 12px
                return new FontFamily(@"file:///" + Path);
            }
        }
    }
}
