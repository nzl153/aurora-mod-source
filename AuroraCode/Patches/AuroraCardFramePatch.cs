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

    public static void Postfix(NCard __instance) => ApplyFrame(__instance);

    internal static void ApplyFrame(NCard __instance)
    {
        try
        {
            // 清掉历史遗留的程序化装饰层（若有）。
            __instance.FindChild(LegacyOverlayName, recursive: true, owned: false)?.QueueFree();

            if (!IsAurora(__instance.Model) || !ResourceLoader.Exists(FramePath))
            {
                return;
            }

            if (ResolveFrame(__instance) is TextureRect frame)
            {
                var tex = ResourceLoader.Load<Texture2D>(FramePath);
                if (tex != null)
                {
                    // beta v0.111.0：Reload 末尾会给卡框挂一层 Model.FrameMaterial 着色器
                    // （引擎按自己的规则对卡框染色）。它会盖掉我们换上去的贴图，表现为"换了等于没换"。
                    // 奥萝拉的框是已经画好颜色的整图，不需要引擎再染一次，直接清掉这层材质。
                    // 正式版上这里本来就是 null，清它无副作用，故不进 #if。
                    frame.Material = null;
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

    /// <summary>
    /// 取卡框 TextureRect。<b>以反射读 NCard 的私有字段 _frame 为准</b>，按节点名查找只作兜底。
    ///
    /// 引擎自己是用唯一名取的（<c>_frame = GetNode&lt;TextureRect&gt;("%Frame")</c>），
    /// 而 <c>FindChild("Frame", recursive)</c> 是深度优先按名字扫，在 beta v0.111.0 的卡牌场景里
    /// 会先撞上另一个同名节点 —— 于是贴图被换到了错的节点上：引擎实际显示的那个框纹丝不动。
    ///
    /// 这个补丁是纯装饰、全程 try/catch，走错节点既不抛异常也不打日志，
    /// 表现只是"卡框看着没变"，极易误判成贴图没打进 pck。字段是引擎的唯一事实来源，因此优先。
    /// </summary>
    private static TextureRect ResolveFrame(NCard card)
    {
        return AccessTools.Field(typeof(NCard), "_frame")?.GetValue(card) as TextureRect
               ?? card.FindChild("Frame", recursive: true, owned: false) as TextureRect;
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

#if STS2_BETA
/// <summary>
/// beta v0.111.0 专用：卡框贴图还需要在 <c>UpdatePortrait</c> 之后再补一次。
///
/// beta 的 <c>UpdateVisuals(PileType, CardPreviewMode)</c> 会独立再调一次 <c>UpdatePortrait()</c>，
/// 而 <c>_frame.Texture = Model.Frame</c> 正在其中——于是 Reload 时换好的贴图会被刷回默认。
/// 又因为我们清掉了 FrameMaterial，露出的是没染色的底图，表现为「卡框变红」。
///
/// <c>UpdatePortrait</c> 是 Reload 与 UpdateVisuals 两条路径的共同末端，挂这一处即可全覆盖。
/// <b>只在 beta 挂</b>：正式版没有这条重刷路径，而 Harmony 目标不存在会让 PatchAll 一炸全停。
/// </summary>
[HarmonyPatch(typeof(NCard), "UpdatePortrait")]
public static class AuroraCardFrameRepaintPatch
{
    public static void Postfix(NCard __instance) => AuroraCardFramePatch.ApplyFrame(__instance);
}
#endif
