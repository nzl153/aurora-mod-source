using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Models;

namespace AuroraMod.AuroraCode.Patches;

/// <summary>
/// 为奥萝拉覆盖多人遗物争夺界面的指向与石头剪刀布手势。
/// 固定路径绕开自定义角色完整 Entry（AURORAMOD-AURORA）导致的原生路径拼接问题。
/// </summary>
[HarmonyPatch]
public static class AuroraHandTexturePatch
{
    [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.ArmPointingTexture), MethodType.Getter)]
    [HarmonyPostfix]
    public static void ArmPointingTexturePostfix(CharacterModel __instance, ref Texture2D __result)
    {
        __result = ResolveHandTexture(__instance, "point", __result);
    }

    [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.ArmRockTexture), MethodType.Getter)]
    [HarmonyPostfix]
    public static void ArmRockTexturePostfix(CharacterModel __instance, ref Texture2D __result)
    {
        __result = ResolveHandTexture(__instance, "rock", __result);
    }

    [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.ArmPaperTexture), MethodType.Getter)]
    [HarmonyPostfix]
    public static void ArmPaperTexturePostfix(CharacterModel __instance, ref Texture2D __result)
    {
        __result = ResolveHandTexture(__instance, "paper", __result);
    }

    [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.ArmScissorsTexture), MethodType.Getter)]
    [HarmonyPostfix]
    public static void ArmScissorsTexturePostfix(CharacterModel __instance, ref Texture2D __result)
    {
        __result = ResolveHandTexture(__instance, "scissors", __result);
    }

    private static Texture2D ResolveHandTexture(CharacterModel model, string suffix, Texture2D fallback)
    {
        var entry = model?.Id.Entry;
        if (string.IsNullOrWhiteSpace(entry)
            || !entry.Contains("aurora", StringComparison.OrdinalIgnoreCase))
        {
            return fallback;
        }

        var path = $"res://images/ui/hands/multiplayer_hand_aurora_{suffix}.png";
        return ResourceLoader.Exists(path)
            ? PreloadManager.Cache.GetTexture2D(path)
            : fallback;
    }
}
