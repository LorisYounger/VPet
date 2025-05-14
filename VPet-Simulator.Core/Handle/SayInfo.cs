using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static System.Net.Mime.MediaTypeNames;

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
        public bool Force = false;
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
        /// <summary>
        /// 说话信息
        /// </summary>
        /// <param name="text">说话内容</param>
        /// <param name="graphname">图像名</param>
        /// <param name="desc">描述</param>
        /// <param name="force">强制显示图像</param>
        public SayInfoWithOutStream(string text, string graphname = null, bool force = false, string desc = null)
        {
            Text = text;
            GraphName = graphname;
            Force = force;
            Desc = desc;
        }

        /// <summary>
        /// 说话信息类
        /// </summary>
        /// <param name="text">说话内容</param>
        /// <param name="graphname">图像名</param>
        /// <param name="msgcontent">消息内容</param>
        /// <param name="force">强制显示图像</param>
        public SayInfoWithOutStream(string text, UIElement msgcontent, string graphname = null, bool force = false)
        {
            Text = text;
            GraphName = graphname;
            MsgContent = msgcontent;
            Force = force;
        }
        /// <summary>
        /// 说话信息类
        /// </summary>
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
        /// <summary>
        /// 说话信息类
        /// </summary>
        public SayInfoWithStream()
        {
        }
        /// <summary>
        /// 说话信息类
        /// </summary>
        /// <param name="graphname">图像名</param>
        /// <param name="desc">描述</param>
        /// <param name="force">强制显示图像</param>
        public SayInfoWithStream(string graphname, bool force = false, string desc = null)
        {
            GraphName = graphname;
            Force = force;
            Desc = desc;
        }

        /// <summary>
        /// 说话信息类
        /// </summary>
        /// <param name="graphname">图像名</param>
        /// <param name="msgcontent">消息内容</param>
        /// <param name="force">强制显示图像</param>
        public SayInfoWithStream(UIElement msgcontent, string graphname = null, bool force = false)
        {
            GraphName = graphname;
            MsgContent = msgcontent;
            Force = force;
        }

        /// <summary>
        /// 说话内容更新事件
        /// </summary>
        public event Action<(string fullText, string changedText)> Event_Update;
        /// <summary>
        /// 生成完成事件, string为生成完成的全部文本
        /// </summary>
        public event Action<string> Event_Finish;
        /// <summary>
        /// 当前对话内容
        /// </summary>
        public StringBuilder CurrentText = new StringBuilder();
        /// <summary>
        /// 是否完成生成
        /// </summary>
        public bool FinishGen = false;
        /// <summary>
        /// 给予sayRndFunction的提示词
        /// </summary>
        public string SayRndPrompt = "";

        /// <summary>
        /// 将当前对话内容全部更新为指定文本
        /// </summary>
        /// <param name="fullText">要替换的文本</param>
        public void UpdateAllText(string fullText)
        {
            CurrentText = new StringBuilder(fullText);
            Event_Update?.Invoke((fullText, fullText));
        }

        /// <summary>
        /// 增加当前对话内容
        /// </summary>
        /// <param name="text">增加的内容</param>
        public void UpdateText(string text)
        {
            CurrentText.Append(text);
            Event_Update?.Invoke((CurrentText.ToString(), text));
        }

        /// <summary>
        /// 结束时调用
        /// </summary>
        public void FinishGenerate()
        {
            if (FinishGen)
                return;
            FinishGen = true;
            Event_Finish?.Invoke(CurrentText.ToString());
        }
    }
}
