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

namespace AuroraMod.AuroraCode.Cards.Common;

/// <summary>
/// 20 接续格挡 / Chained Guard（普通，D 指令连锁）。获得 6 格挡；若打出前已连锁，改为 10 并散 1 热。
/// 升级：格挡 6→8，连锁格挡 10→13。
/// 结算（打出前读连锁快照）：special = IsFirstInSeries && 打出前已连锁；special 则获得连锁格挡并 VentUpTo(1)，
/// 否则只获得基础格挡、不散热。本牌作第 3 张手动牌时打出前未连锁，只得基础格挡。Echo 额外结算只得基础格挡、不重复散热。
/// </summary>
public class AuroraChainedGuard() : AuroraCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override string ArtName => "chained_guard";

    /// <summary>金框：已连锁时额外效果可触发（工坊反馈 #1，沿用原版 Dismantle/Spite 的金框语义）。</summary>
    protected override bool ShouldGlowGoldInternal => AuroraGlow.Chained(this);

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Chain, AuroraMechanic.Heat];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(6, ValueProp.Move),
        new DynamicVar("ChainedBlock", 10m),
        new DynamicVar("VentMax", 1m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        var wasChained = ChainPower.GetIsChained(creature);
        var special = cardPlay.IsFirstInSeries && wasChained;

        if (special)
        {
            await CreatureCmd.GainBlock(creature, (int)DynamicVars["ChainedBlock"].BaseValue, ValueProp.Move, cardPlay);
            await HeatPower.VentUpToAsync(choiceContext, creature, (int)DynamicVars["VentMax"].BaseValue, this);
        }
        else
        {
            await CreatureCmd.GainBlock(creature, (int)DynamicVars.Block.BaseValue, ValueProp.Move, cardPlay);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);               // 6 → 8
        DynamicVars["ChainedBlock"].UpgradeValueBy(3m);     // 10 → 13
    }
}
