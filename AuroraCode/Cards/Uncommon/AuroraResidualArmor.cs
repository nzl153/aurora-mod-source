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

namespace AuroraMod.AuroraCode.Cards.Uncommon;

/// <summary>
/// 33 余热装甲 / Residual Armor（罕见，A 过热暴走；能力）。每回合第一次过热,在承受过热伤害前获得 10 格挡。
/// 升级:10→14。
/// 8/11→10/14——奖励「主动承担过热风险」而非无脑防御(每回合首次门控/结算时点不变;
/// 胜利宽恕没有实际过热结算时不触发)。
/// 结算:经 <see cref="AuroraPowerCmd"/> 挂 <see cref="AuroraResidualArmorPower"/>,Amount = Block。多张合并累加 Amount,
/// 但每回合仍只触发一次(Power 内 UsedThisTurn 门控,回合开始归零)。只缓冲一次,不删除多次过热风险/宕机/递增伤害。
/// Echo/重复能力结算正常叠加 Amount,不加 IsFirst 守卫。
/// </summary>
public class AuroraResidualArmor() : AuroraCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override string ArtName => "residual_armor";

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Heat];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(10, ValueProp.Move),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        await AuroraPowerCmd.Apply<AuroraResidualArmorPower>(
            choiceContext, creature, (int)DynamicVars.Block.BaseValue, creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(4m);   // 10 → 14
    }
}
