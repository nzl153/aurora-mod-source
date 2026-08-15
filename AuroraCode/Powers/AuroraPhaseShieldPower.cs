using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// #60 相变护层 Power—— 可见能力 Power + 热量变更监听。<b>每回合第一次换区</b>时获得 <see cref="Amount"/> 点格挡。
/// 多张合并累加 Amount（基础 4 + 升级 6 = 每次 10）。每回合门闩权威=<see cref="AuroraTurnGatePower"/>.BitPhaseShield（Amount 位掩码，联机/重连一致；DV 不进同步故弃用 DV），门闩回合开始自清。
/// 本 Power 只在能力打出后才存在，故打出前的换区不预先消耗门闩；打出后的下一次换区仍可触发。9→10、10→12（红线内）非换区不触发；过热清零/系统操作不触发（见 <see cref="HeatChangeInfo"/>）。
/// </summary>
public sealed class AuroraPhaseShieldPower : AuroraPower, IAuroraHeatChangeListener
{
    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => true;
    protected override string IconName => "phase_shield";

    public async Task OnHeatChangedAsync(PlayerChoiceContext ctx, Creature owner, HeatChangeInfo info)
    {
        if (owner != Owner || AuroraTurnGatePower.IsGated(owner, AuroraTurnGatePower.BitPhaseShield) || Amount <= 0 || !info.ZoneChanged)
        {
            return;
        }

        // 先置门闩防重入（权威进 Amount 位掩码，重连一致），再获得格挡。
        await AuroraTurnGatePower.MarkAsync(ctx, owner, AuroraTurnGatePower.BitPhaseShield);

        Flash();
        await CreatureCmd.GainBlock(owner, (int)Amount, ValueProp.Unpowered, null);
    }
}
