using System.Threading.Tasks;
using AuroraMod.AuroraCode.Helpers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// 奥萝拉「本回合已触发」共享门闩 —— 隐藏 Power，<b>权威=<see cref="AuroraPower.Amount"/> 位掩码</b>（每个每回合一次的能力占一个 bit）。
/// 用 Power(Amount) 而非各能力自己的 DynamicVar/私有字段：联机/战斗态只序列化 {id, amount}，DV 不进同步→重连后门闩丢失会二次触发。
/// 每回合开始自清（<see cref="AfterPlayerTurnStart"/> 归零全部门闩）；战斗态天然每场重置。
/// 二元门闩用本 Power 的 bit；带计数的「本回合已用 N 次」（自适应底盘）另用 <see cref="AuroraChassisUsedPower"/> 计数器，避免与位掩码互相踩。
/// </summary>
public sealed class AuroraTurnGatePower : AuroraPower
{
    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => false;

    // 每个每回合一次的能力占一个 bit。
    public const int BitResidualArmor = 1 << 0;
    public const int BitPhaseShield = 1 << 1;
    public const int BitSentryArray = 1 << 2;
    public const int BitCoolingCycle = 1 << 3;
    public const int BitCommandCache = 1 << 4;

    /// <summary>该能力本回合是否已触发过。</summary>
    public static bool IsGated(Creature creature, int bit) =>
        ((int)(creature?.GetPowerAmount<AuroraTurnGatePower>() ?? 0) & bit) != 0;

    /// <summary>把某能力标记为本回合已触发：Power 不存在则 Apply(bit) 创建，否则 SetAmount(Amount | bit)。权威在 Amount，重连可恢复。</summary>
    public static async Task MarkAsync(PlayerChoiceContext ctx, Creature creature, int bit)
    {
        if (creature == null)
        {
            return;
        }

        var power = creature.GetPower<AuroraTurnGatePower>();
        if (power == null)
        {
            await AuroraPowerCmd.Apply<AuroraTurnGatePower>(ctx, creature, bit, creature, null, silent: true);
            return;
        }

        power.AssertMutable();
        power.SetAmount((int)power.Amount | bit);
    }

    /// <summary>回合开始清空全部门闩（下一回合各能力可再触发一次）。</summary>
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
