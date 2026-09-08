using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// 剑势：战斗内无上限，不随回合减少。
/// 每3点使攻击每段伤害+1，最多+7；21点达到基础增伤上限。
/// 加算在倍率之前，模块等Unpowered伤害不受影响；不提供力量层数。
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

    /// <summary>每3点剑势提供1点攻击增伤。</summary>
    public const int DamagePerStacks = 3;

    /// <summary>被动伤害加成上限（剑势计数本身不受此限）。</summary>
    public const int MaxDamageBonus = 7;

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
