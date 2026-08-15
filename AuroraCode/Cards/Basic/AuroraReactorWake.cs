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
/// 反应炉唤醒 / Reactor Wake（基础，开机）：0 费消耗。
/// 冷区：将热量提升至 4；否则获得 4 格挡。升级格挡 4→7。
/// </summary>
public class AuroraReactorWake() : AuroraCard(0, CardType.Skill, CardRarity.Basic, TargetType.Self)
{
    protected override string ArtName => "reactor_wake";
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Heat];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(4, ValueProp.Move),
        new DynamicVar("TargetHeat", 4m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        var zone = HeatPower.GetZone(creature);
        if (zone == HeatPower.HeatZone.Cold)
        {
            var delta = 4 - HeatPower.GetHeat(creature);
            if (delta > 0)
            {
                await HeatPower.AddHeatAsync(choiceContext, creature, delta, this);
            }
        }
        else
        {
            await CreatureCmd.GainBlock(creature, (int)DynamicVars.Block.BaseValue, ValueProp.Move, cardPlay);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }

    protected override PileType GetResultPileTypeForCardPlay() => PileType.Exhaust;
}
