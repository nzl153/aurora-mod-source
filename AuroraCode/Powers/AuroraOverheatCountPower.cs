using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// 隐藏计数：记录本场战斗已过热的次数。用于过热伤害递增（10/12/14/16 封顶）与阈值类效果。
/// 战斗结束随 Power 一并清除，天然按场重置。玩家不可见。
/// </summary>
public sealed class AuroraOverheatCountPower : AuroraPower
{
    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => false;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Count", 0m)];

    public static int Get(MegaCrit.Sts2.Core.Entities.Creatures.Creature creature) =>
        (int)(creature?.GetPowerAmount<AuroraOverheatCountPower>() ?? 0);
}
