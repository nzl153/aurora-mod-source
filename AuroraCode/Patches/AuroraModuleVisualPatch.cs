using AuroraMod.AuroraCode.Characters;
using AuroraMod.AuroraCode.Visuals;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace AuroraMod.AuroraCode.Patches;

/// <summary>
/// 奥萝拉 NCreature 就绪后挂上模块视觉管理器；不改 Spine，不碰 Orb 玩法。
/// </summary>
[HarmonyPatch(typeof(NCreature), nameof(NCreature._Ready))]
public static class AuroraModuleVisualPatch
{
    public static void Postfix(NCreature __instance)
    {
        try
        {
            if (__instance?.Entity?.Player?.Character is not Aurora)
            {
                return;
            }

            AuroraModuleVisualManager.EnsureOn(__instance);
        }
        catch
        {
            // 表现层失败不得打断战斗初始化。
        }
    }
}
