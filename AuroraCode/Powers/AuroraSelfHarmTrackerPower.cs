using System.Threading.Tasks;
using AuroraMod.AuroraCode.Helpers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// 自损追踪器（A 过热稀有地基）—— 隐藏计数 Power：累计本场战斗中因奥萝拉<b>自己的牌或过热</b>实际失去的生命
/// （生命条真实下降，经格挡/减伤/灰烬后的净值）。供极限断裂读取。
/// 懒创建：从第一次自损起累加（任何回合的自损都计，满足「战斗开始即记录」——因为从第 1 回合的自损就会创建并累加）。
/// 只记本人；战斗结束随 Power 清零、不跨战斗。敌人攻击、模块伤害、最大生命变化均不经此累计（只有 <see cref="AuroraSelfHarm"/> 写入）。
/// </summary>
public sealed class AuroraSelfHarmTrackerPower : AuroraPower
{
    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => false;

    public static int Get(Creature creature) => (int)(creature?.GetPowerAmount<AuroraSelfHarmTrackerPower>() ?? 0);

    /// <summary>累加一次实际自损掉血量（amount≤0 忽略）。仅 <see cref="AuroraSelfHarm.ApplyAsync"/> 调用。</summary>
    public static async Task RecordAsync(PlayerChoiceContext ctx, Creature creature, int amount)
    {
        if (creature == null || amount <= 0)
        {
            return;
        }

        await AuroraPowerCmd.Apply<AuroraSelfHarmTrackerPower>(ctx, creature, amount, creature, null, silent: true);
    }
}
