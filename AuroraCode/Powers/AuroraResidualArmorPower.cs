using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// #33 余热装甲 Power—— 可见能力 Power + 过热监听。<b>每回合第一次</b>过热时，在承受过热伤害<b>之前</b>
/// 获得 <see cref="Amount"/> 点格挡。多张余热装甲合并为一个 Power 并累加 Amount（=每回合首次过热获得的总格挡）。
/// 每回合门闩权威=<see cref="AuroraTurnGatePower"/>.BitResidualArmor（Amount 位掩码，联机/重连一致；DV 不进同步故弃用 DV），门闩由该 Power 回合开始自清。
/// 不阻止过热计数、过热伤害、清热或宕机。若能力在本回合已过热之后才打出，因监听器按快照派发，本轮不追溯触发，允许下一次过热触发一次（符合设计）。
/// </summary>
public sealed class AuroraResidualArmorPower : AuroraPower, IAuroraOverheatListener
{
    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => true;
    protected override string IconName => "residual_armor";

    public async Task OnOverheatAsync(PlayerChoiceContext ctx, Creature owner, int overheatIndex)
    {
        if (owner != Owner || AuroraTurnGatePower.IsGated(owner, AuroraTurnGatePower.BitResidualArmor) || Amount <= 0)
        {
            return;
        }

        // 先置门闩防重入（权威进 Amount 位掩码，重连一致），再获得格挡。
        await AuroraTurnGatePower.MarkAsync(ctx, owner, AuroraTurnGatePower.BitResidualArmor);

        Flash();
        // 过热伤害之前获得格挡（监听器约定：回调在受伤前）。
        await CreatureCmd.GainBlock(owner, (int)Amount, ValueProp.Unpowered, null);
    }
}
