using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// 剑势 / Momentum（B 蓄力一斩流派）—— 战斗内 Buff，无上限、不自然衰减。
/// 只有明确写「清空剑势」的效果才消耗；模块触发不算攻击牌，不破坏"本回合未攻击"条件。
///
/// 【被动底薪】：每 <see cref="DamagePerStacks"/> 点剑势使本人 powered 攻击 +1 伤害，
/// 该加成最多 <see cref="MaxDamageBonus"/>（= 20 势封顶）。<b>计数本身仍然无上限</b>——只封被动这一段，
/// 一刀两断/无月等兑现口的天花板完全不受影响。
///
/// 【为什么必须有底薪】原实现「本身无被动效果」使剑势成为纯存钱罐：在抽到稀有兑现口之前收益恒为 0，
/// 整局攒势可能完全作废，这是玩家反馈「剑势从来没用出来」的根因。加底薪后攒势即时有回报，
/// 且让「清空」第一次产生真实代价（清 20 势 = 换爆发但失去后续每刀 +5），
/// 使一刀两断（清空）与无月（不清空）成为真抉择。
///
/// 【实现】与原版 <c>StrengthPower</c> 同一钩子 <see cref="ModifyDamageAdditive"/>，故行为与力量完全一致：
/// 多段攻击<b>每段各加一次</b>、加在乘区之前（会被过载 ×1.25 再放大）。但它<b>不是力量层数</b>——
/// 不占 buff 栏、不被「减力量」类 Debuff 削减、不被读取力量的效果计入，无双重结算风险。
/// 仅 powered attack 生效：模块伤害走 Unpowered，天然不吃底薪（防模块×剑势双轴指数）。
/// </summary>
public sealed class MomentumPower : AuroraPower
{
    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override string IconName => "momentum";

    public int Momentum => (int)Amount;
    public override int DisplayAmount => Momentum;
    protected override bool IsVisibleInternal => Momentum > 0;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Momentum", 0m)];

    public static int Get(Creature creature) => (int)(creature?.GetPowerAmount<MomentumPower>() ?? 0);

    /// <summary>
    /// 每多少点剑势换 1 点被动伤害。
    /// 4 → 3。原值下要 20 点剑势才摸到 +5 封顶，而一场能攒到 20 的局并不多，
    /// 底薪实际长期停在 +1~+2，玩家感觉不到剑势存在。改 3 之后 15 点摸顶，
    /// 中段（6~12 势）的收益从 +1~+3 提到 +2~+4，攒势过程终于有可感知的爬升。
    /// <b>封顶 <see cref="MaxDamageBonus"/> 不变</b>，所以强度上限没动，动的只是到达速度。
    /// </summary>
    public const int DamagePerStacks = 3;

    /// <summary>被动伤害加成上限（剑势计数本身不受此限）。</summary>
    public const int MaxDamageBonus = 5;

    /// <summary>当前剑势对应的被动伤害加成（供悬停预览与卡牌文案读取）。</summary>
    public static int DamageBonusFor(Creature creature)
    {
        var momentum = Get(creature);
        if (momentum <= 0)
        {
            return 0;
        }

        return System.Math.Min(momentum / DamagePerStacks, MaxDamageBonus);
    }

    /// <summary>
    /// 与原版力量同一钩子：仅本人打出的 powered attack 生效，每段各加一次，加在乘区之前。
    /// 模块伤害为 Unpowered，天然不吃（防双轴指数）。
    /// </summary>
#if STS2_BETA
    // beta v0.111.0：该钩子上移到 AbstractModel，并在末尾新增 CardPlay?。方法体两分支完全一致。
    public override decimal ModifyDamageAdditive(Creature target, decimal amount, ValueProp props, Creature dealer, CardModel cardSource, MegaCrit.Sts2.Core.Entities.Cards.CardPlay cardPlay)
#else
    public override decimal ModifyDamageAdditive(Creature target, decimal amount, ValueProp props, Creature dealer, CardModel cardSource)
#endif
    {
        if (Owner != dealer || !props.IsPoweredAttack())
        {
            return 0m;
        }

        return DamageBonusFor(Owner);
    }
}
