using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace AuroraMod.AuroraCode.Patches;

/// <summary>
/// 奥萝拉死亡动画整体下移一点点——die 这段 Spine 的站位偏高（弟弟实测反馈），
/// 但 idle 站姿是好的，所以不能改 <c>AuroraVisual.tscn</c> 的整体偏移（那会连带压低 idle）。
///
/// 做法：postfix <see cref="NCreature.StartDeathAnim"/>，仅当死者是奥萝拉时，把 <c>Body</c>（当前视觉根 Node2D）
/// 的局部 Y 往下推 <see cref="DownOffset"/> 像素。死亡是终态、不再切回其它动画，无需还原；Spine 在 Body 内部
/// 驱动骨骼，这层节点偏移叠加其上不冲突。纯表现、不碰 Spine 文件、不改玩法，异常静默。
/// 嫌多/少改 <see cref="DownOffset"/> 一个数即可。
/// </summary>
[HarmonyPatch(typeof(NCreature), nameof(NCreature.StartDeathAnim))]
public static class AuroraDeathOffsetPatch
{
    // 下移像素（Godot 2D +Y 向下）。小幅修正，别过头。
    private const float DownOffset = 23f;

    [HarmonyPostfix]
    public static void Postfix(NCreature __instance)
    {
        try
        {
            var entry = __instance?.Entity?.Player?.Character?.Id.Entry;
            if (string.IsNullOrWhiteSpace(entry)
                || !entry.Contains("aurora", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var body = __instance.Body;
            if (body == null)
            {
                return;
            }

            body.Position += new Vector2(0f, DownOffset);
        }
        catch
        {
            // 纯表现：绝不因它中断死亡结算。
        }
    }
}
