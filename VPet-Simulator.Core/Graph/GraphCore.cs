using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VPet_Simulator.Core
{
    public class GraphCore
    {
        /// <summary>
        /// 动画类型
        /// </summary>
        public enum GraphType
        {
            /// <summary>
            /// 被提起动态 (循环)
            /// </summary>
            Raised_Dynamic,
            /// <summary>
            /// 被提起静态 (开始)
            /// </summary>
            Raised_Static_A_Start,
            /// <summary>
            /// 被提起静态 (循环)
            /// </summary>
            Raised_Static_B_Loop,
            /// <summary>
            /// 从上向右爬 (循环)
            /// </summary>
            Climb_Top_Right,
            /// <summary>
            /// 从上向左爬 (循环)
            /// </summary>
            Climb_Top_Left,
            /// <summary>
            /// 从右边爬 (循环)
            /// </summary>
            Climb_Right,
            /// <summary>
            /// 从左边爬 (循环)
            /// </summary>
            Climb_Left,
            /// <summary>
            /// 呼吸 (循环)
            /// </summary>
            Default,
            /// <summary>
            /// 摸头 (开始)
            /// </summary>
            Touch_Head_A_Start,
            /// <summary>
            /// 摸头 (循环)
            /// </summary>
            Touch_Head_B_Loop,
            /// <summary>
            /// 摸头 (结束)
            /// </summary>
            Touch_Head_C_End,
            /// <summary>
            /// 爬行向右 (循环)
            /// </summary>
            Crawl_Right,
            /// <summary>
            /// 爬行向左 (循环)
            /// </summary>
            Crawl_Left,
            /// <summary>
            /// 下蹲 (开始)
            /// </summary>
            Squat_A_Start,
            /// <summary>
            /// 下蹲 (循环)
            /// </summary>
            Squat_B_Loop,
            /// <summary>
            /// 下蹲 (结束)
            /// </summary>
            Squat_C_End,
            /// <summary>
            /// 下落 (开始/循环)
            /// </summary>
            Fall_A_Loop,
            /// <summary>
            /// 下落 (结束)
            /// </summary>
            Fall_B_End,
            /// <summary>
            /// 走路向右 (开始)
            /// </summary>
            Walk_Right_A_Start,
            /// <summary>
            /// 走路向右 (循环)
            /// </summary>
            Walk_Right_B_Loop,
            /// <summary>
            /// 走路向右 (结束)
            /// </summary>
            Walk_Right_C_End,
            /// <summary>
            /// 走路向左 (开始)
            /// </summary>
            Walk_Left_A_Start,
            /// <summary>
            /// 走路向左 (循环)
            /// </summary>
            Walk_Left_B_Loop,
            /// <summary>
            /// 走路向左 (结束)
            /// </summary>
            Walk_Left_C_End,
        }
        /// <summary>
        /// 动画类型默认设置 前文本|是否循环|是否常用
        /// </summary>
        public static readonly dynamic[][] GraphTypeValue = new dynamic[][]
        {
             new dynamic[]{ "raised_dynamic" ,false,true},
             new dynamic[]{ "raised_static_a", false,true},
             new dynamic[]{ "raised_static_b", false,true},
             new dynamic[]{ "climb_top_right", false,false},
             new dynamic[]{ "climb_top_left", false, false},
             new dynamic[]{ "climb_right", false, false},
             new dynamic[]{ "climb_left", false, false},
             new dynamic[]{ "default", true,true},
             new dynamic[]{ "touch_head_a", false,true},
             new dynamic[]{ "touch_head_b", false,true},
             new dynamic[]{ "touch_head_c", false,true},
             new dynamic[]{ "crawl_right", false, true},
             new dynamic[]{ "crawl_left", false, true},
             new dynamic[]{ "squat_a", false,true},
             new dynamic[]{ "squat_b", false, true},
             new dynamic[]{ "squat_c", false,true},
             new dynamic[]{ "fall_a", false, false},
             new dynamic[]{ "fall_b", false,true},
             new dynamic[]{ "walk_right_a", false,true},
             new dynamic[]{ "walk_right_b", false, true},
             new dynamic[]{ "walk_right_c", false,true},
             new dynamic[]{ "walk_left_a", false,true},
             new dynamic[]{ "walk_left_b", false, true},
             new dynamic[]{ "walk_left_c", false,true},
        };
        /// <summary>
        /// 图像字典
        /// </summary>
        public Dictionary<GraphType, List<IGraph>> Graphs = new Dictionary<GraphType, List<IGraph>>();

        /// <summary>
        /// 添加动画
        /// </summary>
        /// <param name="graph">动画</param>
        /// <param name="type">类型</param>
        public void AddGraph(IGraph graph, GraphType type)
        {
            if (!Graphs.ContainsKey(type))
            {
                Graphs.Add(type, new List<IGraph>());
            }
            Graphs[type].Add(graph);
        }
        public IGraph FindGraph(GraphType type, Save.ModeType mode)
        {
            if (Graphs.ContainsKey(type))
            {
                var list = Graphs[type].FindAll(x => x.ModeType == mode);
                if (list.Count > 0)
                {
                    return list[Function.Rnd.Next(list.Count)];
                }
                else
                {
                    return Graphs[type][Function.Rnd.Next(Graphs[type].Count)];
                }
            }
            return FindGraph(GraphType.Default, mode);
        }
    }
}
