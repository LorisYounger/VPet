using System;
using System.IO;
using System.Security.Cryptography;

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
                AnimationReNameFolder(time, directoryInfo);
            }
        }

        static void AnimationReNameFolder(int time, DirectoryInfo directoryInfo)
        {
            if (directoryInfo.GetFiles("*.png").Length != 0)
                AnimationReName(time, directoryInfo);
            else
                foreach (var fs in directoryInfo.GetDirectories())
                {
                    AnimationReNameFolder(time, fs);
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
