using AuroraMod.AuroraCode.Powers;
using Godot;

namespace AuroraMod.AuroraCode.Visuals;

/// <summary>
/// 单枚模块的纯视觉节点：静态图 + 数值标签 + Tween。不保存玩法状态。
/// </summary>
public partial class AuroraModuleVisual : Node2D
{
    private Sprite2D _sprite;
    private Label _valueLabel;
    private Tween _idleTween;
    private Tween _actionTween;
    private float _phase;
    private float _displayScale = AuroraModuleVisualConfig.TargetEdgePixels / 1254f;

    public ModuleKind Kind { get; private set; }
    public int BoundValue { get; private set; } = -1;

    /// <summary>逻辑锚点（不受 idle 呼吸偏移影响）。Rebuild 比较与呼吸 home 都用它。</summary>
    public Vector2 TargetAnchor { get; private set; }

    public void Setup(ModuleKind kind, int value, float idlePhase)
    {
        Kind = kind;
        _phase = idlePhase;
        Name = $"AuroraModuleVisual_{kind}";

        var path = AuroraModuleVisualConfig.TexturePathFor(kind);
        Texture2D texture = null;
        if (ResourceLoader.Exists(path))
        {
            texture = ResourceLoader.Load<Texture2D>(path);
        }

        _displayScale = AuroraModuleVisualConfig.ScaleForTexture(texture);

        _sprite = new Sprite2D
        {
            Name = "Sprite",
            Centered = true,
            Texture = texture,
            Modulate = new Color(1f, 1f, 1f, 0f),
            Scale = Vector2.One * (_displayScale * AuroraModuleVisualConfig.DeployFromScale),
        };

        AddChild(_sprite);

        _valueLabel = new Label
        {
            Name = "Value",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Position = new Vector2(-18f, 22f),
            Size = new Vector2(36f, 20f),
            Modulate = new Color(0.92f, 0.86f, 1f, 0f),
        };
        _valueLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.9f, 1f));
        _valueLabel.AddThemeColorOverride("font_outline_color", new Color(0.15f, 0.05f, 0.25f));
        _valueLabel.AddThemeConstantOverride("outline_size", 4);
        AddChild(_valueLabel);

        SetValue(value);
        PlayDeploy();
        StartIdle();
    }

    public void PlaceAt(Vector2 anchor)
    {
        TargetAnchor = anchor;
        Position = anchor;
    }

    /// <summary>
    /// 开火时把枪口朝向目标：目标在左则水平翻转。用 <see cref="Sprite2D.FlipH"/> 而非 scale.x 取负，
    /// 避免与 PlayTrigger/idle 的 scale 补间互相覆盖；数值标签不镜像、始终可读。
    /// </summary>
    public void FaceTarget(bool targetIsLeft)
    {
        if (_sprite != null)
        {
            _sprite.FlipH = targetIsLeft;
        }
    }

    /// <summary>仅当逻辑锚点变化时归位并重开呼吸。</summary>
    public void MoveToAnchorIfNeeded(Vector2 anchor)
    {
        if (TargetAnchor.DistanceSquaredTo(anchor) <= 0.25f)
        {
            return;
        }

        PlaceAt(anchor);
        RefreshIdleHome();
    }

    public void SetValue(int value)
    {
        BoundValue = value;
        if (_valueLabel != null)
        {
            _valueLabel.Text = value.ToString();
        }
    }

    public void PlayDeploy()
    {
        KillActionTween();
        var targetScale = Vector2.One * _displayScale;
        _actionTween = CreateTween().SetParallel();
        _actionTween.TweenProperty(_sprite, "modulate:a", 1f, AuroraModuleVisualConfig.DeployDuration)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        _actionTween.TweenProperty(_sprite, "scale", targetScale, AuroraModuleVisualConfig.DeployDuration)
            .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        _actionTween.TweenProperty(_valueLabel, "modulate:a", 1f, AuroraModuleVisualConfig.DeployDuration);
    }

    public void PlayTrigger()
    {
        KillActionTween();
        var baseScale = Vector2.One * _displayScale;
        var punch = baseScale * AuroraModuleVisualConfig.TriggerPunchScale;
        _actionTween = CreateTween();
        _actionTween.TweenProperty(_sprite, "scale", punch, AuroraModuleVisualConfig.TriggerDuration * 0.45f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        _actionTween.TweenProperty(_sprite, "scale", baseScale, AuroraModuleVisualConfig.TriggerDuration * 0.55f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
        _actionTween.Parallel().TweenProperty(_sprite, "modulate", new Color(1.35f, 1.15f, 1.55f, 1f), AuroraModuleVisualConfig.TriggerDuration * 0.35f);
        _actionTween.TweenProperty(_sprite, "modulate", Colors.White, AuroraModuleVisualConfig.TriggerDuration * 0.65f);
    }

    public void PlayEnhance()
    {
        KillActionTween();
        _actionTween = CreateTween();
        _actionTween.TweenProperty(_sprite, "modulate", new Color(1.55f, 1.35f, 1.85f, 1f), AuroraModuleVisualConfig.EnhanceFlashDuration * 0.4f);
        _actionTween.TweenProperty(_sprite, "modulate", Colors.White, AuroraModuleVisualConfig.EnhanceFlashDuration * 0.6f);
    }

    public void PlayRemoveThenFree()
    {
        KillIdleTween();
        KillActionTween();
        _actionTween = CreateTween().SetParallel();
        _actionTween.TweenProperty(_sprite, "modulate:a", 0f, AuroraModuleVisualConfig.RemoveDuration);
        _actionTween.TweenProperty(_sprite, "scale", Vector2.One * (_displayScale * 0.55f), AuroraModuleVisualConfig.RemoveDuration);
        _actionTween.TweenProperty(_valueLabel, "modulate:a", 0f, AuroraModuleVisualConfig.RemoveDuration);
        _actionTween.Chain().TweenCallback(Callable.From(QueueFree));
    }

    private void StartIdle()
    {
        KillIdleTween();
        if (_sprite == null)
        {
            return;
        }

        // 相位只延迟第一圈，不进 SetLoops，避免每圈停顿后趋向同步。
        if (_phase > 0.01f)
        {
            _idleTween = CreateTween();
            _idleTween.TweenInterval(_phase);
            _idleTween.TweenCallback(Callable.From(BeginIdleLoop));
        }
        else
        {
            BeginIdleLoop();
        }
    }

    private void BeginIdleLoop()
    {
        if (_sprite == null || !GodotObject.IsInstanceValid(this))
        {
            return;
        }

        KillIdleTween();

        var bob = AuroraModuleVisualConfig.IdleBobPixels;
        var breath = AuroraModuleVisualConfig.IdleBreathScale;
        var baseScale = _displayScale;
        var duration = AuroraModuleVisualConfig.IdleBobSeconds;
        var homeY = TargetAnchor.Y;

        _idleTween = CreateTween().SetLoops();
        _idleTween.TweenProperty(this, "position:y", homeY - bob, duration)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        _idleTween.Parallel().TweenProperty(_sprite, "scale", Vector2.One * (baseScale * (1f + breath)), duration)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        _idleTween.TweenProperty(this, "position:y", homeY + bob, duration)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        _idleTween.Parallel().TweenProperty(_sprite, "scale", Vector2.One * (baseScale * (1f - breath)), duration)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
    }

    public void RefreshIdleHome()
    {
        StartIdle();
    }

    private void KillIdleTween()
    {
        if (_idleTween != null && _idleTween.IsValid())
        {
            _idleTween.Kill();
        }

        _idleTween = null;
    }

    private void KillActionTween()
    {
        if (_actionTween != null && _actionTween.IsValid())
        {
            _actionTween.Kill();
        }

        _actionTween = null;
    }

    public override void _ExitTree()
    {
        KillIdleTween();
        KillActionTween();
        base._ExitTree();
    }
}
