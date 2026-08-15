using System;
using AuroraMod.AuroraCode.Cards.Token;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace AuroraMod.AuroraCode.Patches;

/// <summary>
/// Aurora 卡框自定义（钩 <see cref="NCard.Reload"/>）：把卡的 CardContainer/Frame(TextureRect) 贴图
/// 替换为定制紫黑机甲金属框（仅对 Aurora 卡）。贴图 1058×1487，与原框 300:422 同比例，不会拉伸。
///
/// 弃用了旧的程序化描边(_Draw 在网格/详情不同缩放下坐标错乱)；贴图替换是原生属性、无绘制错乱。
/// 非 Aurora 卡不触碰（游戏 Reload 每次会按卡自身重设 Frame 贴图，池化复用安全）。
/// 满槽模块令牌挂原生 TokenCardPool，须按类型识别，不能只看 pool id。
/// </summary>
[HarmonyPatch(typeof(NCard), "Reload")]
public static class AuroraCardFramePatch
{
    private const string LegacyOverlayName = "AuroraFrameDeco";
    private const string FramePath = "res://Aurora/Images/CardFrame/aurora_frame.png";

    public static void Postfix(NCard __instance)
    {
        try
        {
            // 清掉历史遗留的程序化装饰层（若有）。
            __instance.FindChild(LegacyOverlayName, recursive: true, owned: false)?.QueueFree();

            if (!IsAurora(__instance.Model) || !ResourceLoader.Exists(FramePath))
            {
                return;
            }

            if (__instance.FindChild("Frame", recursive: true, owned: false) is TextureRect frame)
            {
                var tex = ResourceLoader.Load<Texture2D>(FramePath);
                if (tex != null)
                {
                    frame.Texture = tex;
                    // 温和提亮+微偏紫：抵消游戏内下采样+暗场景的"发黑"，又不过冲发蓝/发白。
                    // (数值可调：还嫌暗就整体调高，发蓝就把 B 往 1.0 收。)
                    frame.Modulate = new Color(1.15f, 1.0f, 1.22f);
                }
            }
        }
        catch
        {
            // 纯装饰：绝不因它中断卡牌渲染。
        }
    }

    private static bool IsAurora(CardModel model)
    {
        if (model is AuroraAttackModuleToken or AuroraShieldModuleToken
                  or AuroraGainHeatToken or AuroraVentHeatToken)
        {
            return true;
        }

        var id = model?.Pool?.Id?.Entry;
        return !string.IsNullOrWhiteSpace(id) && id.Contains("aurora", StringComparison.OrdinalIgnoreCase);
    }
}
