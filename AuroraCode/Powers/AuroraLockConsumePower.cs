using System.Threading.Tasks;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Patches;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// 锁定消费器（架构 §9）—— 挂在每名奥萝拉身上的隐藏 Power，战斗开始由起始遗物挂载。
///
/// 只在<b>真实伤害结算</b>后触发（<see cref="AfterDamageGiven"/> 不在预览路径跑），
/// 与 <see cref="AuroraLockDamagePatch"/> 的纯 +2 用同一判定：当本奥萝拉(Owner=dealer)用
/// powered attack 或攻击模块命中、且该段真实整数伤害落地（TotalDamage&gt;0；完全格挡的正整数伤害也计入）时，消费自己对该目标的 1 层锁定。
/// 消费只减层、不产生二次伤害、不递归触发命中监听器；队友的攻击不会消费本人锁定（dealer!=Owner 直接返回）。
/// </summary>
public sealed class AuroraLockConsumePower : AuroraPower
{
    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;
    protected override bool IsVisibleInternal => false;

    public static int Get(Creature creature) => (int)(creature?.GetPowerAmount<AuroraLockConsumePower>() ?? 0);

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature dealer, DamageResult result, ValueProp props, Creature target, CardModel cardSource)
    {
        // 只有本奥萝拉亲自造成的、可消费来源（powered attack / 攻击模块）才消费自己的锁定。
        if (dealer != Owner || target == null || !AuroraLockDamagePatch.IsLockConsumingSource(props))
        {
            return;
        }

        // 落地判据 = 真实整数伤害段：DamageResult.TotalDamage(=BlockedDamage+UnblockedDamage) > 0。
        // 正整数伤害即使被完全格挡也计入 BlockedDamage → TotalDamage>0，仍消费（L04）；
        // 乘区后取整为 0 的碎段（如 0.5）即便撞已有格挡令 WasFullyBlocked=true，TotalDamage 仍为 0 → 不误消费。
        // 不共享数值 Postfix 的 floor 门控，只信整数化后的真实伤害。
        var landed = result != null && result.TotalDamage > 0;
        if (!landed)
        {
            return;
        }

        await AuroraLockService.ConsumeAsync(choiceContext, target, Owner, 1, null);
    }
}
