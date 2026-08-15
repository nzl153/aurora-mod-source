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
/// A-R05 焦土风暴 / Scorched-Earth Storm（稀有，A 过热；群体终结技）。对全体 14 伤；过载区改为两次；随后积 3 热；
/// 若这 3 热<b>新产生了待结算过热</b>再造一次。升级：每段 14→18。
/// 每段 12/15→14/18——提高 A 群体终结技的 Boss/精英兑现（三段触发顺序与「仅本牌新建 Pending 才引爆」规则均不变）。
/// 关键：只在「本牌 +3 热让 Pending 从无变有」时追加第三段（打出前已有 Pending 不白赚第三段；重复越线也不追加）。
/// 每段独立 powered AoE（各吃力量/易伤/结算时过载/超频、各消费锁定）。新建 Pending 时先<b>立即引爆结算</b>（付 LockedDamage/超频代价、清热、宕机、派发葬炉/炉心淬锋）→ 存活且战斗继续才打第三段；第三段在清热后，不吃过载×1.25/超频，但可吃刚获得的葬炉力量。
/// </summary>
public class AuroraScorchedEarthStorm() : AuroraCard(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
{
    protected override string ArtName => "scorched_earth_storm";

    /// <summary>金框：处于过载区时额外效果可触发（工坊反馈 #1，沿用原版 Dismantle/Spite 的金框语义）。</summary>
    protected override bool ShouldGlowGoldInternal => AuroraGlow.InZone(this, HeatPower.HeatZone.Overload);

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Heat];

    public override AuroraStrikeVfxKind StrikeVfx => AuroraStrikeVfxKind.Ultimate;   // 招牌终结技：大招紫刀光

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(14, ValueProp.Move),
        new PowerVar<HeatPower>(3),
        new DynamicVar("OverloadHitCount", 2m),
        new DynamicVar("PendingBonusHitCount", 1m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        // 打出前快照：区段决定初始段数。
        var initialHits = HeatPower.GetZone(creature) == HeatPower.HeatZone.Overload
            ? (int)DynamicVars["OverloadHitCount"].BaseValue
            : 1;
        var dmg = (int)DynamicVars.Damage.BaseValue;

        // 初始段数：每段独立 powered AoE；全体死亡则停手。
        for (var i = 0; i < initialHits; i++)
        {
            if (!CombatManager.Instance.IsInProgress)
            {
                break;
            }

            await CommonActions.CardAttack(this, cardPlay, cardPlay.Target, dmg, ValueProp.Move).Execute(choiceContext);
        }

        // 全体已清场：不积热、不追加第三段（胜利宽恕）。
        if (!CombatManager.Instance.IsInProgress)
        {
            return;
        }

        // Pending 快照【紧贴 +3 热之前】读取——避免前两段的命中监听期间新建的 Pending 被误算成这 3 热的新债。
        var pendingBeforeHeat = AuroraOverheatPendingPower.IsPending(creature);

        // 积 3 热。
        await HeatPower.AddHeatAsync(choiceContext, creature, 3, this);

        // 仅当本牌这 3 热让 Pending 从「无」变「有」——立即引爆结算这笔债（付完 LockedDamage / 超频最大生命代价、
        // 清热、生成宕机、派发葬炉/炉心淬锋等结算后监听）；存活且战斗仍进行才追加第三段。第三段发生在清热后→不吃过载×1.25，但可吃刚获得的葬炉力量。
        // 打出前已有 Pending：这 3 热只提高 LockedDamage，不引爆、不追加第三段（旧债不白赚、重复越线不追加）。
        var pendingAfter = AuroraOverheatPendingPower.IsPending(creature);
        if (!pendingBeforeHeat && pendingAfter)
        {
            await HeatPower.SettleOverheatAsync(choiceContext, creature, this);

            if (creature.IsAlive && CombatManager.Instance.IsInProgress)
            {
                var bonusHits = (int)DynamicVars["PendingBonusHitCount"].BaseValue;
                for (var i = 0; i < bonusHits; i++)
                {
                    if (!CombatManager.Instance.IsInProgress)
                    {
                        break;
                    }

                    await CommonActions.CardAttack(this, cardPlay, cardPlay.Target, dmg, ValueProp.Move).Execute(choiceContext);
                }
            }
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);   // 14 → 18
    }
}
