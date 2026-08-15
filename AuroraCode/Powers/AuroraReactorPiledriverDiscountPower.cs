using System.Linq;
using System.Threading.Tasks;
using AuroraMod.AuroraCode.Cards.Uncommon;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// #30 反应炉重拳 降费器—— 隐藏二元 Power + 过热监听。记录本人本场是否已过热过一次；
/// 一旦过热，把本人牌堆里所有反应炉重拳的费用用 <c>SetThisCombat(1, reduceOnly:true)</c> 降到不高于 1
/// （本场持续、reduce-only 不会把 0 费顶回 1）。由起始遗物 BeforeCombatStart 挂载，Apply(1) 后归零。
/// 回合开始再扫一遍牌堆兜底：覆盖过热后新进战斗 / 重连补挂的副本（SetThisCombat 幂等，重复设无副作用）。
/// </summary>
public sealed class AuroraReactorPiledriverDiscountPower : AuroraPower, IAuroraOverheatListener
{
    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => false;

    public bool HasOverheated => Amount >= 1;

    /// <summary>战斗开始挂载后初始化为「本场尚未过热」。</summary>
    public void ResetFlag()
    {
        AssertMutable();
        SetAmount(0);
    }

    public Task OnOverheatAsync(PlayerChoiceContext ctx, Creature owner, int overheatIndex)
    {
        if (owner != Owner)
        {
            return Task.CompletedTask;
        }

        if (!HasOverheated)
        {
            AssertMutable();
            SetAmount(1);
        }

        ApplyDiscount(owner);
        return Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner.Player && HasOverheated)
        {
            ApplyDiscount(Owner);
        }

        return Task.CompletedTask;
    }

    /// <summary>把本人全部牌堆里的反应炉重拳降到不高于 1（reduce-only、本场持续、幂等）。</summary>
    private static void ApplyDiscount(Creature owner)
    {
        var combat = owner?.Player?.PlayerCombatState;
        if (combat == null)
        {
            return;
        }

        foreach (var card in combat.AllCards.OfType<AuroraReactorPiledriver>())
        {
            card.EnergyCost.SetThisCombat(1, reduceOnly: true);
        }
    }
}
