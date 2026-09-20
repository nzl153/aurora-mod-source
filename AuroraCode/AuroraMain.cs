using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace AuroraMod.AuroraCode;

/// <summary>模组入口，注册全部 Harmony 补丁。</summary>
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
