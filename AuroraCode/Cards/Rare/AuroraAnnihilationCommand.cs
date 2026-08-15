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
/// D-R01 湮灭指令 / Annihilation Command（稀有，D 指令连锁）。造成 10 伤害；若打出前本回合已手动打出 ≥6 张牌，改为造成 4 次。升级每段 10→12。
/// <b>删除消耗（Exhaust）</b>——解决「第一轮爆发后只剩小伤害」，让 D 在 Boss 战能重复组织连锁爆发。
/// 这是<b>有意保留</b>的高门槛循环：本牌不回手、不抽牌、不退能量，要重复使用必须靠完整构筑（抽牌+能量+小牌体系）自行凑齐 6 张手动出牌，
/// 本牌无法单独闭环。按裁决不加「每回合一次」等全局封锁。
/// 结算：读打出前手动出牌数快照（本牌通常是第 7 张；本牌自身不计入，无法帮自己达标）→ special = IsFirstInSeries && count≥6
/// → 打 HitCount 段、否则 1 段，每段独立 powered（各自消费锁定）。段间守卫战斗仍在进行则继续。
/// AutoPlay/Echo/复制不推进手动出牌数；≥6 仅卡内门槛、不改全局第 3 张激活规则。D 招牌高连锁终结技。
/// </summary>
public class AuroraAnnihilationCommand() : AuroraCard(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override string ArtName => "annihilation_command";

    /// <summary>金框：本回合手动出牌数已达阈值时额外效果可触发（工坊反馈 #1，沿用原版 Dismantle/Spite 的金框语义）。</summary>
    protected override bool ShouldGlowGoldInternal => AuroraGlow.ChainAtLeast(this, Threshold);

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Chain];

    public override AuroraStrikeVfxKind StrikeVfx => AuroraStrikeVfxKind.Ultimate;   // 招牌终结技：大招紫刀光

    private const int Threshold = 6;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(10, ValueProp.Move),
        new DynamicVar("HitCount", 4m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        // 打出前手动出牌数快照（AfterCardPlayed 在 OnPlay 之后才 +1，本牌自身不计入）。
        var special = cardPlay.IsFirstInSeries && ChainPower.GetCount(creature) >= Threshold;
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
        DynamicVars.Damage.UpgradeValueBy(2m);   // 10 → 12
    }
}
