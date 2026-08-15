using System.Collections.Generic;
using System.Threading.Tasks;
using AuroraMod.AuroraCode.Helpers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// #54 指令缓存 Power—— 可见能力 Power + 连锁激活监听。
/// <b>每回合第一次激活连锁</b>时抽 <see cref="Amount"/> 张牌；升级版再获得格挡并积热（收益，非惩罚）。
/// <b>权威全进序列化 Amount</b>（同 <see cref="AuroraCoolingCyclePower"/>）：抽牌数=本 Power Amount；升级格挡/积热=<see cref="AuroraCommandCacheBonusPower"/>.Amount（低8=格挡|次8=积热位打包）；
/// {DrawCount}/{BlockOnTrigger}/{HeatOnTrigger} DV 仅作展示镜像。每回合门闩权威=<see cref="AuroraTurnGatePower"/>.BitCommandCache（DV 不进同步故弃用 DV，回合始由门闩 Power 自清）。
/// 触发来自 <see cref="IAuroraChainListener"/> 的越线派发（每回合天然至多一次）；若能力在本回合已连锁之后才打出，
/// 本轮不追溯触发，允许下一回合首次连锁触发（符合设计，同余热装甲）。
/// </summary>
public sealed class AuroraCommandCachePower : AuroraPower, IAuroraChainListener
{
    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => true;
    protected override string IconName => "command_cache";

    // {DrawCount}/{BlockOnTrigger}/{HeatOnTrigger} 仅作展示镜像：DrawCount 从本 Power Amount 派生，Block/Heat 从 AuroraCommandCacheBonusPower.Amount 派生。
    // 门闩权威=AuroraTurnGatePower.BitCommandCache（回合自清）；升级格挡/积热权威=AuroraCommandCacheBonusPower.Amount（低8=格挡/次8=积热）。皆进 Amount、联机/重连一致。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("DrawCount", 0m),
        new DynamicVar("BlockOnTrigger", 0m),
        new DynamicVar("HeatOnTrigger", 0m),
    ];

    /// <summary>统一入口：叠加抽牌数(=本 Power Amount)与升级格挡/积热贡献(=Bonus Power Amount)，再刷新展示。</summary>
    public static async Task ApplyAsync(PlayerChoiceContext ctx, Creature creature, int drawCount, int blockOnTrigger, int heatOnTrigger, CardModel source)
    {
        await AuroraPowerCmd.Apply<AuroraCommandCachePower>(ctx, creature, drawCount, creature, source, silent: true);
        await AuroraCommandCacheBonusPower.AddAsync(ctx, creature, blockOnTrigger, heatOnTrigger);
        creature.GetPower<AuroraCommandCachePower>()?.RefreshDisplay();
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
        DynamicVars["BlockOnTrigger"].BaseValue = AuroraCommandCacheBonusPower.Block(Owner);
        DynamicVars["HeatOnTrigger"].BaseValue = AuroraCommandCacheBonusPower.Heat(Owner);
        InvokeDisplayAmountChanged();
    }

    public async Task OnChainActivatedAsync(PlayerChoiceContext ctx, Creature owner)
    {
        if (owner != Owner || AuroraTurnGatePower.IsGated(owner, AuroraTurnGatePower.BitCommandCache) || Amount <= 0)
        {
            return;
        }

        // 先置门闩防重入（权威进 Amount 位掩码，重连一致），再按固定顺序：抽牌 → 升级格挡 → 升级积热。
        await AuroraTurnGatePower.MarkAsync(ctx, owner, AuroraTurnGatePower.BitCommandCache);

        Flash();
        await CardPileCmd.Draw(ctx, Amount, owner.Player);

        var block = AuroraCommandCacheBonusPower.Block(owner);
        if (block > 0)
        {
            await CreatureCmd.GainBlock(owner, block, ValueProp.Unpowered, null);
        }

        var heat = AuroraCommandCacheBonusPower.Heat(owner);
        if (heat > 0)
        {
            await HeatPower.AddHeatAsync(ctx, owner, heat, null);
        }
    }
}
