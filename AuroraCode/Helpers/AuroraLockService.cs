using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuroraMod.AuroraCode.Powers;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace AuroraMod.AuroraCode.Helpers;

/// <summary>
/// 锁定统一服务（架构 §9 / §9.1 / §14）—— 唯一负责按施加者施加 / 消费 / 查询锁定层数，
/// 以及攻击模块的「优先打本人锁定层数最高者」选敌。
///
/// 每名奥萝拉对同一敌人最多 6 层（<see cref="LockPower.MaxStacksPerApplier"/>），超出丢弃。
/// 锁定是 Debuff，施加会被人工制品阻挡：<see cref="ApplyAsync"/> 用施加前后差值返回<b>实际新增</b>层数，
/// 被人工制品挡掉时返回 0。归属用 <see cref="AuroraPerApplier"/>（NetId）。
/// </summary>
internal static class AuroraLockService
{
    private const int Max = LockPower.MaxStacksPerApplier;

    /// <summary>查询 applier 对 target 的当前锁定层数（无实例=0）。</summary>
    public static int GetStacks(Creature target, Creature applier) =>
        AuroraPerApplier.FindInstance<LockPower>(target, applier)?.Stacks ?? 0;

    /// <summary>
    /// 施加锁定，返回<b>实际新增</b>层数（施加前后差值，故人工制品阻挡时为 0）。
    /// amount=0 返回 0 且不创建；amount&lt;0 拒绝记错；目标已满 6 层返回 0。
    /// </summary>
    public static async Task<int> ApplyAsync(PlayerChoiceContext ctx, Creature target, Creature applier, int amount, CardModel source)
    {
        if (target == null || applier == null)
        {
            GD.PushError("[Aurora][Lock] ApplyAsync 缺少 target/applier，已忽略（不误用他人层数）。");
            return 0;
        }

        if (amount == 0)
        {
            return 0;
        }

        if (amount < 0)
        {
            GD.PushError($"[Aurora][Lock] ApplyAsync 拒绝负数 {amount}。");
            return 0;
        }

        var before = GetStacks(target, applier);
        var add = Math.Min(amount, Max - before);
        if (add <= 0)
        {
            return 0;
        }

        // Debuff：走人工制品通道；被挡掉则前后差为 0。
        await AuroraPowerCmd.Apply<LockPower>(ctx, target, add, applier, source);
        return Math.Max(GetStacks(target, applier) - before, 0);
    }

    /// <summary>消费 applier 对 target 的锁定层数，返回实际消费量；减到 0 移除实例。</summary>
    public static async Task<int> ConsumeAsync(PlayerChoiceContext ctx, Creature target, Creature applier, int amount, CardModel source)
    {
        if (amount == 0)
        {
            return 0;
        }

        if (amount < 0)
        {
            GD.PushError($"[Aurora][Lock] ConsumeAsync 拒绝负数 {amount}。");
            return 0;
        }

        var instance = AuroraPerApplier.FindInstance<LockPower>(target, applier);
        if (instance == null)
        {
            return 0;
        }

        var actual = Math.Min(amount, instance.Stacks);
        if (actual <= 0)
        {
            return 0;
        }

        if (instance.Stacks - actual <= 0)
        {
            await PowerCmd.Remove(instance);
        }
        else
        {
            await PowerCmd.ModifyAmount(ctx, instance, -actual, applier, source);
        }

        return actual;
    }

    /// <summary>
    /// 攻击模块选敌（架构 §9.1）：只看模块拥有者<b>本人</b>施加的锁定，取层数最高的存活可命中敌人；
    /// 唯一最高直接选、不耗 RNG；并列最高对候选调用一次 CombatTargets RNG；
    /// 本人无正层锁定时，退回对全部敌人调用一次 CombatTargets RNG。完全忽略队友的锁定。
    /// </summary>
    public static Creature SelectAttackModuleTarget(Creature owner, IReadOnlyList<Creature> enemies)
    {
        if (owner == null || enemies == null)
        {
            return null;
        }

        // 保持 HittableEnemies 的稳定顺序，只留存活可命中者。
        var candidates = enemies.Where(e => e != null && e.IsAlive).ToList();
        if (candidates.Count == 0)
        {
            return null;
        }

        var max = 0;
        foreach (var e in candidates)
        {
            var s = GetStacks(e, owner);
            if (s > max)
            {
                max = s;
            }
        }

        var rng = owner.Player.RunState.Rng.CombatTargets;

        if (max <= 0)
        {
            // 本人没有任何正层锁定：保持原逻辑，对全部敌人一次 RNG。
            return rng.NextItem(candidates);
        }

        var top = candidates.Where(e => GetStacks(e, owner) == max).ToList();
        // 唯一最高者直接选择，不消耗 RNG；并列最高对候选一次 RNG。
        return top.Count == 1 ? top[0] : rng.NextItem(top);
    }
}
