using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// 指令连锁 / Chain（D 指令连锁流派）—— 二元回合状态。
/// 本回合已打出 ≥3 张牌后激活（第 4 张是第一张享受连锁的牌），保持到回合结束，回合开始清零。
/// Amount 记录本回合已手动打出的牌数（真值源）；<see cref="IsChained"/> 供卡牌读取。
/// 复制/自动打出/额外结算不推进（AfterCardPlayed 用 !IsAutoPlay && IsFirstInSeries 守卫）。
/// 由起始遗物 BeforeCombatStart 在战斗开始统一挂载，从第 1 张牌起计数。
/// </summary>
public sealed class ChainPower : AuroraPower
{
    public const int ChainThreshold = 3;

    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override string IconName => "chain";

    public int CardsPlayed => (int)Amount;
    public bool IsChained => CardsPlayed >= ChainThreshold;
    public override int DisplayAmount => IsChained ? 1 : 0;
    // 保持显示（作者要求，免得日后忘了加回）。当前无 D 卡消费连锁，这只是个"未连锁/已连锁"指示器。
    protected override bool IsVisibleInternal => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Chained", 0m)];

    public static bool GetIsChained(Creature creature) =>
        (creature?.GetPowerAmount<ChainPower>() ?? 0) >= ChainThreshold;

    /// <summary>读取本回合已手动打出的牌数（本牌结算前的真值：AfterCardPlayed 在 OnPlay 之后才 +1）。无 Power 时 0。</summary>
    public static int GetCount(Creature creature) =>
        (int)(creature?.GetPowerAmount<ChainPower>() ?? 0);

    /// <summary>把本回合已出牌数归零（回合开始，或战斗开始挂载后初始化）。</summary>
    public void ResetCount()
    {
        AssertMutable();
        // CardsPlayed/IsChained 都读 Amount，必须写 Amount；DynamicVar 同步保持一致。
        SetAmount(0);
        DynamicVars["Chained"].BaseValue = 0m;
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 只有玩家「手动打出」的牌推进连锁（架构 §7.1）：
        // - IsAutoPlay=true 的复制/自动打出（CreateDupe→AutoPlay、抽牌堆自动打出）天然排除；
        // - IsFirstInSeries 保证同一次手动打出的多段额外结算只计 1 次
        //   （Echo/复制药水/额外结算会抬高 PlayCount 再循环，但它们 IsAutoPlay 仍为 false，
        //    只靠 !IsAutoPlay 会把额外结算也刷进连锁，故必须再叠 IsFirstInSeries）。
        if (cardPlay?.Card?.Owner != Owner.Player || cardPlay.IsAutoPlay || !cardPlay.IsFirstInSeries)
        {
            return;
        }

        AssertMutable();
        // 写 Amount（真值源），DynamicVar 同步。
        SetAmount(CardsPlayed + 1);
        DynamicVars["Chained"].BaseValue = Amount;
        InvokeDisplayAmountChanged();

        // 恰好越过阈值（2→3）那一刻派发一次连锁激活事件（每回合天然至多一次）。
        if (CardsPlayed == ChainThreshold)
        {
            await DispatchChainActivatedAsync(choiceContext);
        }
    }

    /// <summary>连锁激活派发：快照迭代监听器，在同步 action 链内 await；不消耗 RNG。</summary>
    private async Task DispatchChainActivatedAsync(PlayerChoiceContext ctx)
    {
        var owner = Owner;
        foreach (var power in owner.Powers.ToList())
        {
            if (power is IAuroraChainListener listener)
            {
                await listener.OnChainActivatedAsync(ctx, owner);
            }
        }

        // 遗物也可监听连锁激活（R-01 脉冲节拍器）：快照迭代拥有者遗物。
        var relics = owner.Player?.Relics;
        if (relics != null)
        {
            foreach (var relic in relics.ToList())
            {
                if (relic is IAuroraChainListener relicListener)
                {
                    await relicListener.OnChainActivatedAsync(ctx, owner);
                }
            }
        }
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner.Player && CardsPlayed != 0)
        {
            ResetCount();
        }

        return Task.CompletedTask;
    }
}
