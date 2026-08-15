using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuroraMod.AuroraCode.Visuals;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>模块类型：攻击型 / 护盾型。</summary>
public enum ModuleKind
{
    Attack,
    Shield,
}

/// <summary>
/// 悬浮武装模块（C 悬浮部件流派）的公共基类 —— 架构 §6「两个独立模块槽」。
///
/// 每个已部署模块是一个 <see cref="PowerInstanceType.Instanced"/> 的独立 Power 实例，
/// 因此两枚同类模块不会被并成一个总数 Power：各自保留类型、独立强化值与来源，各自有图标与悬停。
/// <see cref="Amount"/> 直接记录「本模块当前的生效数值」（攻击=回合末伤害，护盾=回合初格挡）。
///
/// 所有部署 / 替换 / 强化 / 触发 / 轮转 / 收回 / 查询都统一走 <see cref="Helpers.AuroraModuleController"/>，
/// 卡牌不直接改本 Power 的底层数值（架构 §6.1 末条）。被动触发（回合初 / 回合末）在各子类的
/// hook 里调用同一个 <see cref="TriggerAsync"/>，主动触发的卡牌也调它，保证两条路径行为一致。
/// </summary>
public abstract class AuroraModulePower : AuroraPower
{
    // 槽位上限不在此定义：基础槽 = AuroraModuleCapacityPower.BaseSlots(2)，
    // 有效上限见 AuroraModuleController.EffectiveCapacity / AuroraModuleCapacityPower.HardMaxSlots(3，经辅助肩架)。

    public abstract ModuleKind Kind { get; }

    /// <summary>刚部署时的基础生效值（攻击 4 / 护盾 5）。轮转类型时以「当前值 - 本类型基础值」保留强化量。</summary>
    public abstract int BaseValue { get; }

    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public int Value => (int)Amount;
    public override int DisplayAmount => Value;
    protected override bool IsVisibleInternal => Value > 0;

    protected override IEnumerable<MegaCrit.Sts2.Core.Localization.DynamicVars.DynamicVar> CanonicalVars =>
        [new MegaCrit.Sts2.Core.Localization.DynamicVars.DynamicVar("Value", 0m)];

    /// <summary>部署前预置生效值到 #{Value}（此时实例尚未上身，Amount 由 Apply 的 amount 参数写入）。</summary>
    public void PrimeValue(int value)
    {
        DynamicVars["Value"].BaseValue = value;
    }

    /// <summary>数值变动后，把描述用的 #{Value} 动态变量与显示同步到当前生效值。</summary>
    public void Refresh()
    {
        AssertMutable();
        DynamicVars["Value"].BaseValue = Value;
        InvokeDisplayAmountChanged();
    }

    /// <summary>触发本模块一次的实际效果。被动 hook 与主动触发共用此实现，保证一致。</summary>
    public abstract Task TriggerAsync(PlayerChoiceContext choiceContext);

    // 上身/重连后同步 #{Value} 与显示，防存档重连后文案与 Amount 脱节。
    public override async Task AfterApplied(Creature applier, CardModel cardSource)
    {
        Refresh();
        AuroraModuleVisualBridge.RequestRebuild(Owner);
        await Task.CompletedTask;
    }

    /// <summary>
    /// 引擎 <c>PowerCmd.Remove</c> 统一钩子。死亡清 Power / 驱散 / Controller 收回都会到这里；
    /// 须用入参 <paramref name="oldOwner"/>（此时 <see cref="AbstractModel.Owner"/> 可能已空）。
    /// </summary>
    public override async Task AfterRemoved(Creature oldOwner)
    {
        AuroraModuleVisualBridge.RequestRebuild(oldOwner);
        await Task.CompletedTask;
    }

    /// <summary>枚举某生物身上全部已部署模块实例（按部署顺序，最早在前）。</summary>
    public static List<AuroraModulePower> All(Creature creature) =>
        creature?.Powers.OfType<AuroraModulePower>().Where(m => m.Owner != null).ToList()
        ?? new List<AuroraModulePower>();
}
