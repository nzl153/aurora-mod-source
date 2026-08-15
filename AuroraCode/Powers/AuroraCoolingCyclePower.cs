using System.Collections.Generic;
using System.Threading.Tasks;
using AuroraMod.AuroraCode.Helpers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// #61 冷却循环 Power—— 可见能力 Power + 热量变更监听。<b>每回合第一次实际散热</b>时抽 <see cref="Amount"/> 张牌，
/// 升级版再获得格挡。<b>权威全进序列化 Amount</b>：抽牌数=本 Power Amount；升级格挡=<see cref="AuroraCoolingCycleBonusPower"/>.Amount；{DrawCount}/{TriggerBlock} DV 仅作展示镜像。
/// 混合叠加正确：基础+升级 = 抽2/格挡3；两张升级 = 抽2/格挡6。每回合门闩权威=<see cref="AuroraTurnGatePower"/>.BitCoolingCycle（DV 不进同步故弃用 DV，回合始由门闩 Power 自清）。
/// 触发条件：<see cref="HeatChangeInfo.ActualVented"/>（卡牌/效果造成的正常降热，含过载打击-1、通量剑降热、红线内降热）；
/// 过热清零/系统操作/0热无变化不触发。抽牌全交 CardPileCmd.Draw；抽牌堆空仍消耗首次触发、升级格挡照给。
/// </summary>
public sealed class AuroraCoolingCyclePower : AuroraPower, IAuroraHeatChangeListener
{
    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => true;
    protected override string IconName => "cooling_cycle";

    // {DrawCount}/{TriggerBlock} 仅作展示镜像：DrawCount 从本 Power 的 Amount 派生，TriggerBlock 从 AuroraCoolingCycleBonusPower.Amount 派生。
    // 门闩权威=AuroraTurnGatePower.BitCoolingCycle（回合自清）；升级格挡权威=AuroraCoolingCycleBonusPower.Amount。皆进 Amount、联机/重连一致。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("DrawCount", 0m),
        new DynamicVar("TriggerBlock", 0m),
    ];

    /// <summary>统一入口：叠加抽牌数（=本 Power Amount）与升级格挡贡献（=Bonus Power Amount），再刷新展示。</summary>
    public static async Task ApplyAsync(PlayerChoiceContext ctx, Creature creature, int drawCount, int triggerBlock, CardModel source)
    {
        await AuroraPowerCmd.Apply<AuroraCoolingCyclePower>(ctx, creature, drawCount, creature, source, silent: true);
        await AuroraCoolingCycleBonusPower.AddAsync(ctx, creature, triggerBlock);
        creature.GetPower<AuroraCoolingCyclePower>()?.RefreshDisplay();
    }

    /// <summary>任何上身/再应用(含重连恢复)后把展示 DV 从权威 Amount 派生同步。</summary>
    public override Task AfterApplied(Creature applier, CardModel cardSource)
    {
        RefreshDisplay();
        return Task.CompletedTask;
    }

    private void RefreshDisplay()
    {
        AssertMutable();
        DynamicVars["DrawCount"].BaseValue = Amount;
        DynamicVars["TriggerBlock"].BaseValue = AuroraCoolingCycleBonusPower.Block(Owner);
        InvokeDisplayAmountChanged();
    }

    public async Task OnHeatChangedAsync(PlayerChoiceContext ctx, Creature owner, HeatChangeInfo info)
    {
        if (owner != Owner || AuroraTurnGatePower.IsGated(owner, AuroraTurnGatePower.BitCoolingCycle) || Amount <= 0 || !info.ActualVented)
        {
            return;
        }

        // 先置门闩防重入（权威进 Amount 位掩码，重连一致），再按固定顺序：抽牌 → 升级格挡。
        await AuroraTurnGatePower.MarkAsync(ctx, owner, AuroraTurnGatePower.BitCoolingCycle);

        Flash();
        await CardPileCmd.Draw(ctx, Amount, owner.Player);

        var block = AuroraCoolingCycleBonusPower.Block(owner);
        if (block > 0)
        {
            await CreatureCmd.GainBlock(owner, block, ValueProp.Unpowered, null);
        }
    }
}
