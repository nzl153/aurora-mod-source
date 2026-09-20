using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuroraMod.AuroraCode.Visuals;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace AuroraMod.AuroraCode.Powers;

public enum ModuleKind
{
    Attack,
    Shield,
}

/// <summary>
/// 悬浮模块的公共基类。每个模块都是独立 Power 实例，Amount 保存当前生效值。
/// 部署、替换、强化和收回统一由 AuroraModuleController 管理。
/// </summary>
public abstract class AuroraModulePower : AuroraPower
{
    public abstract ModuleKind Kind { get; }

    /// <summary>新部署模块的基础值；轮转时会保留已有强化量。</summary>
    public abstract int BaseValue { get; }

    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public int Value => (int)Amount;
    public override int DisplayAmount => Value;
    protected override bool IsVisibleInternal => Value > 0;

    protected override IEnumerable<MegaCrit.Sts2.Core.Localization.DynamicVars.DynamicVar> CanonicalVars =>
        [new MegaCrit.Sts2.Core.Localization.DynamicVars.DynamicVar("Value", 0m)];

    public void PrimeValue(int value)
    {
        DynamicVars["Value"].BaseValue = value;
    }

    public void Refresh()
    {
        AssertMutable();
        DynamicVars["Value"].BaseValue = Value;
        InvokeDisplayAmountChanged();
    }

    /// <summary>触发模块效果；被动触发和主动触发共用此入口。</summary>
    public abstract Task TriggerAsync(PlayerChoiceContext choiceContext);

    public override async Task AfterApplied(Creature applier, CardModel cardSource)
    {
        Refresh();
        AuroraModuleVisualBridge.RequestRebuild(Owner);
        await Task.CompletedTask;
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        // 移除后 Owner 可能已为空，重建视觉时使用 oldOwner。
        AuroraModuleVisualBridge.RequestRebuild(oldOwner);
        await Task.CompletedTask;
    }

    public static List<AuroraModulePower> All(Creature creature) =>
        creature?.Powers.OfType<AuroraModulePower>().Where(m => m.Owner != null).ToList()
        ?? new List<AuroraModulePower>();
}
