using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Cards.Uncommon;

/// <summary>
/// 40 区段校准 / Zone Calibration（罕见，枢纽）。按打出前区段：冷区积 3 热并抽 1；温区获 10 格挡；过载区散 2 热并获 8 格挡。
/// 升级温 10→13、过载 8→11。三分支即整张主体，Echo 每次真实结算读该次打出前区段执行完整分支（不因非首段变空牌）。
/// </summary>
public class AuroraZoneCalibration() : AuroraCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override string ArtName => "zone_calibration";

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Heat, AuroraMechanic.ZoneChange];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("HeatGain", 3m),
        new DynamicVar("DrawCount", 1m),
        new DynamicVar("WarmBlock", 10m),
        new DynamicVar("VentMax", 2m),
        new DynamicVar("OverloadBlock", 8m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        var player = Owner;
        if (creature == null || player == null)
        {
            return;
        }

        var zone = HeatPower.GetZone(creature);   // 该次结算打出前快照
        switch (zone)
        {
            case HeatPower.HeatZone.Cold:
                await HeatPower.AddHeatAsync(choiceContext, creature, (int)DynamicVars["HeatGain"].BaseValue, this);
                if (CombatManager.Instance.IsInProgress)
                {
                    await CardPileCmd.Draw(choiceContext, DynamicVars["DrawCount"].BaseValue, player);
                }

                break;
            case HeatPower.HeatZone.Warm:
                await CreatureCmd.GainBlock(creature, (int)DynamicVars["WarmBlock"].BaseValue, ValueProp.Move, cardPlay);
                break;
            default:   // Overload（含 10+ 红线）
                await HeatPower.VentUpToAsync(choiceContext, creature, (int)DynamicVars["VentMax"].BaseValue, this);
                await CreatureCmd.GainBlock(creature, (int)DynamicVars["OverloadBlock"].BaseValue, ValueProp.Move, cardPlay);
                break;
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["WarmBlock"].UpgradeValueBy(3m);       // 10 → 13
        DynamicVars["OverloadBlock"].UpgradeValueBy(3m);   // 8 → 11
    }
}
