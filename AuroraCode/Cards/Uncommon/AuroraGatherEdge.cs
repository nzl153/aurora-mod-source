using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace AuroraMod.AuroraCode.Cards.Uncommon;

/// <summary>
/// 凝势（普通）：获得4/6剑势，温区额外获得3。区段在开始结算时读取。
/// </summary>
public class AuroraGatherEdge() : AuroraCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override string ArtName => "gather_edge";

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Momentum, AuroraMechanic.Heat];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("MomentumGain", 4m),
        new DynamicVar("WarmBonus", 3m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        var zone = HeatPower.GetZone(creature);   // 打出前快照
        var gain = (int)DynamicVars["MomentumGain"].BaseValue
                   + (zone == HeatPower.HeatZone.Warm ? (int)DynamicVars["WarmBonus"].BaseValue : 0);
        await AuroraMomentumService.GainAsync(choiceContext, creature, gain, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MomentumGain"].UpgradeValueBy(2m);   // 4 → 6
    }
}
