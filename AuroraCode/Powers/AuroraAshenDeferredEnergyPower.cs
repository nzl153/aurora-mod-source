using System.Threading.Tasks;
using AuroraMod.AuroraCode.Helpers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// 灰烬复燃「待发能量」载体 —— 隐藏 Power，<b>权威=<see cref="AuroraPower.Amount"/></b>（= 下回合开始要补发的能量）。
/// 原先存 DynamicVar["DeferredEnergy"]，重连后丢失：若在回合末不可行动窗口触发复燃、能量登记到下回合发放，
/// 期间断线重连会丢掉这笔待发能量。改存序列化的 Amount。下回合开始由灰烬复燃 Power 读取、发放、清零。
/// </summary>
public sealed class AuroraAshenDeferredEnergyPower : AuroraPower
{
    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => false;

    public static int Pending(Creature creature) =>
        (int)(creature?.GetPowerAmount<AuroraAshenDeferredEnergyPower>() ?? 0);

    /// <summary>登记一笔待发能量（累加）。</summary>
    public static async Task AddAsync(PlayerChoiceContext ctx, Creature creature, int energy)
    {
        if (creature == null || energy <= 0)
        {
            return;
        }

        var power = creature.GetPower<AuroraAshenDeferredEnergyPower>();
        if (power == null)
        {
            await AuroraPowerCmd.Apply<AuroraAshenDeferredEnergyPower>(ctx, creature, energy, creature, null, silent: true);
            return;
        }

        power.AssertMutable();
        power.SetAmount((int)power.Amount + energy);
    }

    /// <summary>清空待发能量（发放后调用）。</summary>
    public void Clear()
    {
        if (Amount != 0)
        {
            AssertMutable();
            SetAmount(0);
        }
    }
}
