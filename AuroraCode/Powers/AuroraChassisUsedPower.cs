using System.Threading.Tasks;
using AuroraMod.AuroraCode.Helpers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// 自适应底盘「本回合已用次数」计数器 —— 隐藏 Power，<b>权威=<see cref="AuroraPower.Amount"/> 计数</b>（不是 DynamicVar/私有 int）。
/// 联机/战斗态只序列化 {id, amount}，DV 不进同步→重连后已用次数丢失会超额触发。每回合开始自清。
/// </summary>
public sealed class AuroraChassisUsedPower : AuroraPower
{
    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => false;

    public static int Used(Creature creature) =>
        (int)(creature?.GetPowerAmount<AuroraChassisUsedPower>() ?? 0);

    /// <summary>本回合已用次数 +1。Power 不存在则 Apply(1)，否则 SetAmount(Amount+1)。</summary>
    public static async Task IncrementAsync(PlayerChoiceContext ctx, Creature creature)
    {
        if (creature == null)
        {
            return;
        }

        var power = creature.GetPower<AuroraChassisUsedPower>();
        if (power == null)
        {
            await AuroraPowerCmd.Apply<AuroraChassisUsedPower>(ctx, creature, 1, creature, null, silent: true);
            return;
        }

        power.AssertMutable();
        power.SetAmount((int)power.Amount + 1);
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner.Player && Amount != 0)
        {
            AssertMutable();
            SetAmount(0);
        }

        return Task.CompletedTask;
    }
}
