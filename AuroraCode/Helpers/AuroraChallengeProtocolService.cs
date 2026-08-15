using System;
using System.Threading.Tasks;
using AuroraMod.AuroraCode.Powers;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace AuroraMod.AuroraCode.Helpers;

/// <summary>
/// 挑战协议统一服务（架构 §8.2 / §14）—— 唯一负责按施加者施加 / 消费 / 查询挑战协议层数。
///
/// 每名奥萝拉对同一敌人最多 3 层（<see cref="ChallengeProtocolPower.MaxStacksPerApplier"/>），超出丢弃；
/// 收益按<b>实际新增层数</b>计算，不按尝试施加量（§8.2 / C08）。消费不经过人工制品。
/// 归属用 <see cref="AuroraPerApplier"/>（NetId），不自建 Creature 字典。
/// </summary>
internal static class AuroraChallengeProtocolService
{
    private const int Max = ChallengeProtocolPower.MaxStacksPerApplier;

    /// <summary>查询 applier 对 target 的当前协议层数（无实例=0）。</summary>
    public static int GetStacks(Creature target, Creature applier) =>
        AuroraPerApplier.FindInstance<ChallengeProtocolPower>(target, applier)?.Stacks ?? 0;

    /// <summary>
    /// 施加协议，返回<b>实际新增</b>层数（钳制到 3 层上限后的差值）。
    /// amount=0 返回 0 且不创建；amount&lt;0 拒绝记错；目标已满层返回 0。
    /// </summary>
    public static async Task<int> ApplyAsync(PlayerChoiceContext ctx, Creature target, Creature applier, int amount, CardModel source)
    {
        if (target == null || applier == null)
        {
            GD.PushError("[Aurora][Challenge] ApplyAsync 缺少 target/applier，已忽略（不误用他人层数）。");
            return 0;
        }

        if (amount == 0)
        {
            return 0;
        }

        if (amount < 0)
        {
            GD.PushError($"[Aurora][Challenge] ApplyAsync 拒绝负数 {amount}（施加只接受正整数）。");
            return 0;
        }

        var current = GetStacks(target, applier);
        var add = Math.Min(amount, Max - current);
        if (add <= 0)
        {
            return 0;
        }

        // InstancedPerApplier：引擎按 applier 路由到其自己的实例（无则新建，有则叠加）。
        await AuroraPowerCmd.Apply<ChallengeProtocolPower>(ctx, target, add, applier, source);
        return add;
    }

    /// <summary>消费 applier 对 target 的协议层数，返回实际消费量；减到 0 移除实例。不触发人工制品。</summary>
    public static async Task<int> ConsumeAsync(PlayerChoiceContext ctx, Creature target, Creature applier, int amount, CardModel source)
    {
        if (amount == 0)
        {
            return 0;
        }

        if (amount < 0)
        {
            GD.PushError($"[Aurora][Challenge] ConsumeAsync 拒绝负数 {amount}。");
            return 0;
        }

        var instance = AuroraPerApplier.FindInstance<ChallengeProtocolPower>(target, applier);
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

    /// <summary>清空 applier 对 target 的全部协议层数，返回实际清空量。</summary>
    public static async Task<int> ConsumeAllAsync(PlayerChoiceContext ctx, Creature target, Creature applier, CardModel source)
    {
        var instance = AuroraPerApplier.FindInstance<ChallengeProtocolPower>(target, applier);
        if (instance == null)
        {
            return 0;
        }

        var cleared = instance.Stacks;
        await PowerCmd.Remove(instance);
        return cleared;
    }
}
