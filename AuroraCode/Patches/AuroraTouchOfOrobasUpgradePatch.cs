using AuroraMod.AuroraCode.Relics;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace AuroraMod.AuroraCode.Patches;

/// <summary>
/// 先古之民「奥罗巴斯之触」升级起始遗物时，把「散热核心」升级为「强化散热核心」，
/// 而非回退到通用的头环。照 Kakarot 的 KakarotTouchOfOrobasUpgradePatch。
/// </summary>
[HarmonyPatch(typeof(TouchOfOrobas), nameof(TouchOfOrobas.GetUpgradedStarterRelic))]
public static class AuroraTouchOfOrobasUpgradePatch
{
    public static void Postfix(RelicModel starterRelic, ref RelicModel __result)
    {
        if (starterRelic.Id == ModelDb.Relic<HeatDissipationCore>().Id)
        {
            __result = ModelDb.Relic<HeatDissipationCorePlus>();
        }
    }
}
