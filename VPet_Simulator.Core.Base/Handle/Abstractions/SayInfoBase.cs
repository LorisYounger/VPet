using System;
using System.Text;
using System.Threading.Tasks;

namespace VPet_Simulator.Core
{
    public abstract class SayInfoBase
    {
        /// <summary>
        /// 图像名
        /// </summary>
        public string? GraphName;
        /// <summary>
        /// 说话的描述
        /// </summary>
        public string? Desc;
        /// <summary>
        /// 消息内容
        /// </summary>
        public object? MsgContent;
        /// <summary>
        /// 是否强制显示图像
        /// </summary>
        public bool Force;
        /// <summary>
        /// 是否已经播放了语音
        /// </summary>
        public bool IsGenVoice;

        /// <summary>
        /// 获得说话内容 (若是流式传输则会等待完成)
        /// </summary>
        public abstract Task<string> GetSayText();
    }

    public abstract class SayInfoWithOutStreamBase : SayInfoBase
    {
        protected SayInfoWithOutStreamBase(string text = "", string? graphname = null, object? msgcontent = null, bool force = false, string? desc = null)
        {
            Text = text;
            GraphName = graphname;
            MsgContent = msgcontent;
            Force = force;
            Desc = desc;
        }

        /// <summary>
        /// 说话内容
        /// </summary>
        public string Text;

        public override Task<string> GetSayText()
        {
            return Task.FromResult(Text);
        }
    }

    public abstract class SayInfoWithStreamBase : SayInfoBase
    {
        protected SayInfoWithStreamBase(string? graphname = null, object? msgcontent = null, bool force = false, string? desc = null)
        {
            GraphName = graphname;
            MsgContent = msgcontent;
            Force = force;
            Desc = desc;
        }

        /// <summary>
        /// 说话内容更新事件
        /// </summary>
        public event Action<(string fullText, string changedText)>? Event_Update;
        /// <summary>
        /// 生成完成事件, string为生成完成的全部文本
        /// </summary>
        public event Action<string>? Event_Finish;
        /// <summary>
        /// 当前对话内容
        /// </summary>
        public StringBuilder CurrentText = new StringBuilder();
        /// <summary>
        /// 是否完成生成
        /// </summary>
        public bool IsFinishGen;

        public void UpdateAllText(string fullText)
        {
            CurrentText = new StringBuilder(fullText);
            Event_Update?.Invoke((fullText, fullText));
        }

        public void UpdateText(string text)
        {
            CurrentText.Append(text);
            Event_Update?.Invoke((CurrentText.ToString(), text));
        }

        public void FinishGenerate()
        {
            if (IsFinishGen)
            {
                return;
            }

            IsFinishGen = true;
            Event_Finish?.Invoke(CurrentText.ToString());
        }

        public override async Task<string> GetSayText()
        {
            while (!IsFinishGen)
            {
                await Task.Delay(10);
            }
            return CurrentText.ToString();
        }
    }
}
