using System.Collections.Generic;
using System.Linq;
using AuroraMod.AuroraCode.Cards.Token;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// 宕机攻击减伤（设计文档 §2.4）：只要手牌中存在至少 1 张「宕机」，你的攻击造成的伤害降低 25%。
/// 多张宕机不叠加（动态检查"存在即减"）。对模块伤害无效（模块走 Unpowered，非 powered attack）。
/// 隐藏 Power：过热生成宕机时一并挂上，全场常驻，靠动态查手牌决定是否生效；手牌无宕机时返回 ×1。
/// </summary>
public sealed class AuroraSystemCrashPenaltyPower : AuroraPower
{
    public const decimal AttackPenaltyMultiplier = 0.75m;

    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;
    protected override bool IsVisibleInternal => false;

    public override decimal ModifyDamageMultiplicative(Creature target, decimal amount, ValueProp props, Creature dealer, CardModel cardSource)
    {
        if (dealer != Owner || amount <= 0 || !props.IsPoweredAttack() || !HandHasCrash())
        {
            return 1m;
        }

        return AttackPenaltyMultiplier;
    }

    private bool HandHasCrash()
    {
        var hand = Owner?.Player?.PlayerCombatState?.Hand?.Cards;
        return hand != null && hand.Any(c => c is AuroraSystemCrash);
    }
}
