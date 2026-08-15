using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Cards.Rare;

/// <summary>
/// D-R02 闭环防御 / Closed-Loop Defense（稀有，D 指令连锁）。获得 5 格挡，打出前每手动打出 1 张牌再 +2 格挡（最多计 6 张）；若已 ≥6 张，随后最多散 3 热。升级每张 +2→+3。
/// 结算：读打出前手动出牌数 N → 先获得全部格挡 (5 + 每张×min(N,6)) → 再判 N≥6 走 <see cref="HeatPower.VentUpToAsync"/>(3)。
/// 散热不登记待结算过热、也不取消已登记的 Pending。Echo 按当时同回合计数重算但不推进连锁。后置防御：现在先挡 vs 留到长连锁末获高盾并刹车。
/// </summary>
public class AuroraClosedLoopDefense() : AuroraCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override string ArtName => "closed_loop_defense";

    /// <summary>金框：本回合手动出牌数已达排热阈值时额外效果可触发（工坊反馈 #1，沿用原版 Dismantle/Spite 的金框语义）。</summary>
    protected override bool ShouldGlowGoldInternal => AuroraGlow.ChainAtLeast(this, VentThreshold);

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Chain, AuroraMechanic.Heat];

    private const int CardCap = 6;
    private const int VentThreshold = 6;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(5, ValueProp.Move),
        new DynamicVar("PerCard", 2m),
        new DynamicVar("VentMax", 3m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        var n = ChainPower.GetCount(creature);   // 打出前手动出牌数
        var block = (int)DynamicVars.Block.BaseValue
                    + Math.Min(n, CardCap) * (int)DynamicVars["PerCard"].BaseValue;
        await CreatureCmd.GainBlock(creature, block, ValueProp.Move, cardPlay);

        if (n >= VentThreshold)
        {
            await HeatPower.VentUpToAsync(choiceContext, creature, (int)DynamicVars["VentMax"].BaseValue, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["PerCard"].UpgradeValueBy(1m);   // 2 → 3
    }
}
