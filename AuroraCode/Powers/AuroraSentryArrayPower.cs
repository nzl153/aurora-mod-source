using System.Threading.Tasks;
using AuroraMod.AuroraCode.Helpers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// C-U01 哨戒阵列 Power（batch C-U，C 悬浮模块）—— 可见能力 Power + 成功部署监听。
/// <b>每回合第一次成功部署模块</b>时，新部署的模块获得 <see cref="Amount"/> 点强化并立即触发 1 次。
/// <see cref="Amount"/> = 每回合首次部署给予的总强化（多张合并累加）；触发次数固定 1 次、不随层数叠加。
/// 每回合门闩权威=<see cref="AuroraTurnGatePower"/>.BitSentryArray（Amount 位掩码，联机/重连一致；DV 不进同步故弃用 DV），门闩回合开始自清。
/// 只在 <see cref="AuroraModuleController.DeployAsync"/> 成功路径派发——模块轮转、满槽取消、部署失败都不触发。顺序固定：先强化新模块 → 读强化后数值 → 立即触发 1 次。
/// </summary>
public sealed class AuroraSentryArrayPower : AuroraPower, IAuroraDeployListener
{
    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => true;
    protected override string IconName => "sentry_array";

    public async Task OnModuleDeployedAsync(PlayerChoiceContext ctx, Creature owner, AuroraModulePower newModule)
    {
        if (owner != Owner || AuroraTurnGatePower.IsGated(owner, AuroraTurnGatePower.BitSentryArray) || Amount <= 0 || newModule == null)
        {
            return;
        }

        // 先置门闩防重入（权威进 Amount 位掩码，重连一致），再按固定顺序：强化新模块 → 立即触发 1 次。
        await AuroraTurnGatePower.MarkAsync(ctx, owner, AuroraTurnGatePower.BitSentryArray);

        Flash();
        await AuroraModuleController.EnhanceSpecificAsync(ctx, newModule, (int)Amount, null);
        await AuroraModuleController.TriggerInstanceAsync(ctx, newModule);
    }
}
