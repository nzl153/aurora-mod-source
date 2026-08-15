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
/// 战术收束 / Tactical Convergence（基础，调温）：获得 5 格挡。
/// 只读一次打出前区段：冷区随后积 2 热；温区或过载区随后散 2 热。升级格挡 5→8。
/// 格挡 6/9→5/8——普通战几乎不掉血，基础防御地基过于自足，小幅下调（效果/顺序/调热不变）。
/// 结算：读区段 → 格挡 → 调热。
/// </summary>
public class AuroraTacticalConvergence() : AuroraCard(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
{
    protected override string ArtName => "tactical_convergence";

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Heat];

    private const int ZoneHeatDelta = 2;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(5, ValueProp.Move),
        new PowerVar<HeatPower>(2m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        var zone = creature != null ? HeatPower.GetZone(creature) : HeatPower.HeatZone.Cold;

        await CreatureCmd.GainBlock(creature, (int)DynamicVars.Block.BaseValue, ValueProp.Move, cardPlay);

        if (creature == null)
        {
            return;
        }

        switch (zone)
        {
            case HeatPower.HeatZone.Cold:
                await HeatPower.AddHeatAsync(choiceContext, creature, ZoneHeatDelta, this);
                break;
            case HeatPower.HeatZone.Warm:
            case HeatPower.HeatZone.Overload:
            case HeatPower.HeatZone.Critical:
                await HeatPower.AddHeatAsync(choiceContext, creature, -ZoneHeatDelta, this);
                break;
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);   // 5 → 8
    }
}
