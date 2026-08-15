using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// 锁定 / Lock（敌方 Debuff，架构 §9）—— 由奥萝拉施加的战术标记。
///
/// 走 <see cref="PowerType.Debuff"/> 施加通道，因此<b>会被人工制品阻挡</b>；
/// <see cref="PowerInstanceType.InstancedPerApplier"/>：引擎按施加者(Applier)各建一实例，
/// 同一奥萝拉后续施加叠到自己实例，多名奥萝拉互相独立、只能消费自己的层数。
/// 每名奥萝拉对同一敌人最多 6 层。
///
/// 本 Power 只负责「按施加者存层数」；伤害段的「消费 1 层并纯 +2」由集中的
/// <see cref="Patches.AuroraLockDamagePatch"/> 在乘法取整后、Cap 前统一注入，Power 自身不带伤害钩子。
/// 施加 / 消费 / 查询统一走 <see cref="Helpers.AuroraLockService"/>。
/// </summary>
public sealed class LockPower : AuroraPower
{
    public const int MaxStacksPerApplier = 6;

    public override PowerInstanceType InstanceType => PowerInstanceType.InstancedPerApplier;
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override string IconName => "lock";

    public int Stacks => (int)Amount;
    public override int DisplayAmount => Stacks;
    protected override bool IsVisibleInternal => Stacks > 0;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Stacks", 0m)];
}
