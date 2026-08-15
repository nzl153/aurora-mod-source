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

namespace AuroraMod.AuroraCode.Cards.Uncommon;

/// <summary>
/// D5 连锁防御 / Chain Defense（罕见，D 指令连锁）。获得 5 格挡；若打出前本回合<b>恰好已手动打出 3 张牌</b>（=第 4 张手动牌），改为 13 格挡并抽 1 牌。升级 5/13→7/17。
/// 从泛化「任何已连锁时强化」改为精确「第 4 张手动牌」，与步进斩竞争同一节点（减伤敌 vs 护己）；接续格挡继续保留「任意已连锁时高格挡+散热」的职责。
/// 结算：special = IsFirstInSeries && GetCount==3 → 连锁格挡+抽牌，否则只基础格挡。Echo 额外结算只得基础格挡、不重复抽牌。
/// </summary>
public class AuroraChainDefense() : AuroraCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    private const int SequenceIndex = 3;

    protected override string ArtName => "chain_defense";

    /// <summary>金框：本牌正好是本回合第 N 张手动牌时额外效果可触发（工坊反馈 #1，沿用原版 Dismantle/Spite 的金框语义）。</summary>
    protected override bool ShouldGlowGoldInternal => AuroraGlow.ChainCountIs(this, SequenceIndex);

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Chain];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(5, ValueProp.Move),
        new DynamicVar("ChainedBlock", 13m),
        new DynamicVar("DrawCount", 1m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        var player = Owner;
        if (creature == null || player == null)
        {
            return;
        }

        // 打出前手动出牌数=3（第4张）；排除自动打出——自动打出即使 Count==3 也不触发。
        var special = cardPlay.IsFirstInSeries && !cardPlay.IsAutoPlay && ChainPower.GetCount(creature) == SequenceIndex;

        if (special)
        {
            await CreatureCmd.GainBlock(creature, (int)DynamicVars["ChainedBlock"].BaseValue, ValueProp.Move, cardPlay);
            await CardPileCmd.Draw(choiceContext, DynamicVars["DrawCount"].BaseValue, player);
        }
        else
        {
            await CreatureCmd.GainBlock(creature, (int)DynamicVars.Block.BaseValue, ValueProp.Move, cardPlay);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);              // 5 → 7
        DynamicVars["ChainedBlock"].UpgradeValueBy(4m);    // 13 → 17
    }
}
