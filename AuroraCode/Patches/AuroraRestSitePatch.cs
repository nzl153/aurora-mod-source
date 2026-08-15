using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.RestSite;

namespace AuroraMod.AuroraCode.Patches;

/// <summary>
/// 休息点（篝火）里把奥萝拉向右下微移，让她像倚坐在原木上（纯表现层）。
///
/// 房间在角色节点 _Ready 之后会把外层节点 Position 归零，故不能偏移外层；改为偏移内层 "ControlRoot"
/// （装角色 Spine 视觉的容器），不会被覆盖。仅奥萝拉生效，其它角色不动。
/// </summary>
[HarmonyPatch]
public static class AuroraRestSitePatch
{
    // 右下偏移量（Godot 2D：+X 右、+Y 下）。可调——嫌多嫌少改这里即可。
    private static readonly Vector2 Offset = new(60f, 38f);

    [HarmonyPatch(typeof(NRestSiteCharacter), "_Ready")]
    [HarmonyPostfix]
    public static void ReadyPostfix(NRestSiteCharacter __instance)
    {
        try
        {
            var entry = __instance?.Player?.Character?.Id.Entry;
            if (string.IsNullOrWhiteSpace(entry)
                || !entry.Contains("aurora", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var root = __instance.GetNodeOrNull<Control>("ControlRoot");
            if (root != null)
            {
                root.Position += Offset;
            }
        }
        catch
        {
            // 纯表现：绝不因它中断。
        }
    }
}
