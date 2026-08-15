using System.Collections.Generic;
using System.Threading.Tasks;
using AuroraMod.AuroraCode.Helpers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// B-R04 剑势共鸣 Power—— 可见能力。回合开始时读一次当前剑势：少于 10 则获得 2×层数 剑势；否则抽 层数 张牌。
/// <see cref="Amount"/>=层数（多张累加，共用同一剑势快照）。本次获得剑势即使达到 10 也不在同一次补发抽牌（先判分支再执行）。
/// 分界 6→10——把「叠势阶段」拉长，让剑势真正累积到可兑现的爆发量，而不是早早切进抽牌档（不新增能量）。
/// 打出时无即时收益；Echo 只加层数不即时触发。把"是否清空剑势"变成长期决策：保留高势持续抽牌、清空则回到自动蓄势。
/// </summary>
public sealed class AuroraMomentumResonancePower : AuroraPower
{
    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => true;
    protected override string IconName => "momentum_resonance";

    // 阈值/每层收益由 private const 改为 DynamicVar，文案改用占位符自动追踪。
    // 本能力正是「改代码文案没跟着变」的重灾区——A10 把分界 6→10 时只改了 cards.json，
    // powers.json 仍写 <6，直到实机才发现。变量化后这类漏改在源头消失。
    // MomentumGain = Amount × GainPerStack，随层数在 AfterApplied 同步刷新，
    // 使文案能直接写「获得 {MomentumGain} 点剑势」，不必让玩家心算「{Amount}×2」。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Threshold", 10m),
        new DynamicVar("GainPerStack", 2m),
        new DynamicVar("MomentumGain", 2m),
    ];

    public override Task AfterApplied(Creature applier, CardModel cardSource)
    {
        AssertMutable();
        DynamicVars["MomentumGain"].BaseValue = Amount * DynamicVars["GainPerStack"].BaseValue;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player || Amount <= 0)
        {
            return;
        }

        if (!CombatManager.Instance.IsInProgress)
        {
            return;
        }

        var stacks = (int)Amount;
        var momentum = AuroraMomentumService.Get(Owner);   // 回合开始只读一次
        Flash();

        if (momentum < (int)DynamicVars["Threshold"].BaseValue)
        {
            await AuroraMomentumService.GainAsync(
                choiceContext, Owner, stacks * (int)DynamicVars["GainPerStack"].BaseValue, null);
        }
        else
        {
            await CardPileCmd.Draw(choiceContext, stacks, player);
        }
    }
}
