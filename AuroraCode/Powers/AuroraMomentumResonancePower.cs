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
/// 剑势共鸣：回合开始读取一次剑势，不足10则每层获得3剑势，否则每层抽1张。
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
    // 使文案能直接写「获得 {MomentumGain} 点剑势」，不必让玩家心算「{Amount}×3」。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Threshold", 10m),
        new DynamicVar("GainPerStack", 3m),
        new DynamicVar("MomentumGain", 3m),
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
