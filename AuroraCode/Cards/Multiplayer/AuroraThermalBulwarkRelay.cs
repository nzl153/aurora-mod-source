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

namespace AuroraMod.AuroraCode.Cards.Multiplayer;

/// <summary>
/// MP-01 热障中继 / Thermal Bulwark Relay（联机专属·罕见，H 枢纽/热量）。目标队友获得 7 格挡；你最多散 4 热，每实际散 1 热该队友再 +2 格挡。升级基础格挡 7→10。
/// 结算：先给目标 Block 格挡 → 仅对出牌者 <see cref="HeatPower.VentUpToAsync"/>(4) 取实际散热 N → 同一目标再 +2×N 格挡。只改出牌者热量、不改目标任何奥萝拉状态；散热不取消已登记 Pending。
/// 目标结算前失效则整张牌停止（不让出牌者白白散热）。作为手动牌正常推进出牌者连锁。<see cref="CardMultiplayerConstraint.MultiplayerOnly"/>：单人池不出现。
/// </summary>
public class AuroraThermalBulwarkRelay() : AuroraCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly)
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override string ArtName => "thermal_bulwark_relay";

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Heat];

    private const int VentMax = 4;
    private const int PerHeat = 2;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(7, ValueProp.Move),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var self = Owner?.Creature;
        var target = cardPlay.Target;
        if (self == null || target == null || !target.IsAlive)
        {
            return;   // 目标失效：整张牌停止，不白白散热。
        }

        // 1. 目标队友先获得基础格挡。
        await CreatureCmd.GainBlock(target, (int)DynamicVars.Block.BaseValue, ValueProp.Move, cardPlay);

        // 2. 仅对出牌者散热，取实际散热量 N（不取消已登记 Pending）。
        var actual = await HeatPower.VentUpToAsync(choiceContext, self, VentMax, this);

        // 3. 每实际散 1 热，目标再 +2 格挡（目标仍存活时）。
        if (actual > 0 && target.IsAlive)
        {
            await CreatureCmd.GainBlock(target, actual * PerHeat, ValueProp.Move, cardPlay);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);   // 7 → 10
    }
}
