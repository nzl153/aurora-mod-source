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
/// 记录本回合手动出牌数；第 3 张牌结算后进入连锁，回合开始清零。
/// 自动打出、复制和同一次出牌的额外结算不计数。
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
    protected override bool IsVisibleInternal => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Chained", 0m)];

    public static bool GetIsChained(Creature creature) =>
        (creature?.GetPowerAmount<ChainPower>() ?? 0) >= ChainThreshold;

    public static int GetCount(Creature creature) =>
        (int)(creature?.GetPowerAmount<ChainPower>() ?? 0);

    public void ResetCount()
    {
        AssertMutable();
        SetAmount(0);
        DynamicVars["Chained"].BaseValue = 0m;
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // IsFirstInSeries 防止同一次手动出牌的额外结算重复计数。
        if (cardPlay?.Card?.Owner != Owner.Player || cardPlay.IsAutoPlay || !cardPlay.IsFirstInSeries)
        {
            return;
        }

        AssertMutable();
        SetAmount(CardsPlayed + 1);
        DynamicVars["Chained"].BaseValue = Amount;
        InvokeDisplayAmountChanged();

        if (CardsPlayed == ChainThreshold)
        {
            await DispatchChainActivatedAsync(choiceContext);
        }
    }

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
