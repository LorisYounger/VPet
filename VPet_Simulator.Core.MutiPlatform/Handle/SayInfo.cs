using Avalonia.Controls;
using System;
using System.Text;
using System.Threading.Tasks;
using VPet_Simulator.Core;

namespace VPet_Simulator.Core.MutiPlatform;

public abstract class SayInfo : SayInfoBase
{
    public new Control? MsgContent
    {
        get => base.MsgContent as Control;
        set => base.MsgContent = value;
    }
}

public class SayInfoWithOutStream : SayInfo
{
    public SayInfoWithOutStream(string text, string? graphname = null, bool force = false, string? desc = null)
    {
        Text = text;
        GraphName = graphname;
        Force = force;
        Desc = desc;
    }

    public SayInfoWithOutStream(string text, Control msgcontent, string? graphname = null, bool force = false)
    {
        Text = text;
        GraphName = graphname;
        MsgContent = msgcontent;
        Force = force;
    }

    public SayInfoWithOutStream()
    {
        Text = string.Empty;
    }

    public string Text;

    public override Task<string> GetSayText() => Task.FromResult(Text);
}

public class SayInfoWithStream : SayInfo
{
    public SayInfoWithStream()
    {
    }

    public SayInfoWithStream(string graphname, bool force = false, string? desc = null)
    {
        GraphName = graphname;
        Force = force;
        Desc = desc;
    }

    public SayInfoWithStream(Control msgcontent, string? graphname = null, bool force = false)
    {
        GraphName = graphname;
        MsgContent = msgcontent;
        Force = force;
    }

    public event Action<(string fullText, string changedText)>? Event_Update;
    public event Action<string>? Event_Finish;
    public StringBuilder CurrentText = new();
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
            return;
        IsFinishGen = true;
        Event_Finish?.Invoke(CurrentText.ToString());
    }

    public async Task<SayInfoWithOutStream> ToNoneStream()
    {
        while (!IsFinishGen)
        {
            await Task.Delay(10);
        }

        return new SayInfoWithOutStream
        {
            GraphName = GraphName,
            Force = Force,
            Desc = Desc,
            MsgContent = MsgContent,
            Text = CurrentText.ToString()
        };
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
