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
/// B1 凝势 / Gather Edge（罕见，B 剑势）。获得 3 剑势；若打出前处于温区，额外获得 2 剑势。升级基础剑势 3→5。
/// 结算：打出前读区段快照 → 合并一次 <see cref="AuroraMomentumService.GainAsync"/>。温区只提升剑势效率不改热量。
/// Echo/额外结算每次都产势（纯产势牌，无 IsFirst 守卫，符合原版「获得 X」类复制行为）。
/// </summary>
public class AuroraGatherEdge() : AuroraCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override string ArtName => "gather_edge";

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Momentum, AuroraMechanic.Heat];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("MomentumGain", 3m),
        new DynamicVar("WarmBonus", 2m),
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
        DynamicVars["MomentumGain"].UpgradeValueBy(2m);   // 3 → 5
    }
}
