using System.Threading.Tasks;
using AuroraMod.AuroraCode.Helpers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// 事件预载「本场已消费」门闩 —— 隐藏 Power。<b>权威状态存在 <see cref="AuroraPower.Amount"/> 的位掩码里</b>（每种预载一个 bit），
/// 因为联机/战斗态只序列化 Power 的 {id, amount}，DynamicVars 不进同步。
/// 战斗态天然每场重置（新战斗无此 Power = Amount 0 = 全未消费）；标记走 <see cref="MarkAsync"/>，读取走 <see cref="IsConsumed"/>。
/// </summary>
public sealed class AuroraPreloadConsumedPower : AuroraPower
{
    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => false;

    // 每种预载一个 bit（存入 Amount 位掩码；同场可携多份不同预载、各 bit 独立）。
    public const int BitAttackModule = 1 << 0;
    public const int BitShieldModule = 1 << 1;
    public const int BitHeat11 = 1 << 2;
    public const int BitMomentum4 = 1 << 3;
    public const int BitChainDraw = 1 << 4;

    public static bool IsConsumed(Creature creature, int bit) =>
        ((int)(creature?.GetPowerAmount<AuroraPreloadConsumedPower>() ?? 0) & bit) != 0;

    /// <summary>把某种预载标记为本场已消费：Power 不存在则 Apply(bit) 创建，否则 SetAmount(Amount | bit)。权威在 Amount，重连可恢复。</summary>
    public static async Task MarkAsync(PlayerChoiceContext ctx, Creature creature, int bit)
    {
        if (creature == null)
        {
            return;
        }

        var power = creature.GetPower<AuroraPreloadConsumedPower>();
        if (power == null)
        {
            await AuroraPowerCmd.Apply<AuroraPreloadConsumedPower>(ctx, creature, bit, creature, null, silent: true);
            return;
        }

        power.AssertMutable();
        power.SetAmount((int)power.Amount | bit);
    }
}
