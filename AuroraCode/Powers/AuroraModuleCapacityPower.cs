using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// 辅助肩架容量 / Auxiliary Hardpoint capacity（#76 唯一扩容来源）—— 权威战斗 Power，记录「本场额外模块槽位」。
///
/// 只持续本场战斗（随 Power 按场清除，不写入 deck/run 层）。基础容量固定 2，硬上限固定 3，故额外容量只允许 0/1。
/// 按玩家独立（挂各自 Creature），走同步 Power/战斗状态，单人 / 远端 / 存档 / 重连一致。
/// 容量判断统一由 <see cref="Helpers.AuroraModuleController"/> 读取本 Power，不再依赖写死常量。
/// </summary>
public sealed class AuroraModuleCapacityPower : AuroraPower
{
    /// <summary>基础模块容量（无本 Power 时的槽数）。</summary>
    public const int BaseSlots = 2;

    /// <summary>模块容量硬上限（任何叠加 / 复制都不得突破）。</summary>
    public const int HardMaxSlots = 3;

    /// <summary>额外容量上限 = 硬上限 - 基础 = 1。</summary>
    public const int MaxExtra = HardMaxSlots - BaseSlots;

    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override string IconName => "auxiliary_hardpoint";

    /// <summary>额外容量，钳制到 [0, 1]：即便被多次施加也不会突破。</summary>
    public int Extra => Math.Clamp((int)Amount, 0, MaxExtra);
    public override int DisplayAmount => Extra;
    protected override bool IsVisibleInternal => Extra > 0;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Extra", 0m)];

    /// <summary>某生物当前的额外容量（0/1）。</summary>
    public static int GetExtra(Creature creature) =>
        Math.Clamp((int)(creature?.GetPowerAmount<AuroraModuleCapacityPower>() ?? 0), 0, MaxExtra);

    /// <summary>某生物的权威当前容量 = clamp(2 + extra, 2, 3)。</summary>
    public static int CurrentCapacity(Creature creature) =>
        Math.Clamp(BaseSlots + GetExtra(creature), BaseSlots, HardMaxSlots);
}
