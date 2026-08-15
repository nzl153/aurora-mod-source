using System.Threading.Tasks;
using AuroraMod.AuroraCode.Helpers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// 冷却循环「升级触发格挡」累计 —— 隐藏 Power，<b>权威=<see cref="AuroraPower.Amount"/></b>（= 首次散热时该给的总格挡）。
/// 原先存 DynamicVar["TriggerBlock"]，重连后丢失。战斗内随升级版叠加而累加；主抽牌数仍在可见 Power 的 Amount。
/// 可见 Power 的 {TriggerBlock} DV 仅作展示镜像、从本 Power 派生。
/// </summary>
public sealed class AuroraCoolingCycleBonusPower : AuroraPower
{
    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => false;

    public static int Block(Creature creature) =>
        (int)(creature?.GetPowerAmount<AuroraCoolingCycleBonusPower>() ?? 0);

    public static async Task AddAsync(PlayerChoiceContext ctx, Creature creature, int block)
    {
        if (creature == null || block <= 0)
        {
            return;
        }

        var power = creature.GetPower<AuroraCoolingCycleBonusPower>();
        if (power == null)
        {
            await AuroraPowerCmd.Apply<AuroraCoolingCycleBonusPower>(ctx, creature, block, creature, null, silent: true);
            return;
        }

        power.AssertMutable();
        power.SetAmount((int)power.Amount + block);
    }
}
