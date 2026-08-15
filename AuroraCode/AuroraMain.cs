using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace AuroraMod.AuroraCode;

/// <summary>
/// 模组入口。游戏通过 <see cref="ModInitializerAttribute"/> 在加载时调用 <see cref="Initialize"/>，
/// 在此执行 Harmony.PatchAll() —— 能量球(AuroraEnergyCounterPatch)、卡框装饰(AuroraCardFramePatch)
/// 等所有 [HarmonyPatch] 都靠这一步注册，缺了就全部不生效。
/// </summary>
[ModInitializer(nameof(Initialize))]
public static class AuroraMain
{
    public const string ModId = "Aurora";

    public static void Initialize()
    {
        var harmony = new Harmony(ModId);
        harmony.PatchAll();
    }
}
