using System.Linq;
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

namespace AuroraMod.AuroraCode.Cards.Rare;

/// <summary>
/// D-R05 无限演算 / Infinite Calculation（稀有，D 指令连锁；消耗）。抽牌至手牌 7 张（已连锁则 9），每实际抽 1 张积 1 热。消耗。升级目标 7/9→8/10。
/// 结算：读打出前连锁快照 → 本牌已离手，按当时手牌数算需抽数 = max(0, 目标-手牌)→ 抽 → 以抽前后手牌差取实际抽到数 K（牌堆不足/手牌上限未抽到不计）→ 一次积 K 热。
/// 积热可换区/进红线/登记待结算过热。本牌不产能量/不回手/始终消耗，无法自成循环。D 的一次性续算许可（热量风险换大手牌）。
/// </summary>
public class AuroraInfiniteCalculation() : AuroraCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override string ArtName => "infinite_calculation";

    /// <summary>金框：已连锁时额外效果可触发（工坊反馈 #1，沿用原版 Dismantle/Spite 的金框语义）。</summary>
    protected override bool ShouldGlowGoldInternal => AuroraGlow.Chained(this);

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Chain, AuroraMechanic.Heat];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Target", 7m),
        new DynamicVar("ChainedTarget", 9m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        var player = Owner;
        if (creature == null || player == null)
        {
            return;
        }

        var special = cardPlay.IsFirstInSeries && ChainPower.GetIsChained(creature);   // 打出前连锁快照
        var target = special
            ? (int)DynamicVars["ChainedTarget"].BaseValue
            : (int)DynamicVars["Target"].BaseValue;

        // 本牌已离手，按当时手牌数算需抽数；以抽前后手牌差取实际抽到数（防牌堆不足/手牌上限虚计）。
        // 防御性排除本牌自身（若引擎在 OnPlay 时本牌仍短暂计入手牌，排除后"抽至 N"不会少抽 1）。
        var handBefore = HandCountExcludingSelf(player);
        var needed = Math.Max(0, target - handBefore);
        if (needed > 0)
        {
            await CardPileCmd.Draw(choiceContext, needed, player);
        }

        var handAfter = HandCountExcludingSelf(player);
        var drawn = handAfter - handBefore;
        if (drawn > 0)
        {
            await HeatPower.AddHeatAsync(choiceContext, creature, drawn, this);
        }
    }

    private int HandCountExcludingSelf(MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        var cards = player.PlayerCombatState?.Hand?.Cards;
        return cards == null ? 0 : cards.Count(c => !ReferenceEquals(c, this));
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Target"].UpgradeValueBy(1m);          // 7 → 8
        DynamicVars["ChainedTarget"].UpgradeValueBy(1m);   // 9 → 10
    }
}
