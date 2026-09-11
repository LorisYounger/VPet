using System;
using System.Collections.Generic;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// 动画类型默认前文本
    /// </summary>
    /// 这段逻辑原本在 GraphHelper 里, 但 GraphHelper 依赖 WPF, 而 GraphInfo 的
    /// 路径解析要用到它, 所以拆到这里作为平台无关的共享源码.
    /// GraphHelper.GraphTypeValue 保留原样并转发到这里, 对 MOD 完全透明.
    internal static class GraphTypeNames
    {
        internal static string[][]? graphtypevalue;
        /// <summary>
        /// 动画类型默认前文本
        /// </summary>
        internal static string[][] GraphTypeValue
        {
            get
            {
                if (graphtypevalue == null)
                {
                    List<string[]> gtv = new List<string[]>();
                    foreach (string v in Enum.GetNames(typeof(GraphInfo.GraphType)))
                    {
                        gtv.Add(v.ToLowerInvariant().Split('_'));
                    }
                    graphtypevalue = gtv.ToArray();
                }
                return graphtypevalue;
            }
        }
    }
}
