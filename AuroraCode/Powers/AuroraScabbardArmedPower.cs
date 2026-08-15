using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// 热衬剑鞘「本场已发放」标记 —— 隐藏二元 Power（R-02 用）。
/// 用 Power 而非遗物私有 bool：Power 属战斗态、会被序列化并在断线重连时恢复，故重连后剑鞘不会二次发放。
/// 战斗态天然每场重置（新战斗无此 Power = 未发放）；发放时 Apply(1)，读取 <see cref="IsArmed"/>。
/// </summary>
public sealed class AuroraScabbardArmedPower : AuroraPower
{
    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => false;

    public static bool IsArmed(Creature creature) =>
        (creature?.GetPowerAmount<AuroraScabbardArmedPower>() ?? 0) >= 1;
}
