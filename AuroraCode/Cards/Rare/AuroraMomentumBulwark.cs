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
/// 归势成垒（稀有，保留、消耗）：清空剑势，获得6/10 + 2×清空量的格挡。
/// 清空至少8剑势时抽2张；保留用于改善防御牌的使用时机。
/// </summary>
public class AuroraMomentumBulwark() : AuroraCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override string ArtName => "momentum_bulwark";

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];

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
