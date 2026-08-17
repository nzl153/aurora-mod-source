using System;
using System.Collections;
using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Events.Custom;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace AuroraMod.AuroraCode.Patches;

[HarmonyPatch(typeof(NMerchantRoom), "_Ready")]
public static class AuroraMerchantStaticVisualPatch
{
    private const string MerchantModelPath = "res://Aurora/Images/Charui/aurora_merchant_model.png";
    // 新负剑立绘 1024×1536。0.32 尺寸,右下微调贴合圆毯中心。
    private static readonly Vector2 MerchantPosition = new(-15f, -224f);
    private static readonly Vector2 MerchantScale = new(0.32f, 0.32f);

    public static void Postfix(NMerchantRoom __instance)
    {
        try
        {
            if (!ResourceLoader.Exists(MerchantModelPath))
            {
                return;
            }

            var texture = ResourceLoader.Load<Texture2D>(MerchantModelPath);
            if (texture == null)
            {
                return;
            }

            var players = Traverse.Create(__instance).Field("_players").GetValue<IList>();
            var visuals = __instance.PlayerVisuals;
            if (players == null || visuals == null)
            {
                return;
            }

            var count = Math.Min(players.Count, visuals.Count);
            for (var i = 0; i < count; i++)
            {
                if (players[i] is Player player && AuroraRoomVisualHelper.IsAurora(player))
                {
                    ReplaceVisual(visuals[i], texture);
                }
            }
        }
        catch
        {
            // Presentation-only patch: never break the merchant room.
        }
    }

    private static void ReplaceVisual(NMerchantCharacter visual, Texture2D texture)
    {
        var sprite = visual.GetNodeOrNull<Sprite2D>(AuroraRoomVisualHelper.ModelNodeName);
        if (sprite == null)
        {
            sprite = new Sprite2D { Name = AuroraRoomVisualHelper.ModelNodeName };
            visual.AddChild(sprite);
        }

        sprite.Visible = true;
        sprite.Position = MerchantPosition;
        sprite.Scale = MerchantScale;
        sprite.Texture = texture;

        AuroraRoomVisualHelper.HideDirectSpineChildren(visual);
        AuroraRoomVisualHelper.StartMerchantBreathing(sprite, MerchantScale);
    }
}

[HarmonyPatch(typeof(NFakeMerchant), "_Ready")]
public static class AuroraFakeMerchantStaticVisualPatch
{
    private const string MerchantModelPath = "res://Aurora/Images/Charui/aurora_merchant_model.png";
    // 与真商店保持一致(见 AuroraMerchantStaticVisualPatch 说明)。
    private static readonly Vector2 MerchantPosition = new(-15f, -224f);
    private static readonly Vector2 MerchantScale = new(0.32f, 0.32f);

    public static void Postfix(NFakeMerchant __instance)
    {
        try
        {
            if (!ResourceLoader.Exists(MerchantModelPath))
            {
                return;
            }

            var texture = ResourceLoader.Load<Texture2D>(MerchantModelPath);
            if (texture == null)
            {
                return;
            }

            var players = Traverse.Create(__instance).Field("_players").GetValue<IList>();
            var container = Traverse.Create(__instance).Field("_characterContainer").GetValue<Control>();
            if (players == null || container == null || players.Count != 1 ||
                players[0] is not Player player || !AuroraRoomVisualHelper.IsAurora(player))
            {
                return;
            }

            foreach (var child in container.GetChildren())
            {
                if (child is Node2D visual)
                {
                    ReplaceVisual(visual, texture);
                    return;
                }
            }
        }
        catch
        {
            // Presentation-only patch: never break the fake merchant event.
        }
    }

    private static void ReplaceVisual(Node2D visual, Texture2D texture)
    {
        var sprite = visual.GetNodeOrNull<Sprite2D>(AuroraRoomVisualHelper.ModelNodeName);
        if (sprite == null)
        {
            sprite = new Sprite2D { Name = AuroraRoomVisualHelper.ModelNodeName };
            visual.AddChild(sprite);
        }

        sprite.Visible = true;
        sprite.Position = MerchantPosition;
        sprite.Scale = MerchantScale;
        sprite.ZIndex = 100;
        sprite.Texture = texture;

        AuroraRoomVisualHelper.HideDescendantsExceptStaticModel(visual);
        AuroraRoomVisualHelper.StartMerchantBreathing(sprite, MerchantScale);
    }
}

