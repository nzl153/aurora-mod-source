using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Cards.Basic;

/// <summary>
/// 打击 / Strike（基础）：造成 6 伤害。打出前只读一次区段——冷区或温区随后积 1 热；过载区随后散 1 热。升级 6→9。
/// 温区也 +1（解除温区死锁，让反应炉唤醒→打击×3 能稳定 4→7 进过载）；过载区 -1 由基础打击自然回温，
/// 故基础打击在任何合法热量下都不会把角色从 9 直接推过热（过热风险仍由灼热切割等主动积热牌承担）。
/// </summary>
public class AuroraStrike() : AuroraCard(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
{
    protected override string ArtName => "strike";
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Heat];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6, ValueProp.Move),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        var zone = creature != null ? HeatPower.GetZone(creature) : HeatPower.HeatZone.Cold;

        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);

        if (creature == null)
        {
            return;
        }

        switch (zone)
        {
            // 冷区或温区：随后 +1 热（走 AddHeatAsync，非 VentAsync）。
            case HeatPower.HeatZone.Cold:
            case HeatPower.HeatZone.Warm:
                await HeatPower.AddHeatAsync(choiceContext, creature, 1, this);
                break;
            // 过载区（Critical 仅异常兜底）：随后 -1 热，轻轻回温，基础打击不自触发过热。
            case HeatPower.HeatZone.Overload:
            case HeatPower.HeatZone.Critical:
                await HeatPower.AddHeatAsync(choiceContext, creature, -1, this);
                break;
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
