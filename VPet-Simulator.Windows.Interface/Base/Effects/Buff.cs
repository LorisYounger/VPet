using LinePutScript;
using LinePutScript.Converter;
using LinePutScript.Dictionary;
using System;
using System.Collections.Generic;
using System.Linq;
using VPet_Simulator.Core;
#if VPET_AVALONIA
using Main = VPet_Simulator.Core.MutiPlatform.Display.Main;
#endif

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
            /// 工作收益倍率
            /// </summary>
            [Line(name: "ereward", ignoreCase: true)]
            public double Reward { get; init; } = 1;
            /// <summary>
            /// 体力获取倍率，默认 1；消耗使用 StrengthCost。
            /// </summary>
            [Line(name: "estrength", ignoreCase: true)]
            public double Strength { get; init; } = 1;
            [Line(name: "efood", ignoreCase: true)]
            public double Food { get; init; } = 1;
            [Line(name: "edrink", ignoreCase: true)]
            public double Drink { get; init; } = 1;
            [Line(name: "efeeling", ignoreCase: true)]
            public double Feeling { get; init; } = 1;
            /// <summary>
            /// 健康获取倍率，默认 1；消耗使用 HealthCost。
            /// </summary>
            [Line(name: "ehealth", ignoreCase: true)]
            public double Health { get; init; } = 1;
            /// <summary>
            /// 金钱收益倍率，默认 1。
            /// </summary>
            [Line(name: "emoney", ignoreCase: true)]
            public double Money { get; init; } = 1;
            /// <summary>
            /// 经验变化倍率，默认 1。
            /// </summary>
            [Line(name: "eexperience", ignoreCase: true)]
            public double Experience { get; init; } = 1;
            /// <summary>
            /// 好感度变化倍率，默认 1。
            /// </summary>
            [Line(name: "elikability", ignoreCase: true)]
            public double Likability { get; init; } = 1;

            /// <summary>
            /// 本体活动的体力消耗倍率，不影响食物的负属性。
            /// </summary>
            [Line(name: "estrengthcost", ignoreCase: true)]
            public double StrengthCost { get; init; } = 1;
            [Line(name: "efoodcost", ignoreCase: true)]
            public double FoodCost { get; init; } = 1;
            [Line(name: "edrinkcost", ignoreCase: true)]
            public double DrinkCost { get; init; } = 1;
            [Line(name: "efeelingcost", ignoreCase: true)]
            public double FeelingCost { get; init; } = 1;
            [Line(name: "ehealthcost", ignoreCase: true)]
            public double HealthCost { get; init; } = 1;

            /// <summary>
            /// 体力、饱腹度和口渴值共用基础上限的百分比调整量，0.1 为 +10%；倍率限制在 0.5～20。
            /// </summary>
            [Line(name: "estrengthmax", ignoreCase: true)]
            public double StrengthMax { get; init; }
            /// <summary>
            /// 心情基础上限的百分比调整量，0.1 为 +10%；倍率限制在 0.5～20。
            /// </summary>
            [Line(name: "efeelingmax", ignoreCase: true)]
            public double FeelingMax { get; init; }
            /// <summary>
            /// 没有可显示的内置数值效果时展示的文本；自定义效果由 MOD 自行实现，不影响显示分类。
            /// </summary>
            [Line(name: "eemptystring", ignoreCase: true)]
            public string EmptyString { get; init; } = "";

            private double realPrice = double.NaN;
            /// <summary>
            /// 每小时推荐价 (低于此价格75%视为超模)
            /// </summary>
            [Line(ignore: true)]
            public double RealPrice
            {
                get
                {
                    if (double.IsNaN(realPrice))
                    {
                        // Schedule 单独使用表示所有活动。
                        var activity = Activity == PetActivity.Schedule ? PetActivity.All : Activity & PetActivity.All;

                        // 动占比与每活动小时预算；物品预算独立于活动时间。
                        double idle = activity.HasFlag(PetActivity.Idle) ? 0.4 : 0;
                        double sleep = activity.HasFlag(PetActivity.Sleep) ? 0.2 : 0;
                        double study = activity.HasFlag(PetActivity.Study) ? 0.1 : 0;
                        double work = activity.HasFlag(PetActivity.Work) ? 0.2 : 0;
                        double play = activity.HasFlag(PetActivity.Play) ? 0.1 : 0;
                        double item = activity.HasFlag(PetActivity.TakeItem) ? 1 : 0;
                        double natural = idle + sleep + study + work + play;
                        double coverage = Math.Max(natural, item);
                        double positive = 0, negative = 0;
                        void Value(double value)
                        {
                            positive += Math.Max(value, 0);
                            negative += Math.Min(value, 0);
                        }

                        // 各结算项先分开正负价值，Reward 合入金钱和经验后再限幅。
                        Value((Math.Clamp(Strength, 0, MaxMultiplier) - 1) * 24 * (idle + sleep + item) / 15);
                        Value((Math.Clamp(Food, 0, MaxMultiplier) - 1) * (12 * sleep + 24 * item) / 6);
                        Value((Math.Clamp(Drink, 0, MaxMultiplier) - 1) * 24 * (sleep + item) / 9);
                        Value((Math.Clamp(Feeling, 0, MaxMultiplier) - 1) * 12 * (play + item) / 18);
                        Value((Math.Clamp(Health, 0, MaxMultiplier) - 1) * 2 * (natural + item));
                        Value((1 - Math.Clamp(StrengthCost, 0, MaxMultiplier)) * (14.4 * study + 21.6 * work + 9 * play) / 15);
                        Value((1 - Math.Clamp(FoodCost, 0, MaxMultiplier)) * (24 * idle + 16.8 * study + 29.4 * work + 8.4 * play) / 6);
                        Value((1 - Math.Clamp(DrinkCost, 0, MaxMultiplier)) * (24 * idle + 16.8 * study + 21 * work + 12.6 * play) / 9);
                        Value((1 - Math.Clamp(FeelingCost, 0, MaxMultiplier)) * (15 * idle + 30 * study + 15 * work) / 18);
                        Value((1 - Math.Clamp(HealthCost, 0, MaxMultiplier)) * 5 * idle);
                        Value((Math.Clamp(Money + Reward - 1, 0, MaxMultiplier) - 1) * 179.52 * work);
                        double experience = Math.Clamp(Experience + Reward - 1, 0, MaxMultiplier) - 1;
                        Value(experience * (1958.4 * study + 440.64 * play) / 9);
                        Value(experience * (24 * natural + 30 * item) / 9);
                        Value((Math.Clamp(Likability, 0, MaxMultiplier) - 1) * (3 * natural + 0.2 * item) * 10);
                        Value((Math.Clamp(1 + StrengthMax, 0, MaxMultiplier) - 1) * 104 * (1d / 15 + 1d / 6 + 1d / 9) * 0.15 * natural);
                        Value((Math.Clamp(1 + FeelingMax, 0, MaxMultiplier) - 1) * 102 / 18 * 0.15 * natural);

                        // 各属性分别计二次强度价后相加：普通1、健康0.2、容量0.1、金钱20、经验/Reward40。
                        static double Curve(double delta)
                        {
                            double x = Math.Max(delta, 0) / 0.1;
                            return (x + x * x) / 2;
                        }
                        double intensity = 40 * coverage * (Curve(Reward - 1) + Curve(Experience - 1))
                            + coverage * (Curve(Strength - 1) + Curve(Food - 1)
                                + Curve(Drink - 1) + Curve(Feeling - 1) + Curve(Likability - 1))
                            + 0.2 * coverage * Curve(Health - 1)
                            + 20 * work * Curve(Money - 1)
                            + 0.1 * natural * (3 * Curve(StrengthMax) + Curve(FeelingMax))
                            + natural * (Curve(1 - StrengthCost) + Curve(1 - FoodCost)
                                + Curve(1 - DrinkCost) + Curve(1 - FeelingCost))
                            + 0.2 * natural * Curve(1 - HealthCost);

                        double hourly = Math.Max(0.6 * positive, intensity);
                        double discount = 0.25 * -negative;
                        if (hourly > 0) discount = Math.Min(discount, 0.3 * hourly);
                        realPrice = (hourly - discount) * (Activity.HasFlag(PetActivity.Schedule) ? 0.5 : 1);
                        realPrice = Math.Floor(realPrice * 1.25 * 4) / 4;
                    }
                    return realPrice;
                }
            }
        }

        private const double MaxMultiplier = 20;
        private readonly IMainWindow mw;
        private PetActivity appliedActivity;
        /// <summary>
        /// 效果列表；请使用 Add/Update/Remove 方法修改，避免直接操作列表导致结算不正确。
        /// </summary>
        public List<Effect> Effects { get; }
        private bool isScheduleOn;
        /// <summary>
        /// 日程表是否正在生效
        /// </summary>
        public bool IsScheduleOn
        {
            get => isScheduleOn;
            set
            {
                if (isScheduleOn == value) return;
                isScheduleOn = value;
                RebuildModifiers();
            }
        }

        public Buff(IMainWindow mw)
        {
            this.mw = mw;
            Effects = LPSConvert.DeserializeObject<List<Effect>>(mw.GameSavesData.Data["buff"]) ?? [];
            Effects.RemoveAll(effect => effect.IsExpired);
            RebuildModifiers();
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
            if (Effects.RemoveAll(effect => effect.SourceKey == sourceKey) > 0)
                RebuildModifiers();
        }

        public bool Remove(Effect effect)
        {
            bool removed = Effects.Remove(effect);
            if (removed) RebuildModifiers();
            return removed;
        }

        /// <summary>刷新当前活动的倍率和上限。</summary>
        public void RebuildModifiers()
        {
            var activity = mw.Activity;
            appliedActivity = activity;
            mw.Main.AttributeModifier = GetAttributeMultipliers(activity);
            var save = mw.GameSavesData.GameSave;
            var limits = (Strength: 0d, Feeling: 0d);
            for (int i = 0; i < Effects.Count; i++)
            {
                var effect = Effects[i];
                if (effect.IsExpired || !IsActivityActive(effect.Activity, activity)) continue;
                limits.Strength += effect.StrengthMax;
                limits.Feeling += effect.FeelingMax;
            }
            // GameSave 接收百分比调整量，继续沿用其最低 0.5 点与超额属性规则。
            save.AttributeLimits = (
                Math.Clamp(1 + limits.Strength, 0.5, MaxMultiplier) - 1,
                Math.Clamp(1 + limits.Feeling, 0.5, MaxMultiplier) - 1);
        }

        /// <summary>读取指定活动下的倍率；获取与收益按增减量加算，Cost 连乘，汇总后限制在 0～20。配置方保证倍率有效，不推进时间。</summary>
        public AttributeModifiers GetAttributeMultipliers(PetActivity activity)
        {
            var result = new AttributeModifiers();
            for (int i = 0; i < Effects.Count; i++)
            {
                var effect = Effects[i];
                if (effect.IsExpired || !IsActivityActive(effect.Activity, activity)) continue;
                result.Strength += effect.Strength - 1;
                result.Food += effect.Food - 1;
                result.Drink += effect.Drink - 1;
                result.Feeling += effect.Feeling - 1;
                result.Health += effect.Health - 1;
                result.Money += (effect.Money - 1) + (effect.Reward - 1);
                result.Experience += (effect.Experience - 1) + (effect.Reward - 1);
                result.Likability += effect.Likability - 1;
                result.StrengthCost *= effect.StrengthCost;
                result.FoodCost *= effect.FoodCost;
                result.DrinkCost *= effect.DrinkCost;
                result.FeelingCost *= effect.FeelingCost;
                result.HealthCost *= effect.HealthCost;
            }

            // 全部贡献收集完成后再封顶，避免加入顺序影响增减抵消的结果。
            result.Strength = Math.Clamp(result.Strength, 0, MaxMultiplier);
            result.Food = Math.Clamp(result.Food, 0, MaxMultiplier);
            result.Drink = Math.Clamp(result.Drink, 0, MaxMultiplier);
            result.Feeling = Math.Clamp(result.Feeling, 0, MaxMultiplier);
            result.Health = Math.Clamp(result.Health, 0, MaxMultiplier);
            result.Money = Math.Clamp(result.Money, 0, MaxMultiplier);
            result.Experience = Math.Clamp(result.Experience, 0, MaxMultiplier);
            result.Likability = Math.Clamp(result.Likability, 0, MaxMultiplier);
            result.StrengthCost = Math.Clamp(result.StrengthCost, 0, MaxMultiplier);
            result.FoodCost = Math.Clamp(result.FoodCost, 0, MaxMultiplier);
            result.DrinkCost = Math.Clamp(result.DrinkCost, 0, MaxMultiplier);
            result.FeelingCost = Math.Clamp(result.FeelingCost, 0, MaxMultiplier);
            result.HealthCost = Math.Clamp(result.HealthCost, 0, MaxMultiplier);
            return result;
        }

        private void OnTime(Main value)
        {
            if (Effects.RemoveAll(effect => effect.IsExpired) > 0 || appliedActivity != mw.Activity)
                RebuildModifiers();
        }
        public void Save()
        {
            mw.GameSavesData.Data["buff"] = LPSConvert.GetObjectLine<Line_D>(Effects
                .Where(x => !x.IsExpired && x.EndTime != DateTime.MaxValue)
                .ToList(), "buff");
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
