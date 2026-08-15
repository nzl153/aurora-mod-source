using System.Collections.Generic;
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
/// 39 热力转换 / Thermodynamic Conversion（罕见，枢纽，消耗）。获得 7 格挡，最多散 3 热；每实际散 1 热再获 2 格挡；
/// 若恰好散满 3，获得 1 能量。升级基础 7→9。高热转防御的单次刹车，低热无能量白拿。
/// 结算（整张为主体，Echo 每次完整重复）：GainBlock(基础) → VentUpTo(3)（内部先结算冷却循环）→ 若实际散&gt;0 再 GainBlock(实际×2)
/// → 若实际==VentMax 得 1 能量。散热不取消 Pending。
/// 1 费罕见位数值上调（对照原版 680 张卡解包统计：奥萝拉 1 费罕见攻击均值 7.2 / 中位 7，
/// 原版 9.4 / 8；格挡 6.1 / 6 对原版 8.4 / 7——该档是全卡池唯一明显洼地，而罕见奖励是玩家整局看得最多的三选一）。
/// </summary>
public class AuroraThermodynamicConversion() : AuroraCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override string ArtName => "thermodynamic_conversion";
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Heat];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(7, ValueProp.Move),
        new DynamicVar("VentMax", 3m),
        new DynamicVar("BlockPerHeat", 2m),
        new DynamicVar("EnergyGain", 1m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        var player = Owner;
        if (creature == null || player == null)
        {
            return;
        }

        // 1. 基础格挡。
        await CreatureCmd.GainBlock(creature, (int)DynamicVars.Block.BaseValue, ValueProp.Move, cardPlay);

        // 2. 最多散 VentMax 热（内部会先触发冷却循环等散热监听），取实际散去量。
        var ventMax = (int)DynamicVars["VentMax"].BaseValue;
        var actual = await HeatPower.VentUpToAsync(choiceContext, creature, ventMax, this);

        // 3. 每实际散 1 热再获 BlockPerHeat 格挡（一次结算，不逐点拆分）。
        if (actual > 0)
        {
            await CreatureCmd.GainBlock(creature, actual * (int)DynamicVars["BlockPerHeat"].BaseValue, ValueProp.Move, cardPlay);
        }

        // 4. 恰好散满（实际==VentMax）才得能量。
        if (actual == ventMax && CombatManager.Instance.IsInProgress)
        {
            await PlayerCmd.GainEnergy(DynamicVars["EnergyGain"].BaseValue, player);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);   // 7 → 9
    }
}
