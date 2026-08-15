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

namespace AuroraMod.AuroraCode.Cards.Uncommon;

/// <summary>
/// D3 广播斩 / Broadcast Slash（罕见，D 指令连锁；群体）。对所有敌人造成 5 伤害；若打出前已连锁，改为造成两次。升级每段 5→7。
/// 结算：读打出前连锁快照一次 → special = IsFirstInSeries && 已连锁 → 打 HitCount 段完整 AoE，否则单段。
/// 段间守卫：战斗已结束则停手；群体无单目标死亡判定。连锁只在两段之间不复判。Echo 额外结算按其自身 IsFirst 规则。
/// </summary>
public class AuroraBroadcastSlash() : AuroraCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    protected override string ArtName => "broadcast_slash";

    /// <summary>金框：已连锁时额外效果可触发（工坊反馈 #1，沿用原版 Dismantle/Spite 的金框语义）。</summary>
    protected override bool ShouldGlowGoldInternal => AuroraGlow.Chained(this);

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Chain];

    public override AuroraStrikeVfxKind StrikeVfx => AuroraStrikeVfxKind.Flurry;   // 群体横扫：紫刃齐射

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(5, ValueProp.Move),
        new DynamicVar("HitCount", 2m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;

        var special = cardPlay.IsFirstInSeries && creature != null && ChainPower.GetIsChained(creature);
        var hits = special ? (int)DynamicVars["HitCount"].BaseValue : 1;
        var dmg = (int)DynamicVars.Damage.BaseValue;

        for (var i = 0; i < hits; i++)
        {
            if (!CombatManager.Instance.IsInProgress)
            {
                break;
            }

            await CommonActions.CardAttack(this, cardPlay, cardPlay.Target, dmg, ValueProp.Move).Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);   // 5 → 7
    }
}
