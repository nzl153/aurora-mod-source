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
/// B-R05 归势成垒 / Momentum Bulwark（稀有，B 剑势；消耗）。清空全部剑势，获得 6 + 每势×2 格挡；若清空 ≥8 势，抽 2 张牌。消耗。升级基础格挡 6→10。
/// 结算：先 <see cref="AuroraMomentumService.ClearAllAsync"/> 取清空量 N → 一次获得 (6 + 2N) 格挡（升级 10+2N）→ N≥8 抽 2。
/// 0 势仍得 6/10 格挡但不抽牌；不设读取上限（始终清空全势并消耗）。Echo 首段已清空、后续通常只基础格挡。与剑势护体/一刀两断成三选一。
/// </summary>
public class AuroraMomentumBulwark() : AuroraCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override string ArtName => "momentum_bulwark";

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Momentum];

    private const int PerMomentum = 2;
    private const int DrawThreshold = 8;
    private const int DrawCount = 2;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(6, ValueProp.Move),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        var player = Owner;
        if (creature == null || player == null)
        {
            return;
        }

        var cleared = await AuroraMomentumService.ClearAllAsync(choiceContext, creature, this);
        var block = (int)DynamicVars.Block.BaseValue + cleared * PerMomentum;
        await CreatureCmd.GainBlock(creature, block, ValueProp.Move, cardPlay);

        if (cleared >= DrawThreshold)
        {
            await CardPileCmd.Draw(choiceContext, DrawCount, player);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(4m);   // 6 → 10
    }
}
