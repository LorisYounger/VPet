using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace VPet_Simulator.Core.New
{
    public class AnimationController
    {
        public static readonly Lazy<AnimationController> _instance = new Lazy<AnimationController>(() => new AnimationController());

        public static AnimationController Instance
        {
            get { return _instance.Value; }
        }

        protected IGraphNew graph;

        private List<AnimationInfo> rawAnimations;
        private List<AnimationInfo> animations;

        private List<FrameInfo> framesCache;

        private List<string> existAnimations;
        //动画控制器，接收逻辑状态，输出动画状态
        AnimationController()
        {
            Initialize();
        }

        public void RegistryGraph(IGraphNew graph)
        {
            this.graph = graph;
        }

        //重新初始化控制器，目前数据暂时无法统一初始化
        public void Initialize()
        {
            Dispose();
            //TODO：此处应该读取mod中包含的动画定义数据，暂时先手写代替
            rawAnimations = new List<AnimationInfo>();
            animations = new List<AnimationInfo>();
            framesCache = new List<FrameInfo>();
        }

        public void AddAnimation(DirectoryInfo directoryInfo, string animationName)
        {
            if (directoryInfo.Exists && directoryInfo.GetFiles().Length > 0)
            {
                Console.WriteLine("Add Animation: " + animationName + " => " + directoryInfo.FullName);
                AnimationInfo animation = new AnimationInfo(animationName, "", new Uri(directoryInfo.FullName));
                rawAnimations.Add(animation);

                animation.framesLoop.ForEach(frame => { framesCache.Add(frame); });
            }
        }
        //==============================
        //规则部分，使用现有拼接结构
        //模式分为3种
        static readonly string[] existMode = { @"_Nomal(?=_|$)", "_Happy(?=_|$)", "_Ill(?=_|$)" };
        //Start Loop End分别用_A _B _C标识
        static readonly string[] existSegment = { "_A(?=_|$)", "_B(?=_|$)", "_C(?=_|$)" };
        //随机：总是在最后，以下划线+数字的形式存在

        public void ArrangeAnimation()
        {
            //收集根动画信息，仅接受name和mode的差分
            Dictionary<string, bool> tags = new Dictionary<string, bool>();
            foreach (var a in rawAnimations)
            {
                string name = a.name;
                foreach (var s in existMode)
                {
                    name = Regex.Replace(name, s, "", RegexOptions.IgnoreCase);
                }
                foreach (var s in existSegment)
                {
                    name = Regex.Replace(name, s, "", RegexOptions.IgnoreCase);
                }
                name = Regex.Replace(name, @"_\d+(?=_|$)", "");
                tags[name] = true;
            }
            existAnimations = new List<string>();
            foreach (var kv in tags)
            {
                existAnimations.Add(kv.Key);
                Console.WriteLine(kv.Key);
            }
        }

        public void PlayAnimation(string name, string mode, int loopTimes = 0, bool forceExit = false)
        {

        }

        public void PlayRawFrame(string frameName)
        {
            //Console.WriteLine("Order Render Frame: " + frameName);
            var f = framesCache.Where(x => x.uri.OriginalString == frameName).FirstOrDefault();
            if (f != null)
            {
                //Console.WriteLine("Hit Cache");
                graph.Order(f.stream);
            }
        }

        public void Dispose()
        {
            rawAnimations?.ForEach(x => x.Dispose());
            rawAnimations?.Clear();
            rawAnimations = null;

            animations?.ForEach(x => x.Dispose());
            animations?.Clear();
            animations = null;

            framesCache?.Clear();
            framesCache = null;
        }

        public void DrawSampleFrame()
        {
            if (graph == null)
            {
                throw new ArgumentNullException("需要先使用RegistryGraph注册画布");
            }

            var ts = new ThreadStart(testDrawThread);
            var t = new Thread(testDrawThread);
            t.Start();
        }

        public void testDrawThread()
        {
            int i = 0;
            Random rnd = new Random();
            while (true)
            {
                Thread.Sleep(100);
                var a = rawAnimations[rnd.Next(rawAnimations.Count - 1)];
                var f = a.framesLoop[i++ % a.framesLoop.Count];
                //Console.WriteLine("DrawNext: " + f.stream.Length);
                graph.Order(f.stream);
            }
        }
    }
}
