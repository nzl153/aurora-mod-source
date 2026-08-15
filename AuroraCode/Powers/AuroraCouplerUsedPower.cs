using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// 满载耦合器「本场已触发」标记 —— 隐藏二元 Power（R-03 用）。
/// 用 Power 而非遗物私有 bool：Power 属战斗态、会被序列化并在断线重连时恢复，故重连后满载不会二次触发。
/// 战斗态天然每场重置（新战斗无此 Power = 未触发）；触发时 Apply(1)，读取 <see cref="IsUsed"/>。
/// </summary>
public sealed class AuroraCouplerUsedPower : AuroraPower
{
    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => false;

    public static bool IsUsed(Creature creature) =>
        (creature?.GetPowerAmount<AuroraCouplerUsedPower>() ?? 0) >= 1;
}
