using LinePutScript;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static VPet_Simulator.Core.GraphHelper;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// 动画信息
    /// </summary>
    /// 新版本动画类型是根据整体类型+名字定义而成
    /// 动画类型->动画名字
    /// 动画名字->状态+动作->动画
    /// 类型: 主要动作分类
    /// 动画名字: 用户自定义, 同名字动画支持相同随机,不再使用StoreRand
    /// 动作: 动画的动作 Start Loop End
    /// 状态: 动画的状态 Save.GameSave.ModeType
    public class GraphInfo
    {
        /// <summary>
        /// 创建动画信息
        /// </summary>
        /// <param name="name">动画名字: 用户自定义 同名字动画支持相同随机,不再使用StoreRand</param>
        /// <param name="animat">动作: 动画的动作 Start Loop End</param>
        /// <param name="type">类型: 主要动作分类</param>
        /// <param name="modeType">状态: 4种状态</param>
        public GraphInfo(string name, GraphType type = GraphType.Common, AnimatType animat = AnimatType.Single, GameSave.ModeType modeType = GameSave.ModeType.Nomal)
        {
            Name = name;
            Animat = animat;
            Type = type;
            ModeType = modeType;
        }
        /// <summary>
        /// 通过文件位置和信息获取动画信息
        /// </summary>
        /// <param name="path">文件夹位置</param>
        /// <param name="info">信息</param>
        public GraphInfo(FileSystemInfo path, ILine info)
        {
            var pn = Sub.Split(path.FullName.Substring(0, path.FullName.Length - path.Extension.Length).ToLower(), info[(gstr)"startuppath"].ToLower()).Last();
            var path_name = pn.Replace('\\', '_').Split('_').ToList();
            path_name.RemoveAll(string.IsNullOrWhiteSpace);
            if (!Enum.TryParse(info[(gstr)"mode"], true, out GameSave.ModeType modetype))
            {
                if (path_name.Remove("happy"))
                {
                    modetype = GameSave.ModeType.Happy;
                }
                else if (path_name.Remove("nomal"))
                {
                    modetype = GameSave.ModeType.Nomal;
                }
                else if (path_name.Remove("poorcondition"))
                {
                    modetype = GameSave.ModeType.PoorCondition;
                }
                else if (path_name.Remove("ill"))
                {
                    modetype = GameSave.ModeType.Ill;
                }
                else
                {
                    modetype = GameSave.ModeType.Nomal;
                }
            }

            if (!Enum.TryParse(info[(gstr)"graph"], true, out GraphType graphtype))
            {
                graphtype = GraphInfo.GraphType.Common;
                for (int i = 0; i < GraphTypeValue.Length; i++)
                {//挨个找第一个匹配的
                    if (path_name.Contains(GraphTypeValue[i][0]))
                    {
                        int index = path_name.IndexOf(GraphTypeValue[i][0]);
                        bool ismatch = true;
                        for (int b = 1; b < GraphTypeValue[i].Length && b + index < path_name.Count; b++)
                        {
                            if (path_name[index + b] != GraphTypeValue[i][b])
                            {
                                ismatch = false;
                                break;
                            }
                        }
                        if (ismatch)
                        {
                            graphtype = (GraphType)i;
                            path_name.RemoveRange(index, GraphTypeValue[i].Length);
                            break;
                        }
                    }
                }
            }

            if (!Enum.TryParse(info[(gstr)"animat"], true, out AnimatType animatType))
            {
                if (path_name.Remove("a") || path_name.Remove("start"))
                {
                    animatType = AnimatType.A_Start;
                }
                else if (path_name.Remove("b") || path_name.Remove("loop"))
                {
                    animatType = AnimatType.B_Loop;
                }
                else if (path_name.Remove("c") || path_name.Remove("end"))
                {
                    animatType = AnimatType.C_End;
                }
                else
                {
                    animatType = AnimatType.Single;
                }
            }
            Name = info.Info;
            if (string.IsNullOrWhiteSpace(Name))
            {
                while (path_name.Count > 0 && (double.TryParse(path_name.Last(), out _) || path_name.Last().StartsWith("~")))
                {
                    path_name.RemoveAt(path_name.Count - 1);
                }
                if (path_name.Count > 0)
                    Name = path_name.Last();
            }
            if (string.IsNullOrWhiteSpace(Name))
            {
                Name = graphtype.ToString().ToLower();
            }
            Type = graphtype;
            Animat = animatType;
            ModeType = modetype;
        }
        /// <summary>
        /// 类型: 主要动作分类
        /// </summary>
        /// * 为必须有的动画
        public enum GraphType
        {
            /// <summary>
            /// 通用动画,用于被被其他动画调用或者mod等用途
            /// </summary>
            /// 不被默认启用/使用的 不包含在GrapType
            Common,
            /// <summary>
            /// 被提起动态 *
            /// </summary>
            Raised_Dynamic,
            /// <summary>
            /// 被提起静态 (开始&循环&结束) *
            /// </summary>
            Raised_Static,
            /// <summary>
            /// 现在所有会动的东西都是MOVE
            /// </summary>
            Move,
            /// <summary>
            /// 呼吸 *
            /// </summary>
            Default,
            /// <summary>
            /// 摸头 (开始&循环&结束)
            /// </summary>
            Touch_Head,
            /// <summary>
            /// 摸身体 (开始&循环&结束)
            /// </summary>
            Touch_Body,
            /// <summary>
            /// 空闲 (包括下蹲/无聊等通用空闲随机动画) (开始&循环&结束)
            /// </summary>
            Idel,
            /// <summary>
            /// 睡觉 (开始&循环&结束) *
            /// </summary>
            Sleep,
            /// <summary>
            /// 说话 (开始&循环&结束) *
            /// </summary>
            Say,
            /// <summary>
            /// 待机 模式1 (开始&循环&结束)
            /// </summary>
            StateONE,
            /// <summary>
            /// 待机 模式2 (开始&循环&结束)
            /// </summary>
            StateTWO,
            /// <summary>
            /// 开机 *
            /// </summary>
            StartUP,
            /// <summary>
            /// 关机
            /// </summary>
            Shutdown,
            /// <summary>
            /// 工作 (开始&循环&结束) *
            /// </summary>
            Work,
            /// <summary>
            /// 向上切换状态
            /// </summary>
            Switch_Up,
            /// <summary>
            /// 向下切换状态
            /// </summary>
            Switch_Down,
            /// <summary>
            /// 口渴
            /// </summary>
            Switch_Thirsty,
            /// <summary>
            /// 饥饿
            /// </summary>
            Switch_Hunger,
            /// <summary>
            /// 吃东西
            /// </summary>
            Eat,
            /// <summary>
            /// 喝东西
            /// </summary>
            Drink,
        }
        /// <summary>
        /// 动作: 动画的动作 Start Loop End
        /// </summary>
        public enum AnimatType
        {
            /// <summary>
            /// 动画只有一个动作
            /// </summary>
            Single,
            /// <summary>
            /// 开始动作
            /// </summary>
            A_Start,
            /// <summary>
            /// 循环动作
            /// </summary>
            B_Loop,
            /// <summary>
            /// 结束动作
            /// </summary>
            C_End,
        }
        /// <summary>
        /// 动画名字: 用户自定义 同名字动画支持相同随机,不再使用StoreRand
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 动作: 动画的动作 Start Loop End
        /// </summary>
        public AnimatType Animat { get; set; }
        /// <summary>
        /// 类型: 主要动作分类
        /// </summary>
        public GraphType Type { get; set; }
        /// <summary>
        /// 状态: 4种状态
        /// </summary>
        public GameSave.ModeType ModeType { get; set; }
        ///// <summary>
        ///// 其他附带的储存信息
        ///// </summary>
        //public ILine Info { get; set; }
    }
}
