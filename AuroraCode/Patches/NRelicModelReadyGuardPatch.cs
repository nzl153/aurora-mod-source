using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Relics;

namespace AuroraMod.AuroraCode.Patches;

/// <summary>
/// 兼容性加固：<see cref="NRelic"/> 的 <c>Model</c> getter 在 Model 尚未赋值时会
/// <c>throw new InvalidOperationException("Model was accessed before it was set.")</c>。
///
/// 经 BaseLib 添加的 modded 遗物（奥萝拉「预载」遗物等）流程是「先入树 <c>_Ready</c>、后赋 Model」
/// （见崩溃堆栈 <c>NRelicInventory.Add_Patch1 → AddChildSafely → NRelic._Ready</c>）。若某第三方补丁
/// 在 <c>NRelic._Ready</c> 的 postfix 里<b>无条件读</b> <c>__instance.Model</c>——例如
/// OrchisNecrobinderSkinMod 的 <c>NRelicReadyPatch.Postfix</c> 首行就是 <c>RelicModel model = __instance.Model;</c>——
/// 便会在此刻抛异常，打断遗物添加的 async 流程，导致<b>事件选项（给预载遗物）/宝箱拿遗物直接卡死</b>。
/// 该问题对任何 modded 遗物普遍存在（非奥萝拉独有），根因是 getter「未就绪即抛」而第三方补丁读取时未防空。
///
/// 兜底方式：用 Harmony <b>Finalizer</b>，仅当 getter 因「未就绪」抛 <see cref="InvalidOperationException"/> 时
/// 吞掉异常、以 <c>null</c> 兜底返回。
///   · 正常路径（Model 已就绪）getter 不抛异常，Finalizer 直接放行、<b>零开销</b>；
///   · 返回 <c>null</c> 对正确调用者无影响（他们都在赋值后才读，NRelic 自身访问前亦判 <c>_model != null</c>）；
///   · 对「过早读」的第三方补丁，它们本就带 <c>if (model != null)</c> 判断（Orchis 亦然）→ 安全跳过。
/// 纯兼容层：不改伤害/结算/netcode。
/// </summary>
[HarmonyPatch(typeof(NRelic), nameof(NRelic.Model), MethodType.Getter)]
public static class NRelicModelReadyGuardPatch
{
    public static Exception Finalizer(Exception __exception, ref RelicModel __result)
    {
        if (__exception is InvalidOperationException)
        {
            // 「未就绪即抛」→ 以 null 兜底，避免打断遗物添加流程（第三方补丁自带 null 判断，安全）。
            __result = null;
            return null;
        }

        // 其它异常照旧上抛，不吞。
        return __exception;
    }
}
