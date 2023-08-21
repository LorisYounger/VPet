using LinePutScript.Converter;
using LinePutScript.Localization.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VPet_Simulator.Windows.Interface
{
    /// <summary>
    /// 供玩家选择说话的文本
    /// </summary>
    public class SelectText : ICheckText
    {
        /// <summary>
        /// 玩家选项名称
        /// </summary>
        [Line(IgnoreCase = true)]
        public string Choose { get; set; } = null;

        private string transChoose = null;
        /// <summary>
        /// 玩家选项名称 (翻译)
        /// </summary>
        public string TranslateChoose
        {
            get
            {
                if (transChoose == null)
                {
                    transChoose = LocalizeCore.Translate(Text);
                }
                return transChoose;
            }
            set
            {
                transChoose = value;
            }
        }
        /// <summary>
        /// 标签
        /// </summary>
        [Line(IgnoreCase = true)]
        public List<string> Tags { get; set; } = new List<string>();
        /// <summary>
        /// 跳转到标签
        /// </summary>
        public List<string> ToTags { get; set; } = new List<string>();
    }
}
