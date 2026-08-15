using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// 待结算过热 / Overheat Pending（延迟过热改造）—— 可见持久 Power：热量首次越过阈值 10 时锁定一笔"待结算过热"。
/// 本回合继续保有过载增益(可打完红线连段)，回合结束时由 <see cref="HeatPower.SettleOverheatAsync"/> 统一结算。
/// <b>必须独立于 HeatPower</b>：HeatPower 每次改热量都被整体移除重建(SetHeatAsync)，状态存它身上会丢；
/// 本 Power 与 <see cref="AuroraOverheatCountPower"/> 一样不随热量清零而消失，仅在真正结算完成时移除。
///
/// 语义：Amount = ThresholdCrossCount(本次待结算期间越过 10 的次数，≥1)。存在 = PendingOverheat。
/// 散到 10 以下不取消(仍保留)；<b>结算伤害读已锁定峰值 <see cref="AuroraLockedOverheatDamagePower"/>，散热绝不降低它、也不取消</b>。再次从 &lt;10 升到 ≥10 则 Amount+1(重复越线附加，同步进 Locked)。
/// 权威同步战斗状态(Counter Power)，联机/重连一致；结算幂等靠"结算即移除本 Power"。展示用 {LockedDamage} 从 Locked Power 派生。
/// </summary>
public sealed class AuroraOverheatPendingPower : AuroraPower
{
    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => true;
    protected override string IconName => "overheat_pending";

    // {CrossCount}=越线次数（=Amount 权威）；{LockedDamage}=展示用已锁定伤害镜像，权威在 AuroraLockedOverheatDamagePower.Amount，
    // 由其 SetAtLeastAsync 每次提升后回灌本 DV。
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("CrossCount", 0m), new DynamicVar("LockedDamage", 0m)];

    /// <summary>每次上身/叠加/重建（含重连恢复）时把展示 DV 从权威派生：CrossCount←Amount，LockedDamage←已锁定伤害 Power。</summary>
    public override Task AfterApplied(Creature applier, CardModel cardSource)
    {
        DynamicVars["CrossCount"].BaseValue = Amount;
        DynamicVars["LockedDamage"].BaseValue = AuroraLockedOverheatDamagePower.Get(Owner);
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    /// <summary>由 <see cref="AuroraLockedOverheatDamagePower.SetAtLeastAsync"/> 调用，同步展示用已锁定伤害（纯表现镜像）。</summary>
    public void SetLockedDamageDisplay(int lockedDamage)
    {
        AssertMutable();
        DynamicVars["LockedDamage"].BaseValue = lockedDamage;
        InvokeDisplayAmountChanged();
    }

    /// <summary>是否已锁定待结算过热。</summary>
    public static bool IsPending(Creature creature) =>
        (creature?.GetPowerAmount<AuroraOverheatPendingPower>() ?? 0) >= 1;

    /// <summary>本次待结算期间越过阈值的次数(≥1)。</summary>
    public static int CrossCount(Creature creature) =>
        (int)(creature?.GetPowerAmount<AuroraOverheatPendingPower>() ?? 0);
}
