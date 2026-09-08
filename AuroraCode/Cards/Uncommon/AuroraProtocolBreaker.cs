using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.Helpers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Cards.Uncommon;

/// <summary>
/// 协议破局（普通）：造成8/11伤害，随后移除存活目标上自己的1层挑战协议，成功移除则获得3剑势。
/// 击杀目标时不发放剑势；保留原类名与命名空间以兼容存档。
/// </summary>
public class AuroraProtocolBreaker() : AuroraCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override string ArtName => "protocol_breaker";

    protected override IEnumerable<AuroraMechanic> MechanicTips =>
        [AuroraMechanic.ChallengeProtocol, AuroraMechanic.Momentum];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8, ValueProp.Move),
        new DynamicVar("ProtocolConsumed", 1m),
        new DynamicVar("MomentumGain", 3m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        var target = cardPlay.Target;

        // 1. 单段 powered 攻击（协议只影响敌→你承伤、不影响你的输出；先打后消费只为把风险兑现为剑势）。
        var damage = (int)DynamicVars.Damage.BaseValue;
        await AuroraCardAttack.Create(this, cardPlay, target, damage, ValueProp.Move).Execute(choiceContext);

        // 2. 击杀则跳过（#38 明确：目标死亡不消费、不给剑势）。
        if (creature == null || target == null || !target.IsAlive)
        {
            return;
        }

        // 3. 只消费本人 1 层协议（返回实际消费 0/1）。
        var consumed = await AuroraChallengeProtocolService.ConsumeAsync(
            choiceContext, target, creature, (int)DynamicVars["ProtocolConsumed"].BaseValue, this);

        // 4. 实际消费才获得剑势。
        if (consumed > 0)
        {
            await AuroraMomentumService.GainAsync(choiceContext, creature, (int)DynamicVars["MomentumGain"].BaseValue, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);   // 8 → 11
    }
}
