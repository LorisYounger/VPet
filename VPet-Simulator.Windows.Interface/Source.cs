using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using LinePutScript;
using LinePutScript.Dictionary;

namespace VPet_Simulator.Windows.Interface
{
    /// <summary>
    /// 资源集
    /// </summary>
    public class Resources : LPS_D
    {
        public Resources() { }
        /// <summary>
        /// 添加资源,后来覆盖之前
        /// </summary>
        /// <param name="line">资源行</param>
        /// <param name="modpath">功能位置</param>
        public void AddSource(ILine line, string modpath)
        {
            ISub source = line.Find("source");
            if (source == null)
                return;
            //else if (!source.Info.Contains(":\\"))
            source.Info = modpath + '\\' + source.Info;
            line.Name = line.Name.ToLower();
            AddorReplaceLine(line);
        }
        /// <summary>
        /// 添加资源,后来覆盖之前
        /// </summary>
        /// <param name="line">资源行</param>
        public void AddSource(ILine line)
        {
            ISub source = line.Find("source");
            if (source == null)
                return;
            //else if (!source.Info.Contains(":\\"))
            line.Name = line.Name.ToLower();
            AddorReplaceLine(line);
        }
        /// <summary>
        /// 添加多个资源,后来覆盖之前
        /// </summary>
        /// <param name="lps">资源表</param>
        public void AddSources(ILPS lps)
        {
            foreach (ILine line in lps)
            {
                line.Name = line.Name.ToLower();
                AddSource(line);
            }
        }
        /// <summary>
        /// 添加多个资源,后来覆盖之前
        /// </summary>
        /// <param name="lps">资源表</param>
        /// <param name="modpath">功能位置</param>
        public void AddSources(ILPS lps, string modpath = "")
        {
            foreach (ILine line in lps)
            {
                AddSource(line, modpath);
            }
        }
        /// <summary>
        /// 添加资源,后来覆盖之前的
        /// </summary>
        /// <param name="name">资源名字</param>
        /// <param name="path">资源位置</param>
        public void AddSource(string name, string path)
        {
            AddorReplaceLine(new Line(name.ToLower(), "", "", new Sub("source", path)));
        }
        /// <summary>
        /// 查找资源
        /// </summary>
        /// <param name="name">资源名称</param>
        /// <param name="nofind">如果未找到,退回这个值</param>
        /// <returns>返回资源位置,如果未找到,则退回nofind</returns>
        public string FindSource(string name, string nofind = null)
        {
            ILine line = FindLine(name.ToLower());
            if (line == null)
                return nofind;
            return line.Find("source").Info;
        }
        /// <summary>
        /// 查找资源
        /// </summary>
        /// <param name="name">资源名称</param>
        /// <param name="nofind">如果未找到,退回这个值</param>
        /// <returns>返回资源位置,如果未找到,则退回nofind</returns>
        public Uri FindSourceUri(string name, string nofind = null)
        {
            ILine line = FindLine(name.ToLower());
            if (line == null)
                if (nofind != null)
                    return new Uri(nofind);
                else
                    return null;
            return new Uri(line.Find("source").Info);
        }
    }

    /// <summary>
    /// 图片资源集合
    /// </summary>
    public class ImageResources : Resources
    {
        public ImageResources()
        {

        }
        /// <summary>
        /// 添加图片集,后来覆盖之前
        /// </summary>
        /// <param name="lps">图片集</param>
        /// <param name="modpath">文件夹位置</param>
        public void AddImages(LpsDocument lps, string modpath = "") => AddSources(lps, modpath);
        /// <summary>
        /// 添加单个图片,后来覆盖之前
        /// </summary>
        /// <param name="line">图片行</param>
        /// <param name="modpath">文件夹位置</param>
        public void AddImage(ILine line, string modpath = "") => AddSource(line, modpath);
        /// <summary>
        /// 查找图片资源
        /// </summary>
        /// <param name="imagename">图片名称</param>
        /// <returns>图片资源,如果未找到则退回错误提示图片</returns>
        public BitmapImage FindImage(string imagename) => NewSafeBitmapImage(FindImageUri(imagename));

        public Uri FindImageUri(string imagename)
        {
#if DEBUGs
            var v = FindSourceUri(imagename, "pack://application:,,,/Res/Image/system/error.png");
            if (v.AbsoluteUri == "pack://application:,,,/Res/Image/system/error.png")
                throw new Exception($"image nofound {imagename}");
            return v;
#else
            return FindSourceUri(imagename, "pack://application:,,,/Res/img/error.png");
#endif
        }

        /// <summary>
        /// 查找图片资源 如果找不到则使用上级
        /// </summary>
        /// <param name="imagename">图片名称</param>
        /// <returns>图片资源,如果未找到则退回错误提示图片</returns>
        /// <param name="superior">上级图片 如果没有专属的图片,则提供上级的图片</param>
        public BitmapImage FindImage(string imagename, string superior)
        {
            string source = FindSource(imagename);
            if (source == null)
            {
                return NewSafeBitmapImage(FindImageUri(superior));
            }
            return NewSafeBitmapImage(source);
        }
        /// <summary>
        /// 图片设置 (eg:定位锚点等)
        /// </summary>
        public LpsDocument ImageSetting = new LpsDocument();
        /// <summary>
        /// 更加安全的图片URI加载
        /// </summary>
        /// <param name="source">图片源</param>
        /// <returns>BitmapImage</returns>
        public static BitmapImage NewSafeBitmapImage(string source) => NewSafeBitmapImage(new Uri(source));
        /// <summary>
        /// 更加安全的图片URI加载
        /// </summary>
        /// <param name="source">图片源</param>
        /// <returns>BitmapImage</returns>
        public static BitmapImage NewSafeBitmapImage(Uri source)
        {
            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            bi.UriSource = source;
            bi.EndInit();
            return bi;
        }
    }
}
