using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Cards.Rare;

/// <summary>
/// D-R04 序列截断 / Sequence Cutoff（稀有，D 指令连锁）。造成 16 伤害；若打出前已连锁，再造成 16；随后散尽全部热量。升级每段 16→20。
/// 每段 14/17→16/20——提高 D 终端攻击的 Boss 兑现（连锁条件/段数/费用/手动出牌判定全不变）。
/// 结算：读打出前连锁快照 → 未连锁 1 段、已连锁 2 段（均 powered，散热前打完故打出前过载区正常吃增伤）→ 战斗仍在进行则 <see cref="HeatPower.VentAsync"/> 散尽到 0。
/// 散尽不触发过热、但不取消已登记 Pending；击杀最后一敌则停段并跳过散尽。与湮灭指令错开：这是"吃完当前过载后主动结束热量冲刺"的终端攻击。
/// </summary>
public class AuroraSequenceCutoff() : AuroraCard(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override string ArtName => "sequence_cutoff";

    /// <summary>金框：已连锁时额外效果可触发（工坊反馈 #1，沿用原版 Dismantle/Spite 的金框语义）。</summary>
    protected override bool ShouldGlowGoldInternal => AuroraGlow.Chained(this);

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Chain, AuroraMechanic.Heat];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(16, ValueProp.Move),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        var special = cardPlay.IsFirstInSeries && ChainPower.GetIsChained(creature);   // 打出前连锁快照
        var hits = special ? 2 : 1;
        var dmg = (int)DynamicVars.Damage.BaseValue;

        for (var i = 0; i < hits; i++)
        {
            if (!CombatManager.Instance.IsInProgress)
            {
                break;
            }

            await CommonActions.CardAttack(this, cardPlay, cardPlay.Target, dmg, ValueProp.Move).Execute(choiceContext);
        }

        // 伤害在散热之前；战斗仍在进行则散尽全部热量（不触发过热，不取消已登记 Pending）。
        if (CombatManager.Instance.IsInProgress)
        {
            await HeatPower.VentAsync(choiceContext, creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);   // 16 → 20
    }
}
