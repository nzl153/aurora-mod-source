using System.Threading.Tasks;
using AuroraMod.AuroraCode.Powers;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace AuroraMod.AuroraCode.Helpers;

/// <summary>
/// 剑势统一服务（架构 §8.1 / §14）—— 唯一负责剑势的读取、获得与清空。
///
/// 剑势是非负整数，无玩法上限、不自然衰减、离开战斗随 <see cref="MomentumPower"/> 自动清零；
/// <b>只支持「清空全部」</b>，刻意不提供 Spend(N)/Consume(N)（架构 §8.1）。
/// 卡牌 / 遗物 / 能力一律走本服务，不直接改 <see cref="MomentumPower.Amount"/>。
/// 本服务自身不读取或修改热量：温区奖励等由卡牌显式组合（§8.1 末条）。
/// </summary>
internal static class AuroraMomentumService
{
    /// <summary>读取当前剑势（无 Power 时为 0）。</summary>
    public static int Get(Creature owner) => MomentumPower.Get(owner);

    /// <summary>
    /// 获得剑势。战斗已结束直接返回；amount=0 不创建 Power 直接返回；amount&lt;0 拒绝并记错；
    /// 累加溢出钳制到 <see cref="int.MaxValue"/> 并记错，绝不回绕。
    /// </summary>
    public static async Task GainAsync(PlayerChoiceContext ctx, Creature owner, int amount, CardModel source)
    {
        if (owner == null)
        {
            GD.PushError("[Aurora][Momentum] GainAsync 收到 null owner，已忽略。");
            return;
        }

        // 【战斗结束守卫】与 HeatPower.AddHeatAsync 对齐——收尾斩杀后再给剑势本场已无意义，
        // 且会在战斗结算期改动 Power 状态。守卫放在服务内部而非各调用点：
        // 攻击后给势的卡有 通量剑 / 协议破局 / 反制姿态，逐张打补丁必然漏（全面审核 P1）。
        // 只守「获得」方向：ClearAllAsync 返回的清空量被 一刀两断/归势成垒 用来算伤害，
        // 且只在打出牌时调用（此时战斗必在进行），不加守卫以免改变其返回语义。
        if (!CombatManager.Instance.IsInProgress)
        {
            return;
        }

        if (amount == 0)
        {
            return;
        }

        if (amount < 0)
        {
            GD.PushError($"[Aurora][Momentum] GainAsync 拒绝负数剑势 {amount}（剑势只能通过 ClearAll 清空，不能负向消费）。");
            return;
        }

        var current = Get(owner);
        long sum = (long)current + amount;
        if (sum > int.MaxValue)
        {
            GD.PushError($"[Aurora][Momentum] 剑势累加溢出（{current}+{amount}），已钳制到 int.MaxValue。");
            sum = int.MaxValue;
        }

        var delta = (int)sum - current;
        if (delta <= 0)
        {
            return;
        }

        if (owner.HasPower<MomentumPower>())
        {
            await PowerCmd.ModifyAmount(ctx, owner.GetPower<MomentumPower>(), delta, owner, source);
        }
        else
        {
            await AuroraPowerCmd.Apply<MomentumPower>(ctx, owner, delta, owner, source);
        }
    }

    /// <summary>
    /// 原子清空全部剑势：读取当前层数、移除 <see cref="MomentumPower"/>，返回实际清空的层数。
    /// 无剑势时返回 0。「一刀两断」等终结牌应先调用本方法取快照，再按快照结算伤害（§8.1.3）。
    /// </summary>
    public static async Task<int> ClearAllAsync(PlayerChoiceContext ctx, Creature owner, CardModel source)
    {
        var power = owner?.GetPower<MomentumPower>();
        if (power == null)
        {
            return 0;
        }

        var cleared = (int)power.Amount;
        await PowerCmd.Remove(power);
        return cleared;
    }
}
