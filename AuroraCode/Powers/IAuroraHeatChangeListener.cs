using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// 热量变更原因。用于区分「换区」「实际散热」这类玩法事件与「过热清零/系统操作」。
/// </summary>
public enum HeatChangeReason
{
    /// <summary>正常积热或提升到某值（正向）。</summary>
    Add,
    /// <summary>卡牌/效果造成的正常降热（VentUpTo/Vent/负 delta 积热）。可触发冷却循环。</summary>
    Vent,
    /// <summary>过热结算强制清零。绝不触发换区/散热类效果。</summary>
    OverheatClear,
    /// <summary>战斗初始化、重连修正、战斗清理等系统操作。绝不触发玩法效果。</summary>
    System,
}

/// <summary>一次真实热量变更的快照。before==after 时不派发。</summary>
public readonly struct HeatChangeInfo
{
    public readonly int BeforeHeat;
    public readonly int AfterHeat;
    public readonly HeatPower.HeatZone BeforeZone;
    public readonly HeatPower.HeatZone AfterZone;
    public readonly HeatChangeReason Reason;
    public readonly CardModel Source;

    public HeatChangeInfo(int beforeHeat, int afterHeat, HeatPower.HeatZone beforeZone,
        HeatPower.HeatZone afterZone, HeatChangeReason reason, CardModel source)
    {
        BeforeHeat = beforeHeat;
        AfterHeat = afterHeat;
        BeforeZone = beforeZone;
        AfterZone = afterZone;
        Reason = reason;
        Source = source;
    }

    /// <summary>实际变化量（正=升温，负=降温）。</summary>
    public int Delta => AfterHeat - BeforeHeat;

    /// <summary>
    /// 换区（#60 相变护层）：区段确实不同，且原因是正常积热/散热（排除过热清零、系统操作）。
    /// 10+ 红线仍属 Overload，故 9→10、10→12 均非换区；3↔4、6↔7、10→6 才是。
    /// </summary>
    public bool ZoneChanged => BeforeZone != AfterZone && Reason is HeatChangeReason.Add or HeatChangeReason.Vent;

    /// <summary>实际散热（#61 冷却循环）：热量确实下降，且属卡牌/效果造成的正常降热（排除过热清零、系统操作）。</summary>
    public bool ActualVented => AfterHeat < BeforeHeat && Reason == HeatChangeReason.Vent;
}

/// <summary>
/// 热量变更监听接口。HeatPower 在每次<b>真实</b>热量变更完成后、于同步 <see cref="PlayerChoiceContext"/>
/// action 链内 await 派发一次（before==after 不派发）。用于 #60 相变护层（换区加盾）、#61 冷却循环（散热抽牌）。
/// 约定：快照派发；不消耗 RNG；绝不 fire-and-forget；过热清零/系统操作原因不触发玩法效果（见 <see cref="HeatChangeInfo"/>）。
/// </summary>
public interface IAuroraHeatChangeListener
{
    Task OnHeatChangedAsync(PlayerChoiceContext ctx, Creature owner, HeatChangeInfo info);
}
