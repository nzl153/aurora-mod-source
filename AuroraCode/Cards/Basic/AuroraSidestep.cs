using System.Collections.Generic;
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

namespace AuroraMod.AuroraCode.Cards.Basic;

/// <summary>
/// 侧移 / Sidestep（基础，D 一次浅连锁教学）。0 费、消耗：
/// 未连锁获 3 格挡；本牌结算前已连锁则获 6 格挡。升级 3/6→4/8。
/// </summary>
public class AuroraSidestep() : AuroraCard(0, CardType.Skill, CardRarity.Basic, TargetType.Self)
{
    protected override string ArtName => "sidestep";

    /// <summary>金框：已连锁时额外效果可触发（工坊反馈 #1，沿用原版 Dismantle/Spite 的金框语义）。</summary>
    protected override bool ShouldGlowGoldInternal => AuroraGlow.Chained(this);
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Chain];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(3, ValueProp.Move),
        new DynamicVar("ChainedBlock", 6m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;

        // 开始结算时只读一次连锁状态：作为本回合第 4 张（结算前已连锁）用连锁值，第 3 张仍用未连锁值。
        var chained = creature != null && ChainPower.GetIsChained(creature);
        var block = (int)(chained ? DynamicVars["ChainedBlock"].BaseValue : DynamicVars.Block.BaseValue);
        await CreatureCmd.GainBlock(creature, block, ValueProp.Move, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(1m);
        DynamicVars["ChainedBlock"].UpgradeValueBy(2m);
    }

    protected override PileType GetResultPileTypeForCardPlay() => PileType.Exhaust;
}
