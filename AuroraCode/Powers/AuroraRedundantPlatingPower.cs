using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// 冗余装甲 Power（C 悬浮模块）—— 回合开始时保留一半格挡（向下取整）。
///
/// 【为什么不是「格挡不消失」】引擎只提供 <see cref="ShouldClearBlock"/> 这个布尔钩子，
/// 原版屏障/模糊都是「整个不清」。整个不清 + 装甲冲撞（伤害=格挡）＝ 一代最出名的无限组合，
/// 叠加奥萝拉的过载 ×1.25 与剑势底薪会比一代更夸张。
/// 保留一半则<b>数学上有界</b>：每回合产出 B，稳态收敛到 B + B/2 + B/4 + … = <b>2B</b>，
/// 滚得起来但滚不飞，装甲冲撞的天花板因此可控。
///
/// 【实现】照抄原版遗物 <c>SturdyClamp</c>（稳固夹钳）的做法：
/// <see cref="ShouldClearBlock"/> 拦住清除 → 引擎随即调用 <see cref="AfterPreventingBlockClear"/>，
/// 在那一刻用 <c>CreatureCmd.LoseBlock</c> 削到目标值。
/// <b>落点选这里而不是回合开始钩子</b>：这是引擎「本该清空格挡」的确切时刻，不用猜时机，
/// 也不会与其他回合开始效果抢顺序。
///
/// 【多张叠加】StackType=Single，多次打出不叠加效果（仍是保留一半），与原版屏障一致。
/// </summary>
public sealed class AuroraRedundantPlatingPower : AuroraPower
{
    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    protected override bool IsVisibleInternal => true;
    protected override string IconName => "redundant_plating";

    /// <summary>拦住「清空格挡」，仅对自己生效。</summary>
    public override bool ShouldClearBlock(Creature creature) => Owner != creature;

    /// <summary>
    /// 引擎在「本该清空格挡」的那一刻调用本方法。此时把格挡削到一半（向下取整）。
    /// 例：11 格挡 → 失去 6、保留 5；1 格挡 → 失去 1、保留 0。
    /// </summary>
    public override async Task AfterPreventingBlockClear(AbstractModel preventer, Creature creature)
    {
        if (this != preventer || creature != Owner)
        {
            return;
        }

        var block = creature.Block;
        if (block <= 0)
        {
            return;
        }

        var lose = block - (block / 2);   // 保留 floor(block/2)
        if (lose > 0)
        {
#if STS2_BETA
            // beta v0.111.0：LoseBlock 只剩 (ctx, target, amount, remover) 一个重载。
            // 本方法拿不到 PlayerChoiceContext，照抄游戏本体自己的做法：临时构造一个阻塞上下文。
            await CreatureCmd.LoseBlock(
                new MegaCrit.Sts2.Core.GameActions.Multiplayer.BlockingPlayerChoiceContext(),
                creature, lose, null);
#else
            await CreatureCmd.LoseBlock(creature, lose);
#endif
        }

        Flash();
    }
}