[HarmonyPatch(typeof(NRestSiteRoom), "_Ready")]
public static class AuroraRestSiteStaticVisualPatch
{
    private const string RestSiteModelPath = "res://Aurora/Images/Charui/aurora_rest_site_model.png";
    private static readonly Vector2 RestSitePosition = new(176f, -43f);
    private static readonly Vector2 RestSiteScale = new(0.476f, 0.476f);

    public static void Postfix(NRestSiteRoom __instance)
    {
        try
        {
            if (!ResourceLoader.Exists(RestSiteModelPath))
            {
                return;
            }

            var texture = ResourceLoader.Load<Texture2D>(RestSiteModelPath);
            if (texture == null)
            {
                return;
            }

            foreach (var character in __instance.Characters)
            {
                if (!AuroraRoomVisualHelper.IsAurora(character?.Player))
                {
                    continue;
                }

                ReplaceVisual(character, texture);
            }
        }
        catch
        {
            // Presentation-only patch: never break the rest-site room.
        }
    }

    private static void ReplaceVisual(NRestSiteCharacter character, Texture2D texture)
    {
        var root = AuroraRestSitePatch.ResolveControlRoot(character);
        if (root == null)
        {
            return;
        }

        var sprite = root.GetNodeOrNull<Sprite2D>(AuroraRoomVisualHelper.ModelNodeName);
        if (sprite == null)
        {
            sprite = new Sprite2D { Name = AuroraRoomVisualHelper.ModelNodeName };
            root.AddChild(sprite);
        }

        sprite.Visible = true;
        sprite.Position = RestSitePosition;
        sprite.Scale = RestSiteScale;
        sprite.ZIndex = 0;
        sprite.Texture = texture;

        AuroraRoomVisualHelper.HideDirectSpineChildren(character);
    }
}

internal static class AuroraRoomVisualHelper
{
    internal const string ModelNodeName = "AuroraStaticModel";
    private const string MerchantBreatheMeta = "aurora_merchant_breathe_started";

    internal static bool IsAurora(Player player)
    {
        return player?.Character is AuroraMod.AuroraCode.Characters.Aurora;
    }

    internal static void StartMerchantBreathing(Sprite2D sprite, Vector2 baseScale)
    {
        if (sprite.HasMeta(MerchantBreatheMeta))
        {
            return;
        }

        sprite.SetMeta(MerchantBreatheMeta, true);
        // 胸口起伏：纵向幅度略大于横向，读起来像呼吸而非整体缩放。
        // 之前 1.003/1.006(0.3%/0.6%)肉眼看不出=像张死图；现拉到 1.0%/2.0% 明显但仍轻柔。
        var inhaleScale = new Vector2(baseScale.X * 1.009f, baseScale.Y * 1.018f);
        var tween = sprite.CreateTween().SetLoops();
        tween.TweenProperty(sprite, "scale", inhaleScale, 2.0)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
        tween.TweenProperty(sprite, "scale", baseScale, 2.0)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
    }

    internal static void HideDirectSpineChildren(Node root)
    {
        var hidden = new List<CanvasItem>();
        try
        {
            foreach (var child in root.GetChildren())
            {
                if (child is Node2D node && node.GetClass() == "SpineSprite" && node.Visible)
                {
                    node.Visible = false;
                    hidden.Add(node);
                }
            }
        }
        catch
        {
            RestoreVisibility(hidden);
            throw;
        }
    }

    internal static void HideDescendantsExceptStaticModel(Node root)
    {
        var stack = new Stack<Node>();
        var hidden = new List<CanvasItem>();
        foreach (Node child in root.GetChildren())
        {
            stack.Push(child);
        }

        try
        {
            while (stack.Count > 0)
            {
                var node = stack.Pop();
                if (node.Name != ModelNodeName && node is CanvasItem { Visible: true } canvasItem)
                {
                    canvasItem.Visible = false;
                    hidden.Add(canvasItem);
                }

                foreach (Node child in node.GetChildren())
                {
                    stack.Push(child);
                }
            }
        }
        catch
        {
            RestoreVisibility(hidden);
            throw;
        }
    }

    private static void RestoreVisibility(IEnumerable<CanvasItem> nodes)
    {
        foreach (var node in nodes)
        {
            if (GodotObject.IsInstanceValid(node))
            {
                node.Visible = true;
            }
        }
    }
}
