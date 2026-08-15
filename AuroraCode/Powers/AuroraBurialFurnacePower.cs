using System.Threading.Tasks;
using AuroraMod.AuroraCode.Helpers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// A-R02 葬炉 Power（A 过热稀有）—— 可见能力 + <see cref="IAuroraOverheatResolvedListener"/>。
/// 每当一笔过热<b>完整结算后</b>且角色仍存活，获得 <see cref="Amount"/> 点力量（多张副本累加 Amount，线性叠加）。
/// 只在真正结算完成派发（引爆/回合末结算存活后都算；创建 Pending、红线积热、重复越线、散热、胜利宽恕、过热致死均不触发）。
/// 一笔 Pending 只触发一次（结算中心保证）。力量为战斗内标准力量、不跨战斗；引爆后获得的力量可用于同回合后续攻击。
/// </summary>
public sealed class AuroraBurialFurnacePower : AuroraPower, IAuroraOverheatResolvedListener
{
    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => true;
    protected override string IconName => "burial_furnace";

    public async Task OnOverheatResolvedAsync(PlayerChoiceContext ctx, Creature owner, int overheatIndex)
    {
        if (owner != Owner || Amount <= 0)
        {
            return;
        }

        Flash();
        await AuroraPowerCmd.Apply<StrengthPower>(ctx, owner, (int)Amount, owner, null, silent: true);
    }
}
