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
/// B2 蓄能架式 / Charging Stance（罕见，B 剑势；能力）。回合结束时若本回合未打出攻击牌：获得 2 剑势并积 1 热；
/// 若结算后处于温区，额外获得 1 剑势。升级基础剑势 2→3。
/// 设计偏离：旧稿积 2 热，延迟过热下被动积热是真实时钟，降为 +1（见 <see cref="AuroraChargingStancePower"/>）。
/// 结算：经 <see cref="AuroraChargingStancePower.ApplyAsync"/> 挂能力（Amount=每回合基础剑势，多张累加；温区剑势/积热固定）。
/// </summary>
public class AuroraChargingStance() : AuroraCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override string ArtName => "charging_stance";

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Momentum, AuroraMechanic.Heat];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        // 2→3，与本批剑势产出统一上调（升级仍 +1，故 3→4）。
    new DynamicVar("MomentumGain", 3m),
        new DynamicVar("HeatGain", 1m),
        new DynamicVar("WarmBonus", 1m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        await AuroraChargingStancePower.ApplyAsync(
            choiceContext, creature,
            (int)DynamicVars["MomentumGain"].BaseValue,
            (int)DynamicVars["WarmBonus"].BaseValue,
            (int)DynamicVars["HeatGain"].BaseValue,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MomentumGain"].UpgradeValueBy(1m);   // 3 → 4
    }
}
