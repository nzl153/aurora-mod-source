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

namespace AuroraMod.AuroraCode.Cards.Common;

/// <summary>
/// 22 序列打击 / Sequence Strike（普通，D 指令连锁·锁定）。造成 8 伤害；若打出前已连锁，改为 5 伤害 2 次；
/// 若打出前还在冷区，随后积 1 热。升级：普通 8→10，连锁每段 5→6，段数仍 2。
/// 结算（打出前读连锁+区段快照）：special = IsFirstInSeries && 打出前已连锁。special 走连续 HitCount 段 powered 攻击
/// （每段独立结算力量/易伤/过载/取整；锁定消费+2由常驻锁定消费器逐段自动处理），两段尝试后若打出前在冷区则积 1 热；
/// 否则单段基础伤害、不积热。本牌作第 3 张手动牌时不享受连锁。Echo 额外结算只造成基础单段、不重复积热。
/// </summary>
public class AuroraSequenceStrike() : AuroraCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override string ArtName => "sequence_strike";

    /// <summary>金框：已连锁时额外效果可触发（工坊反馈 #1，沿用原版 Dismantle/Spite 的金框语义）。</summary>
    protected override bool ShouldGlowGoldInternal => AuroraGlow.Chained(this);

    protected override IEnumerable<AuroraMechanic> MechanicTips =>
        [AuroraMechanic.Chain, AuroraMechanic.Lock, AuroraMechanic.Heat];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8, ValueProp.Move),
        new DynamicVar("ChainedDamage", 5m),
        new DynamicVar("HitCount", 2m),
        new PowerVar<HeatPower>(1),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        var wasChained = ChainPower.GetIsChained(creature);
        var zone = HeatPower.GetZone(creature);
        var special = cardPlay.IsFirstInSeries && wasChained;

        if (special)
        {
            var hits = (int)DynamicVars["HitCount"].BaseValue;
            var dmg = (int)DynamicVars["ChainedDamage"].BaseValue;
            for (var i = 0; i < hits; i++)
            {
                // 段间守卫：战斗已结束或目标已死则停手，不空打尸体。
                if (!CombatManager.Instance.IsInProgress || cardPlay.Target == null || cardPlay.Target.IsDead)
                {
                    break;
                }

                await CommonActions.CardAttack(this, cardPlay, cardPlay.Target, dmg, ValueProp.Move).Execute(choiceContext);
            }

            // 两段尝试后：打出前在冷区则积 1 热（目标即使在攻击中死亡，只要战斗仍有效仍积热）。
            if (zone == HeatPower.HeatZone.Cold && CombatManager.Instance.IsInProgress)
            {
                await HeatPower.AddHeatAsync(choiceContext, creature, 1, this);
            }
        }
        else
        {
            var dmg = (int)DynamicVars.Damage.BaseValue;
            await CommonActions.CardAttack(this, cardPlay, cardPlay.Target, dmg, ValueProp.Move).Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);                // 8 → 10
        DynamicVars["ChainedDamage"].UpgradeValueBy(1m);      // 5 → 6
    }
}
