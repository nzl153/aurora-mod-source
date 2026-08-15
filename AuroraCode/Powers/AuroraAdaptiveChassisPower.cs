using System.Threading.Tasks;
using AuroraMod.AuroraCode.Helpers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// C-R04 自适应底盘 Power—— 可见能力 Power + 轮转/替换监听。与哨戒阵列严格错位：
/// 哨戒奖励「向空槽部署」，本能力奖励「轮转 或 满槽成功替换」。每回合前 <see cref="Amount"/> 次（=总可生效次数，多张累加；升级每张 1→2）成功轮转/替换后，使操作后的模块获得 <c>Enhance</c> 点强化并立即触发 1 次。
/// 只在操作成功后消耗次数（取消/失败/无合法模块不消耗；向空槽的普通部署不属于替换、不触发）；同一次操作只获得一份强化并触发一次。
/// 每回合「已用次数」权威=<see cref="AuroraChassisUsedPower"/> 计数器（Amount，联机/重连一致；DV 不进同步故弃用 DV），回合开始自清。
/// </summary>
public sealed class AuroraAdaptiveChassisPower : AuroraPower, IAuroraModuleRotateListener
{
    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => true;
    protected override string IconName => "adaptive_chassis";

    private const int Enhance = 3;

    public async Task OnModuleRotatedAsync(PlayerChoiceContext ctx, Creature owner, AuroraModulePower resultModule)
    {
        if (owner != Owner || Amount <= 0 || resultModule == null
            || AuroraChassisUsedPower.Used(owner) >= (int)Amount)
        {
            return;
        }

        // 先记一次消耗防重入（权威进 Amount 计数器，重连一致），再按固定顺序：强化操作后的模块 → 立即触发 1 次。
        await AuroraChassisUsedPower.IncrementAsync(ctx, owner);

        Flash();
        await AuroraModuleController.EnhanceSpecificAsync(ctx, resultModule, Enhance, null);
        await AuroraModuleController.TriggerInstanceAsync(ctx, resultModule);
    }
}
