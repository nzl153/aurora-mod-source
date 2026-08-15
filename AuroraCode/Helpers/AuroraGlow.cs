using System;
using AuroraMod.AuroraCode.Powers;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace AuroraMod.AuroraCode.Helpers;

/// <summary>
/// 卡面发光条件的统一取值口（工坊反馈 #1）。原版用 <c>ShouldGlowGoldInternal</c> 给「额外效果已可触发」的卡
/// 描金边（如 <c>Dismantle</c>：敌人身上有易伤 / <c>Spite</c>：本回合受过伤），奥萝拉整套是条件驱动却一处未接。
///
/// 这些属性会在**非战斗环境**被反复读取（牌库预览、卡池浏览、奖励界面），此时 canonical 实例上
/// <c>card.Owner</c> 会抛 <c>CanonicalModelException</c>。所以取生物一律走 <see cref="Self"/>：
/// 先用「<c>CombatState</c> 对 canonical 安全返回 null」挡掉绝大多数情况，再兜一层 try/catch。
/// 把这层安全收在一处，是为了避免 30 张卡各写各的空判——漏一张就是浏览牌库时崩溃。
///
/// 只给**二元条件**用。连续加成的卡（无月之刃、一刀断念等伤害随剑势线性增长）不该发光：
/// 它们没有「触没触发」之分，且 A14 已让卡面直接显示真实数值。
/// </summary>
internal static class AuroraGlow
{
    /// <summary>安全取本人生物；非战斗态 / canonical 实例一律返回 null（发光即关闭）。</summary>
    private static Creature Self(CardModel card)
    {
        try
        {
            return card?.CombatState == null ? null : card.Owner?.Creature;
        }
        catch (Exception)
        {
            // canonical 实例访问 Owner 会抛 CanonicalModelException——静默按「不发光」处理。
            // 这里不 PushError：牌库预览每帧都会走到，报错会刷屏。
            return null;
        }
    }

    /// <summary>当前处于连锁状态（打出前快照口径，与各卡 OnPlay 一致）。</summary>
    public static bool Chained(CardModel card)
    {
        var creature = Self(card);
        return creature != null && ChainPower.GetIsChained(creature);
    }

    /// <summary>本回合手动出牌数 ≥ <paramref name="threshold"/>。</summary>
    public static bool ChainAtLeast(CardModel card, int threshold)
    {
        var creature = Self(card);
        return creature != null && ChainPower.GetCount(creature) >= threshold;
    }

    /// <summary>本回合手动出牌数恰为 <paramref name="index"/>（序列位次卡专用）。</summary>
    public static bool ChainCountIs(CardModel card, int index)
    {
        var creature = Self(card);
        return creature != null && ChainPower.GetCount(creature) == index;
    }

    /// <summary>已连锁且模块满槽（阵列处决的强化档双条件）。</summary>
    public static bool ChainedWithFullModules(CardModel card)
    {
        var creature = Self(card);
        return creature != null
               && ChainPower.GetIsChained(creature)
               && AuroraModuleController.IsFull(creature);
    }

    /// <summary>当前热量处于指定区段。</summary>
    public static bool InZone(CardModel card, HeatPower.HeatZone zone)
    {
        var creature = Self(card);
        return creature != null && HeatPower.GetZone(creature) == zone;
    }
}
