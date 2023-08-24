using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Xml.Linq;
using VPet_Simulator.Core;
using static VPet_Simulator.Core.GraphCore;

namespace VPet_Simulator.Tool
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("VPet Simulator Tool");
        start:
            Console.WriteLine("请输入需要使用的功能编号");
            Console.WriteLine("1. 精简动画相同图片");
            switch (Console.ReadLine())
            {
                case "1":
                    Animation();
                    break;
                //case "2":
                //    FontPetNew();
                //    break;
                default:
                    Console.WriteLine("暂无该功能");
                    goto start;
            }
        }
        /// <summary>
        /// 将图片重命名并转换成 名字_序号_持续时间 格式
        /// </summary>
        static void Animation()
        {
            Console.WriteLine("请输入每张图片的持续时间 (单位: 毫秒)");
            string timestr = Console.ReadLine();
            if (!int.TryParse(timestr, out int time))
            {
                time = 125;
            }
            while (true)
            {
                Console.WriteLine("请输入图片位置");
                DirectoryInfo directoryInfo = new DirectoryInfo(Console.ReadLine());
                if (directoryInfo.GetFiles().Length != 0)
                    AnimationReName(time, directoryInfo);
                else
                    foreach (var fs in directoryInfo.GetDirectories())
                    {
                        AnimationReName(time, fs);
                    }
            }
        }
        static void AnimationReName(int time, DirectoryInfo directoryInfo)
        {
            int id = 0;
            int rpt = 1;
            string hash = null;
            FileInfo lastf = null;
            foreach (FileInfo fileInfo in directoryInfo.GetFiles())
            {
                if (lastf == null)
                {
                    lastf = fileInfo;
                    hash = GetFileHash(fileInfo);
                    continue;
                }
                string filehash = GetFileHash(fileInfo);
                if (hash.Equals(filehash))
                {
                    //这个文件和上一个文件的hash值相同，这个上个文件
                    lastf.Delete();
                    lastf = fileInfo;
                    rpt++;
                    continue;
                }
                hash = filehash;
                lastf.MoveTo(Path.Combine(directoryInfo.FullName, $"{GetFileName(lastf)}_{id++:D3}_{rpt * time}.png"));
                rpt = 1;
                lastf = fileInfo;
            }
            lastf.MoveTo(Path.Combine(directoryInfo.FullName, $"{GetFileName(lastf)}_{id++:D3}_{rpt * time}.png"));
            Console.WriteLine("图片处理已完成");
        }
        //static void FontPetNew()
        //{
        //    Console.WriteLine("请输入储存位置");
        //    DirectoryInfo directoryInfo = new DirectoryInfo(Console.ReadLine());

        //    var elist = Properties.Resources.laenum.Replace("            ", "").Replace("/// <summary>", "")
        //        .Replace("/// </summary>", "").Replace("/// ", "").Replace("\r", "").Replace("\n\n", "\n")
        //        .Replace("\n\n", "\n").Replace("\n\n", "\n").Split('\n').ToList();
        //    elist.RemoveAll(x => x.EndsWith(","));
        //    for (int i = 0; i < elist.Count; i++)
        //    {
        //        var paths = GraphTypeValue[i].Split('_');
        //        DirectoryInfo nowpath = directoryInfo;
        //        foreach (var path in paths)
        //        {
        //            nowpath = nowpath.CreateSubdirectory(path);
        //        }
        //        foreach (string v in Enum.GetNames(typeof(GameSave.ModeType)))
        //        {
        //            using (Bitmap image = new Bitmap(500, 500))
        //            {
        //                using (Graphics g = Graphics.FromImage(image))
        //                {
        //                    var strs = elist[i].Split(' ');
        //                    g.DrawString(strs[0], new Font("胡晓波男神体2.0", 66, FontStyle.Bold), new SolidBrush(Color.DarkSlateBlue), 10, 100);
        //                    g.DrawString(strs[0], new Font("胡晓波男神体2.0", 64), new SolidBrush(Color.AliceBlue), 15, 100);
        //                    for (int j = 1; j < strs.Length - 1; j++)
        //                    {
        //                        g.DrawString(strs[j], new Font("胡晓波萌萌体", 50, FontStyle.Bold), new SolidBrush(Color.LightGray), 10, 150 + 50 * j);
        //                        g.DrawString(strs[j], new Font("胡晓波萌萌体", 48, FontStyle.Bold), new SolidBrush(Color.Gray), 15, 150 + 50 * j);
        //                    }
        //                    g.DrawString(v, new Font("胡晓波润圆体35", 50, FontStyle.Bold), new SolidBrush(Color.DeepSkyBlue), 10, 350);
        //                    g.DrawString(v, new Font("胡晓波润圆体35", 48, FontStyle.Bold), new SolidBrush(Color.SkyBlue), 15, 350);
        //                    int len = 2000;
        //                    var last = strs.Last();
        //                    if(last == "S")
        //                    {
        //                        len = 250;
        //                    }
        //                    else if (last == "M")
        //                    {
        //                        len = 1000;
        //                    }
        //                    image.Save(nowpath.CreateSubdirectory(v).FullName + $"\\{paths[0]}_{len}.png");
        //                }
        //            }
        //        }

        //    }
        //}
        public static string GetFileHash(FileInfo fileInfo)
        {
            //创建一个哈希算法对象
            using (HashAlgorithm hash = HashAlgorithm.Create())
            {
                using (FileStream file = fileInfo.OpenRead())
                {
                    //哈希算法根据文本得到哈希码的字节数组                
                    return BitConverter.ToString(hash.ComputeHash(file));
                }
            }
        }
        public static string GetFileName(FileInfo fileInfo)
        {
            var strs = fileInfo.Name.Split('_', '-');
            if (strs.Length == 1)
            {
                strs = fileInfo.Name.Replace("00", "_").Split('_');
            }
            if (strs.Length == 1)
            {
                return fileInfo.Directory.Name;
            }
            return strs[0];
        }
    }

}
