using Godot;

namespace AuroraMod.AuroraCode.Visuals;

/// <summary>
/// 攻击模块开火光束：从模块位置射向目标的紫色激光（外发光 + 内芯双线）。
/// 纯表现层、快闪即消、结束自 QueueFree；不进结算链、零玩法状态。
/// 起止点由调用方换算到父节点局部空间传入。
/// </summary>
public partial class AuroraModuleBeam : Node2D
{
    public static void Spawn(Node2D parent, Vector2 fromLocal, Vector2 toLocal)
    {
        if (parent == null || !GodotObject.IsInstanceValid(parent))
        {
            return;
        }

        var beam = new AuroraModuleBeam();
        parent.AddChild(beam);
        beam.Fire(fromLocal, toLocal);
    }

    private void Fire(Vector2 from, Vector2 to)
    {
        Name = "AuroraModuleBeam";
        var pts = new[] { from, to };

        var glow = new Line2D
        {
            Name = "Glow",
            Points = pts,
            Width = AuroraModuleVisualConfig.BeamGlowWidth,
            DefaultColor = AuroraModuleVisualConfig.BeamGlowColor,
            BeginCapMode = Line2D.LineCapMode.Round,
            EndCapMode = Line2D.LineCapMode.Round,
            JointMode = Line2D.LineJointMode.Round,
        };
        AddChild(glow);

        var core = new Line2D
        {
            Name = "Core",
            Points = pts,
            Width = AuroraModuleVisualConfig.BeamCoreWidth,
            DefaultColor = AuroraModuleVisualConfig.BeamCoreColor,
            BeginCapMode = Line2D.LineCapMode.Round,
            EndCapMode = Line2D.LineCapMode.Round,
            JointMode = Line2D.LineJointMode.Round,
        };
        AddChild(core);

        var dur = AuroraModuleVisualConfig.BeamDuration;
        var tween = CreateTween().SetParallel();
        tween.TweenProperty(core, "modulate:a", 0f, dur)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
        tween.TweenProperty(glow, "modulate:a", 0f, dur)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
        tween.TweenProperty(core, "width", AuroraModuleVisualConfig.BeamCoreWidth * 0.25f, dur);
        tween.TweenProperty(glow, "width", AuroraModuleVisualConfig.BeamGlowWidth * 0.25f, dur);
        tween.Chain().TweenCallback(Callable.From(QueueFree));
    }
}
