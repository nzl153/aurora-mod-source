using System.Threading.Tasks;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace AuroraMod.AuroraCode.Relics;

/// <summary>
/// R-03 满载耦合器 / Full-Load Coupler（稀有，C 悬浮模块）—— 每场战斗第一次把模块槽由未满填满时：所有模块 +2 强化、各触发 1 次，随后积 3 热。
/// 挂 <see cref="IAuroraModuleFilledListener"/>，由 <see cref="AuroraModuleController.DeployAsync"/> 在「空槽普通部署恰好填满」时派发（满→满替换不派发；取消/失败不消耗）。
/// 每场一次：用隐藏 <see cref="AuroraCouplerUsedPower"/> 标记（属战斗态、会被序列化并在重连恢复），而非遗物私有 bool——故断线重连后不会二次触发。
/// 容量 2 时 2/2 触发、扩到 3 后须 3/3；本场已在 2/2 触发过则扩槽再满也不再触发。战斗态天然每场重置，无需 BeforeCombatStart 复位。
/// 顺序：部署及既有部署监听完成 → 全模块 +2 → 全模块各触发 1 次 → 战斗仍在进行则积 3 热。模块触发 Unpowered、不吃过载、不推连锁。
/// </summary>
public class FullLoadCoupler : AuroraRelic, IAuroraModuleFilledListener
{
    public override RelicRarity Rarity => RelicRarity.Rare;
    protected override string ArtName => "full_load_coupler";

    private const int Enhance = 2;
    private const int Heat = 3;

    public async Task OnModuleSlotsFilledAsync(PlayerChoiceContext ctx, Creature owner)
    {
        if (owner == null || owner != Owner?.Creature
            || AuroraCouplerUsedPower.IsUsed(owner) || !CombatManager.Instance.IsInProgress)
        {
            return;
        }

        // 隐藏标记置位（Apply(1)），重连恢复后 IsUsed 为真 → 不二次触发。
        await AuroraPowerCmd.Apply<AuroraCouplerUsedPower>(ctx, owner, 1, owner, null, silent: true);
        Flash();
        await AuroraModuleController.EnhanceAllAsync(ctx, owner, Enhance, null);
        await AuroraModuleController.TriggerAsync(ctx, owner);

        if (CombatManager.Instance.IsInProgress)
        {
            await HeatPower.AddHeatAsync(ctx, owner, Heat, null);
        }
    }
}
