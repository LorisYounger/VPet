using LinePutScript.Converter;
using System;
using System.Collections.Generic;
using System.Linq;
using VPet_Simulator.Core;
using static VPet_Simulator.Core.GraphHelper;
using AttributeModifiers = VPet_Simulator.Core.Main.AttributeModifiers;

namespace VPet_Simulator.Windows.Interface
{
    /// <summary>
    /// 特殊效果管理器
    /// </summary>
    public class Buff : IDisposable
    {
        /// <summary>
        /// 单个效果 (BUFF)
        /// </summary>
        public class Effect
        {
            /// <summary>
            /// 到期时间（本地时间），DateTime.MaxValue 为永久效果；暂停和休眠不延长效果。
            /// </summary>
            [Line(name: "eendtime")]
            public DateTime EndTime { get; set; } = DateTime.MaxValue;
            /// <summary>
            /// 由来源方提供的稳定标识，用于更新或移除同一来源的效果
            /// </summary>
            [Line(name: "esourcekey")]
            public string SourceKey { get; init; } = "";
            /// <summary>是否过期</summary>
            public bool IsExpired => EndTime <= DateTime.Now;
            /// <summary>
            /// 是否仅适用于工作、学习或娱乐，包含日程限定和组合范围
            /// </summary>
            public bool IsWorkActivity => (Activity & PetActivity.All) != PetActivity.None
                && ((Activity & PetActivity.All) & ~(PetActivity.Work | PetActivity.Study | PetActivity.Play)) == PetActivity.None;
            /// <summary>
            /// 描述
            /// </summary>
            [Line(name: "edescription", ignoreCase: true)]
            public string Description { get; init; } = "";
            /// <summary>
            /// 文本显示来源；稳定标识使用 SourceKey
            /// </summary>
            [Line(name: "esource", ignoreCase: true)]
            public string Source { get; init; } = "";
            /// <summary>
            /// 显示名称
            /// </summary>
            [Line(name: "ename", ignoreCase: true)]
            public string Name { get; init; } = "";
            /// <summary>
            /// Remix Icon 字符
            /// </summary>
            [Line(name: "eicon", ignoreCase: true)]
            public string Icon { get; init; } = "\uEC7A";
            /// <summary>
            /// 效果显示分类：true 为减益，false 为增益，null 为特殊效果。
            /// </summary>
            [Line(converter: typeof(DebuffConverter), name: "edebuff", ignoreCase: true)]
            public bool? IsDebuff { get; init; }

            // LinePutScript 不能直接反序列化 bool?，保留 edebuff 原有的布尔文本格式。
            private class DebuffConverter : LPSConvert.ConvertFunction
            {
                public DebuffConverter() { }
                public override string Convert(dynamic value) => value.ToString();
                public override dynamic ConvertBack(string info) => bool.Parse(info);
            }
            /// <summary>
            /// 限定条件/范围
            /// </summary>
            [Line(ignore: true)]
            public PetActivity Activity { get; init; } = PetActivity.All;
            /// <summary>
            /// 限定条件/范围
            /// </summary>
            [Line(name: "eactivity")]
            public int ActivityMask { get => (int)Activity; init => Activity = (PetActivity)value; }
            /// <summary>
            /// 工作金钱或学习、娱乐经验的收益倍率 (总)
            /// </summary>
            [Line(name: "ereward", ignoreCase: true)]
            public double Reward { get; init; } = 1;
            /// <summary>体力变化倍率，默认 1；工作为消耗倍率，睡眠、空闲和物品为相应变化倍率。</summary>
            [Line(name: "estrength", ignoreCase: true)]
            public double Strength { get; init; } = 1;
            [Line(name: "efood", ignoreCase: true)]
            public double Food { get; init; } = 1;
            [Line(name: "edrink", ignoreCase: true)]
            public double Drink { get; init; } = 1;
            [Line(name: "efeeling", ignoreCase: true)]
            public double Feeling { get; init; } = 1;
            /// <summary>健康变化倍率，默认 1。</summary>
            [Line(name: "ehealth", ignoreCase: true)]
            public double Health { get; init; } = 1;
            /// <summary>金钱收益倍率，默认 1。</summary>
            [Line(name: "emoney", ignoreCase: true)]
            public double Money { get; init; } = 1;
            /// <summary>经验变化倍率，默认 1。</summary>
            [Line(name: "eexperience", ignoreCase: true)]
            public double Experience { get; init; } = 1;
            /// <summary>好感度变化倍率，默认 1。</summary>
            [Line(name: "elikability", ignoreCase: true)]
            public double Likability { get; init; } = 1;

