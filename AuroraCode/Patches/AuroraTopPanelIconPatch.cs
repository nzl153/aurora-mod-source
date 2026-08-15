using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Models;

namespace AuroraMod.AuroraCode.Patches;

/// <summary>
/// 顶栏角色头像走 <see cref="CharacterModel.Icon"/>（一个 Control 场景，
/// 路径由 SceneHelper 写死为 res://scenes/ui/character_icons/&lt;entry小写&gt;_icon.tscn），
/// mod 的 pck 想叠加进游戏原生 res://scenes/ 命名空间一直不可靠、加载失败还不报错。
/// 所以这里直接 patch getter：对奥萝拉现造一个带我们贴图的 TextureRect 返回，
/// 彻底绕开场景文件与 pck 叠加。IconTexture 一并 patch（多人/事件立绘等处用到）。
/// </summary>
// 类级 [HarmonyPatch] 不可省：本类 patch 两个不同方法(Icon/IconTexture)，
// 靠方法级 [HarmonyPatch] 各自指定目标；但缺了这个无参类级标记，PatchAll 会整类跳过。
[HarmonyPatch]
public static class AuroraTopPanelIconPatch
{
    private const string IconPath = "res://Aurora/Images/Charui/character_icon_aurora.png";

    // Icon 是 Control 场景，顶栏 NTopBarPortrait.AddChildSafely(player.Character.Icon) 用它。
    [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.Icon), MethodType.Getter)]
    [HarmonyPrefix]
    public static bool IconPrefix(CharacterModel __instance, ref Control __result)
    {
        GD.Print($"[Aurora] Icon getter called. entry='{__instance?.Id.Entry}' isAurora={IsAurora(__instance)} exists={ResourceLoader.Exists(IconPath)}");
        if (!IsAurora(__instance) || !ResourceLoader.Exists(IconPath))
            return true; // 非奥萝拉 → 走原逻辑

        var tex = PreloadManager.Cache.GetTexture2D(IconPath);
        var rect = new TextureRect
        {
            Texture = tex,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        rect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        __result = rect;
        return false; // 跳过原 getter
    }

    [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.IconTexture), MethodType.Getter)]
    [HarmonyPostfix]
    public static void IconTexturePostfix(CharacterModel __instance, ref Texture2D __result)
    {
        if (IsAurora(__instance) && ResourceLoader.Exists(IconPath))
            __result = PreloadManager.Cache.GetTexture2D(IconPath);
    }

    private static bool IsAurora(CharacterModel model)
    {
        var entry = model?.Id.Entry;
        return !string.IsNullOrWhiteSpace(entry) && entry.Contains("aurora", StringComparison.OrdinalIgnoreCase);
    }
}
