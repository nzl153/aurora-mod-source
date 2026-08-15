using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace AuroraMod.AuroraCode.Helpers;

/// <summary>
/// 手牌位置快照—— 统一「本牌打出时是否位于手牌最右侧」的判定，供 #19 快速指令及后续
/// #48 步进斩 / #69 湮灭指令 / #71 队列覆写复用，禁止各卡自写一套位置判断。
///
/// 为什么需要它：卡牌进入 <c>OnPlay</c> 结算时通常已被 <c>AddDuringManualCardPlay</c> 移出手牌，
/// 无法在 OnPlay 里现读位置。故在 <c>CardModel.OnPlayWrapper</c> 的 prefix（早于移出手牌那步）捕获一次快照：
///   · 自动打出（isAutoPlay）→ 直接记 false；
///   · 手动打出 → 读该牌所有者当前手牌，判断本牌是否为视觉最右侧（= Hand.Cards 末元素；
///     Hand.Cards 顺序即视觉左→右，见 HandLayoutHelper 按 pile 顺序映射子节点）。
///
/// 快照按卡实例存于 ConditionalWeakTable（随卡 GC 自动回收，无需手动清理、无泄漏）；每次打出 prefix 覆写，
/// 故不会读到过期值。位置在「打出命令」阶段确定并随命令同步，联机双方不各自在动画阶段重算，无 RNG。
/// 「本次结算是否首段/是否自动」的进一步收窄由调用方叠加 cardPlay.IsFirstInSeries / IsAutoPlay 完成。
///
/// ⚠️ 若实机发现左右判反（最右侧误判成最左侧），把 <see cref="IsRightmostInHand"/> 里的末元素判断改成首元素即可。
/// </summary>
public static class AuroraHandPositionSnapshot
{
    private static readonly ConditionalWeakTable<CardModel, StrongBox<bool>> Table = new();

    /// <summary>在 OnPlayWrapper prefix 捕获一次：自动打出记 false，否则记「是否手牌最右侧」。</summary>
    public static void Capture(CardModel card, bool isAutoPlay)
    {
        if (card == null)
        {
            return;
        }

        var wasRightmost = !isAutoPlay && IsRightmostInHand(card);
        Table.AddOrUpdate(card, new StrongBox<bool>(wasRightmost));
    }

    /// <summary>读取本牌最近一次打出时的最右侧快照；无记录（如从非手牌区结算）返回 false。</summary>
    public static bool WasRightmost(CardModel card) =>
        card != null && Table.TryGetValue(card, out var box) && box.Value;

    private static bool IsRightmostInHand(CardModel card)
    {
        var hand = card.Owner?.PlayerCombatState?.Hand?.Cards;
        if (hand == null || hand.Count == 0)
        {
            return false;
        }

        // 手牌视觉左→右 = Cards 顺序，末元素为最右侧；唯一手牌天然是末元素（同时最左也最右）。
        return ReferenceEquals(hand[hand.Count - 1], card);
    }
}

/// <summary>
/// 把手牌位置快照接到出牌管线：<c>CardModel.OnPlayWrapper</c> 的 prefix 早于卡牌被移出手牌那步，
/// 是唯一能读到「打出瞬间手牌构成」的稳定时机。纯读取、不改结算、异常静默。
/// </summary>
[HarmonyPatch]
internal static class AuroraHandPositionPatch
{
    [HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
    [HarmonyPrefix]
    public static void Prefix(CardModel __instance, bool isAutoPlay)
    {
        try
        {
            AuroraHandPositionSnapshot.Capture(__instance, isAutoPlay);
        }
        catch
        {
            // 纯表现/读取：绝不因它中断出牌。
        }
    }
}
