using System.Collections.Generic;
using System.Threading.Tasks;
using AuroraMod.AuroraCode.Helpers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// H-R01 三相指令核 Power—— 可见能力 Power + 连锁激活监听。跨流派构筑核心：
/// 每回合连锁激活（第 3 张手动牌完成结算越阈那一刻）时，按<b>当时热区</b>触发一个分支（不连续补发跨过的区段）：
/// 冷区→获得 <c>Momentum</c> 剑势(B)；温区→强化最低强化的一枚模块 <c>Enhance</c> 点(C)；过载区→所有模块各触发 1 次(C)。
/// 温/过载区无模块时该分支无效果、不提供后备。多层：冷区剑势按 <see cref="Amount"/> 累加；温区每层各强化一枚最弱者；过载区每层各触发一轮。
/// 触发来自 <see cref="IAuroraChainListener"/> 的越线派发（每回合天然至多一次）；若本能力自身是第 3 张手动牌，
/// 先成功上身、随后连锁激活 → 本回合可立即触发一次。
/// </summary>
public sealed class AuroraTriPhaseCommandCorePower : AuroraPower, IAuroraChainListener
{
    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => true;
    protected override string IconName => "tri_phase_command_core";

    // 由 private const 改为 DynamicVar——原先代码与文案两头都硬编码 4 / 2，
    // 改代码时文案不会跟着变（剑势共鸣阈值 6→10 就是这么漏掉的）。改成变量后本地化用占位符自动追踪。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("MomentumPerStack", 4m),
        new DynamicVar("EnhancePerStack", 2m),
    ];

    public async Task OnChainActivatedAsync(PlayerChoiceContext ctx, Creature owner)
    {
        if (owner != Owner || Amount <= 0)
        {
            return;
        }

        var zone = HeatPower.GetZone(owner);
        Flash();

        // 每层各执行一次对应分支（冷区剑势合并为一次获得；温区/过载区逐层各操作一次）。
        var stacks = (int)Amount;

        if (zone == HeatPower.HeatZone.Cold)
        {
            await AuroraMomentumService.GainAsync(ctx, owner, stacks * (int)DynamicVars["MomentumPerStack"].BaseValue, null);
            return;
        }

        if (zone == HeatPower.HeatZone.Warm)
        {
            for (var i = 0; i < stacks; i++)
            {
                // 每层重新选当前强化最少的一枚强化 2；无模块则无效果、不后备。
                await AuroraModuleController.EnhanceLeastEnhancedAsync(ctx, owner, (int)DynamicVars["EnhancePerStack"].BaseValue, null);
            }

            return;
        }

        // 过载区：每层使所有模块各触发 1 次。
        for (var i = 0; i < stacks; i++)
        {
            await AuroraModuleController.TriggerAsync(ctx, owner);
        }
    }
}
