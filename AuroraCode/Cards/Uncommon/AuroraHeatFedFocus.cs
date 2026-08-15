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
/// B5 借热养势 / Heat-fed Focus（罕见，B/枢纽）。最多散 3 热；每实际散 1 热得 1 剑势；若因此从过载区降至温区，额外 2 剑势。
/// 升级散热上限 3→4、额外剑势 2→3。
/// 结算：读散热前区段 → <see cref="HeatPower.VentUpToAsync"/> 取实际量（内部触发冷却循环等散热监听，本卡不二次分发）→
/// 每实际散 1 热得 MomentumPerHeat 剑势 → 前过载&&后温区 → 额外 DropBonus 剑势。散热不取消已锁定过热。
/// </summary>
public class AuroraHeatFedFocus() : AuroraCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override string ArtName => "heat_fed_focus";

    /// <summary>金框：处于过载区时额外效果可触发（工坊反馈 #1，沿用原版 Dismantle/Spite 的金框语义）。</summary>
    protected override bool ShouldGlowGoldInternal => AuroraGlow.InZone(this, HeatPower.HeatZone.Overload);

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Momentum, AuroraMechanic.Heat];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("VentMax", 3m),
        new DynamicVar("MomentumPerHeat", 1m),
        new DynamicVar("DropBonus", 2m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        var beforeZone = HeatPower.GetZone(creature);
        var actual = await HeatPower.VentUpToAsync(choiceContext, creature, (int)DynamicVars["VentMax"].BaseValue, this);

        if (actual > 0)
        {
            await AuroraMomentumService.GainAsync(
                choiceContext, creature, actual * (int)DynamicVars["MomentumPerHeat"].BaseValue, this);
        }

        if (beforeZone == HeatPower.HeatZone.Overload && HeatPower.GetZone(creature) == HeatPower.HeatZone.Warm)
        {
            await AuroraMomentumService.GainAsync(choiceContext, creature, (int)DynamicVars["DropBonus"].BaseValue, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["VentMax"].UpgradeValueBy(1m);     // 3 → 4
        DynamicVars["DropBonus"].UpgradeValueBy(1m);   // 2 → 3
    }
}
