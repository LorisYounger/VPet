using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace VPet_Simulator.Core.New
{
    public enum AnimationType
    {
        NONE,           //未定义
        SIMPLE_LOOP,    //整个序列循环播放，循环播放和是否播放时循环没有关系
        COMPLEX_LOOP,   //有开头段和结尾段，开头->循环主体->结尾
    }


    //一个完整的动画片段，包括循环体，开头和结尾
    //主体保存配置，和原始内存，由于原始文件大小不会太大，配置部分就不做内存池了
    public class AnimationInfo
    {
        //动画标识名，比如idle，walk，sleep
        public string name;
        //动画状态名，比如happy，normal，ill
        public string mode;

        public AnimationType type;

        public List<FrameInfo> framesStart;
        public List<FrameInfo> framesLoop;
        public List<FrameInfo> framesEnd;

        public Vector animationShift;

        public AnimationInfo(string name, string mode, Uri path, AnimationType type = AnimationType.SIMPLE_LOOP)
        {
            this.name = name;
            this.mode = mode;
            this.type = type;
            if(type == AnimationType.SIMPLE_LOOP)
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(path.AbsolutePath);
                framesLoop = new List<FrameInfo>();

                var files = directoryInfo.GetFiles();
                for (int i = 0; i < files.Length; i++)
                {
                    FileInfo file = files[i];
                    int frameTime = int.Parse(file.Name.Split('.').Reverse().ToArray()[1].Split('_').Last());
                    FrameInfo frame = new FrameInfo(i, frameTime, i == files.Length - 1, new Uri(file.FullName));
                    framesLoop.Add(frame);
                }
                animationShift = framesLoop.Select(x => x.frameShift).Aggregate((a, b) => Vector.Add(a, b));
            }else if(type == AnimationType.COMPLEX_LOOP)
            {

            }

        }

        public void Dispose()
        {
            DisposeFrameInfoList(ref framesStart);
            DisposeFrameInfoList(ref framesLoop);
            DisposeFrameInfoList(ref framesEnd);
        }

        private void DisposeFrameInfoList(ref List<FrameInfo> frames)
        {
            frames?.ForEach(frame => frame.Dispose());
            frames?.Clear();
            frames = null;
        }
    }

    public class FrameInfo
    {
        public Uri uri;                     //本帧的原始URI，用于无法找到缓存需要硬加载的情况（未实现）
        public Stream stream;               //已注册的流，建议用内存流

        public int index;                   //本帧序号，从0开始
        public bool isLastFrame;            //是否是结尾帧（表达单次播放时，此动画是否已结束用）
        public int frameTime;               //帧停留时间，单位为毫秒

        public Vector frameShift;           //帧位移，用于移动等需要进行位移的动画，代表播放本帧时应该把容器位移多少

        public bool isReady;                //流是否已准备好，用于异步初始化

        //创建时即缓存内存，后继如果需要读条，则需要改造成支持异步
        public FrameInfo(int index, int frameTime, bool isLastFrame, Uri uri, Vector frameShift = new Vector())
        {
            this.index = index;
            this.frameTime = frameTime;
            this.isLastFrame = isLastFrame;
            this.uri = uri;
            this.frameShift = frameShift;
            LoadToMemory();
        }

        private void LoadToMemory()
        {
            //只允许载入本地资源，暂时不支持载入网络资源
            stream = new MemoryStream(File.ReadAllBytes(uri.OriginalString));
            isReady = true;
        }

        public void Dispose()
        {
            stream?.Dispose();
        }
    }

}
