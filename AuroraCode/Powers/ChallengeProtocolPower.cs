using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// 挑战协议 / Challenge Protocol（B 挑战协议流派，架构 §8.2）—— 敌方正面 Buff。
///
/// 走 <see cref="PowerType.Buff"/> 施加通道，因此<b>不被人工制品阻挡</b>；
/// <see cref="PowerInstanceType.InstancedPerApplier"/>：引擎按施加者(Applier)各建一实例，
/// 同一奥萝拉后续施加叠到自己实例，多名奥萝拉互相独立。悬停由引擎按 Applier 显示归属+层数。
///
/// 每层只使「该敌人对<b>本施加者本人</b>造成的 powered attack」伤害 +10%（1/2/3 层 = ×1.10/1.20/1.30）。
/// 群体攻击只提高施加者本人承受的那一份；伤害被重定向给队友时增伤不跟随（以最终承受者 target 判定）。
/// 施加 / 消费 / 查询统一走 <see cref="Helpers.AuroraChallengeProtocolService"/>，卡牌不直接改层数。
/// </summary>
public sealed class ChallengeProtocolPower : AuroraPower
{
    public const int MaxStacksPerApplier = 3;
    public const decimal PerStackBonus = 0.10m;

    public override PowerInstanceType InstanceType => PowerInstanceType.InstancedPerApplier;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override string IconName => "challenge_protocol";

    public int Stacks => (int)Amount;
    public override int DisplayAmount => Stacks;
    protected override bool IsVisibleInternal => Stacks > 0;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Stacks", 0m)];

    /// <summary>
    /// 敌人(Owner)对本实例施加者(Applier)造成 powered attack 时，按层数放大：×(1 + 0.10×层数)。
    /// 乘法阶段生效（§8.2 公式）；非本施加者承受、Unpowered、非攻击一律 ×1。
    /// </summary>
#if STS2_BETA
    // beta v0.111.0：该钩子上移到 AbstractModel，并在末尾新增 CardPlay?。方法体两分支完全一致。
    public override decimal ModifyDamageMultiplicative(Creature target, decimal amount, ValueProp props, Creature dealer, CardModel cardSource, MegaCrit.Sts2.Core.Entities.Cards.CardPlay cardPlay)
#else
    public override decimal ModifyDamageMultiplicative(Creature target, decimal amount, ValueProp props, Creature dealer, CardModel cardSource)
#endif
    {
        // dealer==Owner：确实是携带本 Buff 的敌人在打；target==Applier：只提高施加者本人承受的那份
        //（重定向给队友时 target≠Applier → ×1，增伤不跟随）。
        if (dealer != Owner || target == null || target != Applier || amount <= 0 || !props.IsPoweredAttack())
        {
            return 1m;
        }

        return 1m + PerStackBonus * Stacks;
    }
}
