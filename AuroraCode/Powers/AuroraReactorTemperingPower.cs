using System.Threading.Tasks;
using AuroraMod.AuroraCode.Helpers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// H-R02 炉心淬锋 Power—— 可见能力 + <see cref="IAuroraOverheatResolvedListener"/>。
/// 每当一笔过热<b>完整结算后</b>且仍存活，获得 <see cref="Amount"/> 点剑势（多张线性叠加）。
/// 只在真正结算完成派发（引爆/回合末结算存活后都算；创建 Pending、红线积热、重复越线、散热、胜利宽恕、过热致死均不触发）。
/// 引爆后获得的剑势可当回合继续使用；回合末结算获得的剑势保留到下一回合。A+B 桥梁：把承担过热后的残局转化为下一次剑势倾泻资源。
/// </summary>
public sealed class AuroraReactorTemperingPower : AuroraPower, IAuroraOverheatResolvedListener
{
    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => true;
    protected override string IconName => "reactor_tempering";

    public async Task OnOverheatResolvedAsync(PlayerChoiceContext ctx, Creature owner, int overheatIndex)
    {
        if (owner != Owner || Amount <= 0)
        {
            return;
        }

        Flash();
        await AuroraMomentumService.GainAsync(ctx, owner, (int)Amount, null);
    }
}