            /// <summary>体力、饱腹度和口渴值共用基础上限的百分比调整量，0.1 为增加 10%，-0.1 为减少 10%；多个效果相加。</summary>
            [Line(name: "estrengthmax", ignoreCase: true)]
            public double StrengthMax { get; init; }
            /// <summary>心情基础上限的百分比调整量，0.1 为增加 10%，-0.1 为减少 10%；多个效果相加。</summary>
            [Line(name: "efeelingmax", ignoreCase: true)]
            public double FeelingMax { get; init; }
            /// <summary>没有可显示的内置数值效果时展示的文本；自定义效果由 MOD 自行实现，不影响显示分类。</summary>
            [Line(name: "eemptystring", ignoreCase: true)]
            public string EmptyString { get; init; } = "";
        }

        private readonly IMainWindow mw;
        /// <summary>
        /// 效果列表；请使用 Add/Update/Remove 方法修改，避免直接操作列表导致结算不正确。
        /// </summary>
        public List<Effect> Effects { get; } = [];
        private bool isScheduleOn;
        /// <summary>
        /// 日程表是否正在生效
        /// </summary>
        public bool IsScheduleOn
        {
            get => isScheduleOn;
            set
            {
                isScheduleOn = value;
                RebuildModifiers();
            }
        }

        public Buff(IMainWindow mw)
        {
            this.mw = mw;
            mw.Main.TimeHandle += OnTime;
        }

        /// <summary>
        /// 查看两个效果是否匹配活动和日程状态
        /// </summary>
        public bool IsActivityActive(PetActivity effect, PetActivity activity) =>
            ((effect & PetActivity.Schedule) == 0 || IsScheduleOn)
            && ((activity) & PetActivity.All) != PetActivity.None
            && (effect == PetActivity.Schedule || (effect & (activity) & PetActivity.All) != 0);

        /// <summary>
        /// 查看效果是否匹配当前桌宠的活动和日程状态
        /// </summary>
        public bool IsActivityActive(PetActivity effect) =>
            ((effect & PetActivity.Schedule) == 0 || IsScheduleOn)
            && ((mw.Activity) & PetActivity.All) != PetActivity.None
            && (effect == PetActivity.Schedule || (effect & (mw.Activity) & PetActivity.All) != 0);

        /// <summary>
        /// 添加效果
        /// </summary>
        public Effect Add(Effect effect)
        {
            Effects.Add(effect);
            RebuildModifiers();
            return effect;
        }

        /// <summary>
        /// 按 SourceKey 替换对应来源的全部效果
        /// </summary>
        public void Update(List<Effect> values)
        {
            var keys = values.Select(effect => effect.SourceKey).ToHashSet();
            Effects.RemoveAll(effect => keys.Contains(effect.SourceKey));
            Effects.AddRange(values);
            RebuildModifiers();
        }
        /// <summary>
        /// 删除指定 SourceKey 的效果
        /// </summary>
        public void Remove(string sourceKey)
        {
            Effects.RemoveAll(effect => effect.SourceKey == sourceKey);
            RebuildModifiers();
        }

        public bool Remove(Effect effect)
        {
            bool removed;
            removed = Effects.Remove(effect);
            RebuildModifiers();
            return removed;
        }

        /// <summary>刷新当前活动的倍率和上限。</summary>
        public void RebuildModifiers()
        {
            var activity = mw.Activity;
            mw.Main.AttributeModifier = GetAttributeMultipliers(activity);
            var save = mw.GameSavesData.GameSave;
            var limits = (Strength: 0d, Feeling: 0d);
            {
                for (int i = 0; i < Effects.Count; i++)
                {
                    var effect = Effects[i];
                    if (effect.IsExpired || !IsActivityActive(effect.Activity, activity)) continue;
                    limits.Strength += effect.StrengthMax;
                    limits.Feeling += effect.FeelingMax;
                }
            }
            save.AttributeLimits = limits;
        }

        /// <summary>读取指定活动下的属性变化倍率；多个效果相乘，不推进时间。</summary>
        public AttributeModifiers GetAttributeMultipliers(PetActivity activity)
        {
            var result = new AttributeModifiers();
            for (int i = 0; i < Effects.Count; i++)
            {
                var effect = Effects[i];
                if (effect.IsExpired || !IsActivityActive(effect.Activity, activity)) continue;
                result.Reward *= effect.Reward;
                result.Strength *= effect.Strength;
                result.Food *= effect.Food;
                result.Drink *= effect.Drink;
                result.Feeling *= effect.Feeling;
                result.Health *= effect.Health;
                result.Money *= effect.Money;
                result.Experience *= effect.Experience;
                result.Likability *= effect.Likability;
            }
            return result;
        }

        private void OnTime(Main value)
        {
            Effects.RemoveAll(effect => effect.IsExpired);
            RebuildModifiers();
        }

        /// <summary>取消定时刷新并清除效果、倍率和上限调整。</summary>
        public void Dispose()
        {
            mw.Main.TimeHandle -= OnTime;
            Effects.Clear();
            RebuildModifiers();
            mw.Main.AttributeModifier = null;
        }
    }
}
