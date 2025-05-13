using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// 增加父类 以便适应带有流式传输的说话
    /// </summary>
    public abstract class SayInfo
    {
        public SayInfo()
        {

        }


        /* --------- 消息信息 -----------*/
        /// <summary>
        /// 图像名
        /// </summary>
        public string GraphName;
        /// <summary>
        /// 说话的描述
        /// </summary>
        public string Desc;
        /// <summary>
        /// 消息内容
        /// </summary>
        public UIElement MsgContent;
        /// <summary>
        /// 是否强制显示图像
        /// </summary>
        public bool Force = true;
        /// <summary>
        /// 是否已经播放了语音
        /// </summary>
        public bool IsGenVoice = false;
    }
    /// <summary>
    /// 说话信息类 原本的SayInfo
    /// </summary>
    public class SayInfoWithOutStream : SayInfo
    {
        public SayInfoWithOutStream(String text)
        {
            Text = text;
        }
        public SayInfoWithOutStream() { }
        /// <summary>
        /// 说话内容
        /// </summary>
        public string Text;
    }
    /// <summary>
    /// 说话信息类 带有流式传输的SayInfo
    /// </summary>
    public class SayInfoWithStream : SayInfo
    {
        public SayInfoWithStream()
        {
            CurrentText = new StringBuilder();
            FinishGen = false;
        }
        /// <summary>
        /// 说话内容
        /// </summary>
        public event Action<string> Text;
        /// <summary>
        /// 全部说话内容
        /// </summary>
        public event Action<string> FullText;
        /// <summary>
        /// 生成完成事件, string为生成完成的全部文本
        /// </summary>
        public event Action<string> Finish;
        /// <summary>
        /// 当前对话内容
        /// </summary>
        public StringBuilder CurrentText;
        /// <summary>
        /// 是否完成生成
        /// </summary>
        public bool FinishGen = false;
        /// <summary>
        /// 给予sayRndFunction的提示词
        /// </summary>
        public string SayRndPrompt = "";

        /// <summary>
        /// 将当前对话内容更新为指定文本
        /// </summary>
        /// <param name="text">要替换的文本</param>
        public void UpdateAllText(string text)
        {
            CurrentText = new StringBuilder(text);
            Text?.Invoke(text);
            FullText?.Invoke(text);
        }

        /// <summary>
        /// 增加当前对话内容
        /// </summary>
        /// <param name="text">增加的内容</param>
        public void UpdateText(string text)
        {
            CurrentText.Append(text);
            Text?.Invoke(text);
            FullText?.Invoke(CurrentText.ToString());
        }

        /// <summary>
        /// 结束时调用
        /// </summary>
        public void FinishGenerate()
        {
            if (FinishGen)
                return;
            FinishGen = true;
            Finish?.Invoke(CurrentText.ToString());
        }
    }
}
