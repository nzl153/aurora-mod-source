using AuroraMod.AuroraCode.Characters;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Visuals;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace AuroraMod.AuroraCode.Patches;

/// <summary>
/// 手牌悬停 → 热量柱显示「打出后会烧到哪」的预览段。
///
/// 补的是 <c>NPlayerHand.OnHolderFocused</c>（私有方法，Harmony 可补）——这正是原版自己通知
/// <c>HoveredModelTracker</c> 的那一处，语义最准，不必自己去猜鼠标状态。
///
/// 纯表现层：只读卡牌 DynamicVars 算增量（见 <see cref="AuroraHeatPreview"/>），
/// 不改任何状态、不消费 RNG、失败一律静默。
/// </summary>
[HarmonyPatch(typeof(NPlayerHand), "OnHolderFocused")]
public static class AuroraHeatPreviewShowPatch
{
    public static void Postfix(NHandCardHolder holder)
    {
        try
        {
            var card = holder?.CardModel;
            var creature = card?.Owner?.Creature;
            if (creature == null || creature.Player?.Character is not Aurora)
            {
                return;
            }

            // 条件卡取不到确定增量时返回 0，等于清除预览——宁可不显示，也不显示会骗人的段。
            AuroraHeatBarBridge.RequestPreview(creature, AuroraHeatPreview.ResolveDelta(card));
        }
        catch
        {
            // 悬停是高频路径，绝不因表现层报错影响操作。
        }
    }
}

/// <summary>鼠标移开手牌 → 清除预览段。与 <see cref="AuroraHeatPreviewShowPatch"/> 配对。</summary>
[HarmonyPatch(typeof(NPlayerHand), "OnHolderUnfocused")]
public static class AuroraHeatPreviewHidePatch
{
    public static void Postfix(NHandCardHolder holder)
    {
        try
        {
            var creature = holder?.CardModel?.Owner?.Creature;
            if (creature == null || creature.Player?.Character is not Aurora)
            {
                return;
            }

            AuroraHeatBarBridge.RequestPreview(creature, 0);
        }
        catch
        {
        }
    }
}
