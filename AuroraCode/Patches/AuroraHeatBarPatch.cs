using AuroraMod.AuroraCode.Characters;
using AuroraMod.AuroraCode.Visuals;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace AuroraMod.AuroraCode.Patches;

/// <summary>
/// 奥萝拉 NCreature 就绪后挂上热量竖柱。只给本地玩家建（判定收在 <see cref="AuroraHeatBar.EnsureOn"/>）。
/// 与 <see cref="AuroraModuleVisualPatch"/> 补同一个方法，Harmony 允许多个 Postfix 并存，互不影响。
/// </summary>
[HarmonyPatch(typeof(NCreature), nameof(NCreature._Ready))]
public static class AuroraHeatBarPatch
{
    public static void Postfix(NCreature __instance)
    {
        try
        {
            if (__instance?.Entity?.Player?.Character is not Aurora)
            {
                return;
            }

            AuroraHeatBar.EnsureOn(__instance);
        }
        catch
        {
            // 表现层失败不得打断战斗初始化。
        }
    }
}
