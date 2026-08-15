using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace AuroraMod.AuroraCode.Patches;

/// <summary>
/// 把战斗左上角能量球换成 Aurora 的紫黑水晶机甲核心（5 层贴图）。
/// 贴图已烤成紫色成品，因此 tint 用白色(不改色)，只给外圈粒子染紫，避免二次压暗。
/// Layer2/3 属于 RotationLayers，游戏会自动旋转 → 水晶星核闪烁 + 外圈刻度转动。
/// 结构/映射参照 Kakarot 的 KakarotEnergyCounterColorPatch。
/// </summary>
[HarmonyPatch(typeof(NEnergyCounter), "_Ready")]
public static class AuroraEnergyCounterPatch
{
    // 贴图已经是紫色成品：主层不改色，粒子/描边染紫。
    private static readonly Color NoTint = new(1f, 1f, 1f);
    private static readonly Color ParticleTint = new("b96bff");
    private static readonly Color OutlineTint = new("2a1a3f");

    private const string Dir = "res://Aurora/Images/energy_counters/aurora/";
    private const string Orb1 = Dir + "aurora_orb_layer_1.png";
    private const string Orb2 = Dir + "aurora_orb_layer_2.png";
    private const string Orb3 = Dir + "aurora_orb_layer_3.png";
    private const string Orb4 = Dir + "aurora_orb_layer_4.png";
    private const string Orb5 = Dir + "aurora_orb_layer_5.png";

    public static void Postfix(NEnergyCounter __instance)
    {
        try
        {
            var player = Traverse.Create(__instance).Field("_player").GetValue<Player>();
            if (!IsAurora(player))
            {
                return;
            }

            ReplaceTexture(__instance, "Layers/Layer1", Orb1);
            ReplaceTexture(__instance, "Layers/RotationLayers/Layer2", Orb2);
            ReplaceTexture(__instance, "Layers/RotationLayers/Layer3", Orb3);
            ReplaceTexture(__instance, "Layers/Layer4", Orb4);
            ReplaceTexture(__instance, "Layers/Layer5", Orb5);

            TintTree(__instance.GetNodeOrNull<Control>("%Layers"), NoTint);
            TintTree(__instance.GetNodeOrNull<Control>("%RotationLayers"), NoTint);

            var label = __instance.GetNodeOrNull<Label>("Label");
            label?.AddThemeColorOverride("font_outline_color", OutlineTint);

            // silent 占位粒子 color=绿。之前用 "%BurstBack" 唯一名取不到(返回null)→没染上。
            // 改用直接子节点名，并递归兜底把所有 CPUParticles2D 全染紫，确保绿闪彻底消失。
            TintParticles(__instance.GetNodeOrNull<CpuParticles2D>("BurstBack"));
            TintParticles(__instance.GetNodeOrNull<CpuParticles2D>("BurstFront"));
            TintAllParticles(__instance);

            // July builds wrap the gain-energy burst in NParticlesContainer/GpuParticles2D.
            // Duplicate each process material before recoloring so Silent or co-op teammates
            // never inherit Aurora's purple tint from the shared resource cache.
            TintGpuContainer(Traverse.Create(__instance).Field("_backVfx").GetValue<NParticlesContainer>());
            TintGpuContainer(Traverse.Create(__instance).Field("_frontVfx").GetValue<NParticlesContainer>());
        }
        catch
        {
            // 纯装饰补丁：绝不因它中断战斗流程。
        }
    }

    // 能量增减时爆发的粒子（silent 占位带绿色 color_ramp 渐变）。
    // CpuParticles2D 一旦设了 ColorRamp/ColorInitialRamp，就无视 .Color → 必须先清渐变，
    // 或换成紫色渐变，紫色 .Color 才会真正显示出来。
    private static void TintParticles(CpuParticles2D p)
    {
        if (p == null)
        {
            return;
        }

        // 换成紫色渐变（从亮紫到透明），彻底覆盖原绿色渐变。
        var grad = new Gradient();
        grad.SetColor(0, new Color(ParticleTint, 1f));
        grad.SetColor(1, new Color(ParticleTint, 0f));
        p.ColorRamp = grad;
        p.ColorInitialRamp = null;
        p.Color = ParticleTint;
    }

    // 递归把节点树下所有 CPUParticles2D 染紫（兜底，防唯一名/结构差异漏网）。
    private static void TintAllParticles(Node root)
    {
        if (root == null)
        {
            return;
        }

        foreach (Node child in root.GetChildren())
        {
            if (child is CpuParticles2D p)
            {
                TintParticles(p);
            }

            TintAllParticles(child);
        }
    }

    private static void TintGpuContainer(NParticlesContainer container)
    {
        if (container == null)
        {
            return;
        }

        var particles = Traverse.Create(container)
            .Field("_particles")
            .GetValue<Godot.Collections.Array<GpuParticles2D>>();
        if (particles == null)
        {
            return;
        }

        foreach (var particle in particles)
        {
            TintGpuParticles(particle);
        }
    }

    private static void TintGpuParticles(GpuParticles2D particle)
    {
        if (particle?.ProcessMaterial is not ParticleProcessMaterial sourceMaterial)
        {
            return;
        }

        var material = sourceMaterial.Duplicate() as ParticleProcessMaterial;
        if (material == null)
        {
            return;
        }

        var gradient = new Gradient();
        gradient.SetColor(0, new Color(ParticleTint, 1f));
        gradient.SetColor(1, new Color(ParticleTint, 0f));

        material.Color = ParticleTint;
        material.ColorRamp = new GradientTexture1D { Gradient = gradient };
        material.ColorInitialRamp = null;
        particle.ProcessMaterial = material;
    }

    private static bool IsAurora(Player p)
    {
        return p?.Character is AuroraMod.AuroraCode.Characters.Aurora;
    }

    private static void TintTree(Node root, Color tint)
    {
        if (root == null)
        {
            return;
        }

        foreach (Node child in root.GetChildren())
        {
            if (child is CanvasItem ci)
            {
                ci.Modulate = tint;
            }

            TintTree(child, tint);
        }

        if (root is CanvasItem rootCi)
        {
            rootCi.Modulate = tint;
        }
    }

    private static void ReplaceTexture(Node root, string nodePath, string texturePath)
    {
        if (!ResourceLoader.Exists(texturePath))
        {
            return;
        }

        var textureRect = root.GetNodeOrNull<TextureRect>(nodePath);
        if (textureRect == null)
        {
            return;
        }

        var texture = ResourceLoader.Load<Texture2D>(texturePath);
        if (texture != null)
        {
            textureRect.Texture = texture;
        }
    }
}
