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
/// D5 连锁防御 / Chain Defense（罕见，D 指令连锁）。获得 5 格挡；若打出前<b>已连锁</b>，改为 13 格挡并抽 1 牌。升级 5/13→7/17。
/// 2026-09-07 改：原为精确「第 4 张手动牌」序列节点（GetCount==3），与步进斩同批改回泛化「已连锁」——
/// 「恰好打出 3 张」的措辞与连锁定义混淆，且第 5 张起静默失效不直观。数值未动。
/// 结算：special = IsFirstInSeries && 已连锁 → 连锁格挡+抽牌，否则只基础格挡。Echo 额外结算只得基础格挡、不重复抽牌。
/// </summary>
public class AuroraChainDefense() : AuroraCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override string ArtName => "chain_defense";

    /// <summary>金框：已连锁时额外效果可触发（工坊反馈 #1，沿用原版 Dismantle/Spite 的金框语义）。</summary>
    protected override bool ShouldGlowGoldInternal => AuroraGlow.Chained(this);

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

        // 打出前连锁快照，只读一次。Echo 段 IsFirstInSeries=false 天然不重复。
        var special = cardPlay.IsFirstInSeries && ChainPower.GetIsChained(creature);

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
