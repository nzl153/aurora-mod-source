using System.Threading.Tasks;
using AuroraMod.AuroraCode.Helpers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// 已锁定过热伤害 / Locked Overheat Damage—— 隐藏 Power，<b>权威=<see cref="AuroraPower.Amount"/></b>。
/// 与 <see cref="AuroraOverheatPendingPower"/> 配对：Pending 建立时记录一份"已锁定伤害"，Pending 存在期间每次正向积热取
/// <c>LockedDamage = Max(LockedDamage, 当前预计伤害)</c>；<b>散热只降热量/换区，绝不降低本值、也不取消 Pending</b>。
/// 回合末/引爆结算时直接读本值作为过热伤害（不再按结算时热量事后抹债）。序列化战斗态→联机/重连一致；结算完成时随 Pending 一并移除。
/// 战斗态天然每场重置；胜利宽恕不结算 Pending → 本 Power 随战斗结束消失，整笔债务免除。
/// </summary>
public sealed class AuroraLockedOverheatDamagePower : AuroraPower
{
    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => false;

    public static int Get(Creature creature) =>
        (int)(creature?.GetPowerAmount<AuroraLockedOverheatDamagePower>() ?? 0);

    /// <summary>把已锁定伤害提升到至少 <paramref name="value"/>（只升不降）。Power 不存在则 Apply(value)，否则 SetAmount(Max)。
    /// 更新后同步 <see cref="AuroraOverheatPendingPower"/> 的展示用 LockedDamage。</summary>
    public static async Task SetAtLeastAsync(PlayerChoiceContext ctx, Creature creature, int value)
    {
        if (creature == null || value <= 0)
        {
            return;
        }

        var power = creature.GetPower<AuroraLockedOverheatDamagePower>();
        int finalVal;
        if (power == null)
        {
            await AuroraPowerCmd.Apply<AuroraLockedOverheatDamagePower>(ctx, creature, value, creature, null, silent: true);
            finalVal = value;
        }
        else if (value > (int)power.Amount)
        {
            power.AssertMutable();
            power.SetAmount(value);
            finalVal = value;
        }
        else
        {
            finalVal = (int)power.Amount;
        }

        creature.GetPower<AuroraOverheatPendingPower>()?.SetLockedDamageDisplay(finalVal);
    }
}
