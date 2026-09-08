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

namespace AuroraMod.AuroraCode.Cards.Common;

/// <summary>
/// 守势蓄锐（罕见）：获得7/10格挡；本回合未打出攻击时获得3剑势，温区改为4。
/// 攻击记录包含自动打出的攻击。额外结算重复格挡，但剑势只在首次结算获得。
/// </summary>
public class AuroraGuardedFocus() : AuroraCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override string ArtName => "guarded_focus";

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Momentum, AuroraMechanic.Heat];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(7, ValueProp.Move),
        // 2→3 / 3→4。剑势产出普遍只有 2，配合底薪要十来张卡才成型，是「剑势没存在感」的主因之一。
        new DynamicVar("MomentumGain", 3m),
        new DynamicVar("WarmMomentum", 4m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        // 打出前读一次快照。
        var isFirst = cardPlay.IsFirstInSeries;
        var hadPlayedAttack = AuroraAttackTurnTracker.HasPlayedAttackThisTurn(creature);
        var zone = HeatPower.GetZone(creature);

        // 1. 每次真实结算都获得格挡。
        await CreatureCmd.GainBlock(creature, (int)DynamicVars.Block.BaseValue, ValueProp.Move, cardPlay);

        // 2. 仅首次结算 + 本回合未打出攻击 → 获得剑势（温区改为 WarmMomentum）。
        if (isFirst && !hadPlayedAttack)
        {
            var momentum = zone == HeatPower.HeatZone.Warm
                ? (int)DynamicVars["WarmMomentum"].BaseValue
                : (int)DynamicVars["MomentumGain"].BaseValue;
            await AuroraMomentumService.GainAsync(choiceContext, creature, momentum, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);   // 7 → 10
    }
}
