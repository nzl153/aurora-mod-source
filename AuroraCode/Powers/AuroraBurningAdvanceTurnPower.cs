using System.Linq;
using System.Threading.Tasks;
using AuroraMod.AuroraCode.Cards.Uncommon;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// #29 燃烧进军 返手门闩—— 隐藏二元 Power：记录本人本回合是否已用掉「返手」机会。
/// 多张燃烧进军共享同一门闩：每名玩家每回合合计最多返手一次。由起始遗物 BeforeCombatStart 挂载
/// （同 AttackTurnTracker），Apply(1) 后归零。回合开始重置门闩，并清除该玩家所有燃烧进军的「武装」状态
/// （ExhaustOnNextPlay）——未在本回合重打的返手卡下回合恢复正常，不会意外消耗。
/// </summary>
public sealed class AuroraBurningAdvanceTurnPower : AuroraPower
{
    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => false;

    public bool ReturnUsed => Amount >= 1;

    public static bool HasUsedReturnThisTurn(Creature creature) =>
        (creature?.GetPowerAmount<AuroraBurningAdvanceTurnPower>() ?? 0) >= 1;

    /// <summary>战斗开始 / 回合开始挂载后初始化为「返手机会未使用」。</summary>
    public void ResetFlag()
    {
        AssertMutable();
        SetAmount(0);
    }

    /// <summary>标记本回合返手机会已用掉。</summary>
    public void MarkReturnUsed()
    {
        AssertMutable();
        SetAmount(1);
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
        {
            return Task.CompletedTask;
        }

        if (ReturnUsed)
        {
            ResetFlag();
        }

        // 清除所有燃烧进军的武装：未重打者下回合恢复正常费用与去向。
        foreach (var card in player.PlayerCombatState.AllCards.OfType<AuroraBurningAdvance>())
        {
            card.DisarmReturn();
        }

        return Task.CompletedTask;
    }
}
