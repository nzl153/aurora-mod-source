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
/// 38 协议破局 / Protocol Breaker（罕见，B+枢纽）。造成 9 伤害；随后移除目标上由本人施加的 1 层挑战协议；若实际移除，获得 3 剑势。
/// 升级：伤害 9→12，消费层数/剑势不变。
/// 结算（攻击→消费 1 层→按实际得剑势）：挑战协议是「该敌人对施加者本人的 powered attack +10% 承伤」的挑衅风险，
/// 不影响本牌对敌输出；本牌先打后消费，只是把这层风险兑现为剑势。伤害后若目标存活，走
/// <see cref="AuroraChallengeProtocolService.ConsumeAsync"/> 只消费本人 1 层（返回 0/1），实际消费才 +3 剑势。
/// 与 #34 全清型分工（继续冒险 vs 逐层兑现）；无协议仍是 1 费 9 伤不空牌；击杀目标则跳过消费与剑势。
/// </summary>
public class AuroraProtocolBreaker() : AuroraCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override string ArtName => "protocol_breaker";

    protected override IEnumerable<AuroraMechanic> MechanicTips =>
        [AuroraMechanic.ChallengeProtocol, AuroraMechanic.Momentum];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(9, ValueProp.Move),
        new DynamicVar("ProtocolConsumed", 1m),
        new DynamicVar("MomentumGain", 3m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        var target = cardPlay.Target;

        // 1. 单段 powered 攻击（协议只影响敌→你承伤、不影响你的输出；先打后消费只为把风险兑现为剑势）。
        var damage = (int)DynamicVars.Damage.BaseValue;
        await CommonActions.CardAttack(this, cardPlay, target, damage, ValueProp.Move).Execute(choiceContext);

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
        DynamicVars.Damage.UpgradeValueBy(3m);   // 9 → 12
    }
}
