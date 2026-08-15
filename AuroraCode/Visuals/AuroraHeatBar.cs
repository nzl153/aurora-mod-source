using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Powers;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Rooms;

namespace AuroraMod.AuroraCode.Visuals;

/// <summary>
/// 热量竖柱：纯视觉，只读 <see cref="HeatPower"/>，零玩法状态。挂在 NCreature 上、不抬 ZIndex，
/// 战斗结束自清（对齐 <see cref="AuroraModuleVisualManager"/> 的纪律）。
///
/// 联机：<b>队友的奥萝拉也画</b>。两张接力卡确实都只改出牌者自己的热量（队友热量不可操作），
/// 但看得见队友烧到哪一档，配合起来更有信息量。柱子各挂各的 NCreature，互不打架。
///
/// 与 Buff 图标分工：柱子负责一眼看懂当前区段，Buff 图标保留精确数字与
/// 「下次过热伤害」悬停提示（<c>HeatPower.CanonicalVars</c> 的 NextOverheatDamage），两者都不删。
/// </summary>
public partial class AuroraHeatBar : Node2D
{
    private Creature _creature;
    private Tween _fillTween;
    private bool _combatEndedHooked;

    /// <summary>当前绘制用填充比例 0..1（Tween 的目标，故意与真实热量解耦以获得平滑滑动）。</summary>
    private float _fill;

    /// <summary>红线脉动相位累积（仅红线时推进）。</summary>
    private float _pulsePhase;

    private bool _redline;

    /// <summary>上一次已知热量。用于判断换区（闪光）与脉动提速。</summary>
    private int _lastHeat;

    /// <summary>换区闪光强度 0..1，Tween 衰减到 0。</summary>
    private float _flash;

    private Tween _flashTween;

    /// <summary>
    /// 打出前预览：悬停手牌时该卡会造成的热量变化量。0 = 不显示预览段。
    /// 只是画一层幽灵，绝不参与任何结算。
    /// </summary>
    private int _previewDelta;

    /// <summary>柱体上的鼠标热区（Node2D 自身收不到鼠标事件，挂个 Control 代收）。</summary>
    private Control _hitbox;

    /// <summary>柱体矩形（NCreature 局部空间）。随包围盒变化重算，取不到时走兜底。</summary>
    private Rect2 _trackRect;

    public Creature BoundCreature => _creature;

    public void Bind(Creature creature)
    {
        _creature = creature;
        Name = AuroraHeatBarConfig.NodeName;
        HookCombatEnded();
        HookCreature();
        EnsureHitbox();
        RecomputeGeometry();
        // 首次绑定不做滑动：重连/战斗开始时不该看到一段假的爬升。
        SnapToCurrent();
    }

    /// <summary>按当前 <see cref="HeatPower"/> 立即刷新，无过渡。用于绑定 / 重连 / 战斗初始化。</summary>
    public void SnapToCurrent()
    {
        var heat = HeatPower.GetHeat(_creature);
        KillFillTween();
        // 保险：战斗结束时被隐藏过，任何一次刷新都要把它收回来。
        // （NCreature 大概率每场战斗重建，但不确证；这行是防「柱子永久消失」的兜底。）
        Visible = true;
        _fill = RatioFor(heat);
        _lastHeat = heat;
        _redline = heat >= HeatPower.OverheatThreshold;
        QueueRedraw();
    }

    /// <summary>
    /// 热量变更后滑到新值。<paramref name="discharge"/> 为真时用「泄压」的快速时长
    /// （过热结算清零专用——那是暴力事件，不该温柔地流下去）。
    /// </summary>
    public void AnimateTo(int heat, bool discharge = false)
    {
        Visible = true;
        RecomputeGeometry();

        var target = RatioFor(heat);
        var wasRedline = _redline;
        _redline = heat >= HeatPower.OverheatThreshold;

        // 换区（冷↔温↔过载）是玩法上最关键的事件——一堆卡按区段结算。
        // 平滑滑过刻痕完全没有「咔哒」感，所以这里补一记闪光 + 边框加粗。
        // 过热清零不算换区（那是被强制清零，不该奖励一次闪光）。
        if (!discharge && HeatPower.ZoneOf(_lastHeat) != HeatPower.ZoneOf(heat))
        {
            PlayZoneFlash();
        }

        _lastHeat = heat;

        // 刚踏进红线：相位归零，让第一次脉动从亮处起，更容易被注意到。
        if (_redline && !wasRedline)
        {
            _pulsePhase = 0f;
        }

        KillFillTween();

        if (Mathf.IsEqualApprox(_fill, target))
        {
            QueueRedraw();
            return;
        }

        _fillTween = CreateTween();
        _fillTween.SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        _fillTween.TweenMethod(
            Callable.From<float>(SetFillRatio),
            _fill,
            target,
            discharge ? AuroraHeatBarConfig.DischargeDuration : AuroraHeatBarConfig.FillDuration);
    }

    private void SetFillRatio(float value)
    {
        _fill = value;
        QueueRedraw();
    }

    // ---------------- 换区闪光 ----------------

    private void PlayZoneFlash()
    {
        KillFlashTween();
        _flash = 1f;

        _flashTween = CreateTween();
        _flashTween.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        _flashTween.TweenMethod(
            Callable.From<float>(SetFlash),
            1f,
            0f,
            AuroraHeatBarConfig.ZoneFlashDuration);
    }

    private void SetFlash(float value)
    {
        _flash = value;
        QueueRedraw();
    }

    private void KillFlashTween()
    {
        if (_flashTween != null && _flashTween.IsValid())
        {
            _flashTween.Kill();
        }

        _flashTween = null;
    }

    // ---------------- 打出前预览 ----------------

    /// <summary>
    /// 悬停手牌时设置预览增量（正=积热、负=散热、0=清除）。纯表现，不参与结算。
    /// </summary>
    public void SetPreviewDelta(int delta)
    {
        if (_previewDelta == delta)
        {
            return;
        }

        _previewDelta = delta;
        QueueRedraw();
    }

    public void ClearPreview() => SetPreviewDelta(0);

    // ---------------- 悬停热区 ----------------

    /// <summary>
    /// Node2D 收不到鼠标事件，挂一个 Control 代收。悬停时复用现有的「热量」机制提示
    /// （与卡面上那条是同一份 loc，不另写文案）。
    /// </summary>
    private void EnsureHitbox()
    {
        if (_hitbox != null && GodotObject.IsInstanceValid(_hitbox))
        {
            return;
        }

        _hitbox = new Control
        {
            Name = "HeatBarHitbox",
            // 用 Pass 不用 Stop：一样能收到 MouseEntered/Exited，但事件继续向上传递。
            // Stop 会吞掉落在这块矩形里的鼠标事件，万一它压到出牌拖拽/选目标的路径上就是玩法事故。
            MouseFilter = Control.MouseFilterEnum.Pass,
        };

        AddChild(_hitbox);
        _hitbox.MouseEntered += OnBarHovered;
        _hitbox.MouseExited += OnBarUnhovered;
    }

    private void OnBarHovered()
    {
        try
        {
            if (GetParent() is not NCreature nc)
            {
                return;
            }

            nc.ShowHoverTips(AuroraMechanicTips.Build([AuroraMechanic.Heat]));
        }
        catch
        {
            // 纯表现：提示失败不得影响战斗。
        }
    }

    private void OnBarUnhovered()
    {
        try
        {
            (GetParent() as NCreature)?.HideHoverTips();
        }
        catch
        {
        }
    }

    private static float RatioFor(int heat) =>
        Mathf.Clamp(heat / AuroraHeatBarConfig.FullScaleHeat, 0f, 1f);

    private void KillFillTween()
    {
        if (_fillTween != null && _fillTween.IsValid())
        {
            _fillTween.Kill();
        }

        _fillTween = null;
    }

    public override void _Process(double delta)
    {
        // 只有红线才每帧重绘（脉动）。其余时间完全静止，不浪费绘制。
        if (!_redline)
        {
            return;
        }

        _pulsePhase += (float)delta;
        QueueRedraw();
    }

    /// <summary>
    /// 依父 NCreature 的 Hitbox 算柱体矩形（NCreature 局部空间；本节点挂原点、scale=1）。
    /// Hitbox 未就绪时退回兜底位置，绝不因取不到包围盒而不画。
    /// </summary>
    private void RecomputeGeometry()
    {
        float height;
        Vector2 bottomCenter;

        if (TryGetLocalBounds(out var bounds))
        {
            height = Mathf.Clamp(
                bounds.Size.Y * AuroraHeatBarConfig.BarHeightRatio,
                AuroraHeatBarConfig.BarHeightMin,
                AuroraHeatBarConfig.BarHeightMax);

            // 放左侧：柱体右边缘距包围盒左边缘 GapFromBounds。
            var right = bounds.Position.X - AuroraHeatBarConfig.GapFromBounds;
            var bottom = bounds.Position.Y + bounds.Size.Y - AuroraHeatBarConfig.BottomLift;
            bottomCenter = new Vector2(right - AuroraHeatBarConfig.BarWidth * 0.5f, bottom);
        }
        else
        {
            height = AuroraHeatBarConfig.BarHeightMin;
            bottomCenter = AuroraHeatBarConfig.FallbackBottomCenter;
        }

        _trackRect = new Rect2(
            new Vector2(bottomCenter.X - AuroraHeatBarConfig.BarWidth * 0.5f, bottomCenter.Y - height),
            new Vector2(AuroraHeatBarConfig.BarWidth, height));

        // 鼠标热区跟随柱体；略微外扩，细柱子才好悬停到。
        if (_hitbox != null && GodotObject.IsInstanceValid(_hitbox))
        {
            const float pad = 6f;
            _hitbox.Position = _trackRect.Position - new Vector2(pad, pad);
            _hitbox.Size = _trackRect.Size + new Vector2(pad * 2f, pad * 2f);
        }
    }

    private bool TryGetLocalBounds(out Rect2 bounds)
    {
        bounds = default;
        if (GetParent() is not NCreature nc || nc.Hitbox == null)
        {
            return false;
        }

        var size = nc.Hitbox.Size;
        if (size.X <= 1f || size.Y <= 1f)
        {
            return false;
        }

        bounds = new Rect2(nc.Hitbox.GlobalPosition - nc.GlobalPosition, size);
        return true;
    }

    public override void _Draw()
    {
        // 每次绘制前重算：角色缩放/位移、Hitbox 迟就绪、窗口尺寸变化都会让旧几何过期。
        // 只在需要重绘时才走到这里（静止时一帧都不跑），几个浮点运算，代价可以忽略。
        RecomputeGeometry();
        if (_trackRect.Size.Y <= 1f)
        {
            return;
        }

        // 1. 空槽 + 边框（空柱时也看得出量程）。换区闪光时边框加粗，做出顿挫感。
        DrawRect(_trackRect, AuroraHeatBarConfig.TrackColor);
        var borderWidth = AuroraHeatBarConfig.BorderThickness *
                          Mathf.Lerp(1f, AuroraHeatBarConfig.ZoneFlashBorderScale, _flash);
        DrawRect(_trackRect, AuroraHeatBarConfig.BorderColor, filled: false, width: borderWidth);

        // 2. 自下而上的填充
        var fill = Mathf.Clamp(_fill, 0f, 1f);
        if (fill > 0.001f)
        {
            var color = AuroraHeatBarConfig.FillColorFor(fill);

            if (_redline)
            {
                // 红线：满柱不再涨，改用亮度呼吸表达「已锁定一笔待结算过热」。
                // 周期随热量缩短——10 热和 25 热的危险程度差很多，不该长得一模一样。
                var period = AuroraHeatBarConfig.PulsePeriodFor(_lastHeat, HeatPower.OverheatThreshold);
                var wave = (Mathf.Sin(_pulsePhase / period * Mathf.Tau) + 1f) * 0.5f;
                var mix = 1f - AuroraHeatBarConfig.PulseAmplitude + wave * AuroraHeatBarConfig.PulseAmplitude;
                color = AuroraHeatBarConfig.RedlineColor.Lerp(AuroraHeatBarConfig.HotColor, mix);
            }

            // 换区闪光：向白色混合。
            if (_flash > 0.001f)
            {
                color = color.Lerp(AuroraHeatBarConfig.ZoneFlashColor,
                    _flash * AuroraHeatBarConfig.ZoneFlashStrength);
            }

            DrawRect(RectForSpan(0f, fill), color);
        }

        // 3. 打出前预览：悬停手牌时叠一段幽灵，直接回答「打出去会不会越线」。
        DrawPreview(fill);

        // 4. 区段刻痕：热量 4（冷/温）与 7（温/过载）
        DrawTick(AuroraHeatBarConfig.WarmTickHeat);
        DrawTick(AuroraHeatBarConfig.OverloadTickHeat);
    }

    /// <summary>取柱体上 [from, to] 两个比例之间的矩形（0=底、1=顶）。</summary>
    private Rect2 RectForSpan(float from, float to)
    {
        var lo = Mathf.Min(from, to);
        var hi = Mathf.Max(from, to);
        var top = _trackRect.Position.Y + _trackRect.Size.Y * (1f - hi);
        var height = _trackRect.Size.Y * (hi - lo);
        return new Rect2(new Vector2(_trackRect.Position.X, top), new Vector2(_trackRect.Size.X, height));
    }

    /// <summary>
    /// 预览段：积热画在当前填充<b>之上</b>（会涨到哪），散热画在<b>之内</b>（会掉到哪）。
    /// 预览会越过红线阈值时整段转成警告红——这是玩家最需要提前知道的一件事。
    /// </summary>
    private void DrawPreview(float fill)
    {
        if (_previewDelta == 0)
        {
            return;
        }

        var predicted = Mathf.Clamp(_lastHeat + _previewDelta, 0, HeatPower.SafeMaxHeat);
        var predictedRatio = RatioFor(predicted);

        if (Mathf.IsEqualApprox(predictedRatio, fill))
        {
            return;
        }

        Color color;
        if (_previewDelta > 0)
        {
            // 打出后会踏进红线（而现在还没踏进）→ 警告色。
            var willCross = predicted >= HeatPower.OverheatThreshold && _lastHeat < HeatPower.OverheatThreshold;
            color = willCross
                ? AuroraHeatBarConfig.PreviewDangerColor
                : AuroraHeatBarConfig.FillColorFor(predictedRatio);
        }
        else
        {
            color = AuroraHeatBarConfig.PreviewVentColor;
        }

        var ghost = RectForSpan(fill, predictedRatio);
        DrawRect(ghost, new Color(color, AuroraHeatBarConfig.PreviewAlpha));
        DrawRect(ghost, new Color(color, 0.9f), filled: false,
            width: AuroraHeatBarConfig.PreviewEdgeThickness);
    }

    private void DrawTick(float heat)
    {
        var t = Mathf.Clamp(heat / AuroraHeatBarConfig.FullScaleHeat, 0f, 1f);
        var y = _trackRect.Position.Y + _trackRect.Size.Y * (1f - t);
        DrawLine(
            new Vector2(_trackRect.Position.X, y),
            new Vector2(_trackRect.Position.X + _trackRect.Size.X, y),
            AuroraHeatBarConfig.TickColor,
            AuroraHeatBarConfig.TickThickness);
    }

    // ---------------- 生命周期 ----------------

    /// <summary>战斗结束隐藏，避免压「搜刮!」奖励窗 / 地图残留（对齐 Orb ClearOrbs）。</summary>
    private void OnCombatEnded(CombatRoom _)
    {
        KillFillTween();
        KillFlashTween();
        ClearPreview();
        Visible = false;
    }

    /// <summary>死亡即收柱（尸体旁挂个温度计很怪）；复活再收回来。</summary>
    private void HookCreature()
    {
        if (_creature == null)
        {
            return;
        }

        _creature.Died -= OnCreatureDied;
        _creature.Revived -= OnCreatureRevived;
        _creature.Died += OnCreatureDied;
        _creature.Revived += OnCreatureRevived;
    }

    private void UnhookCreature()
    {
        if (_creature == null)
        {
            return;
        }

        try
        {
            _creature.Died -= OnCreatureDied;
            _creature.Revived -= OnCreatureRevived;
        }
        catch
        {
            // 退出/热重载时实体可能已不可用。
        }
    }

    private void OnCreatureDied(Creature _)
    {
        KillFillTween();
        KillFlashTween();
        Visible = false;
    }

    private void OnCreatureRevived(Creature _) => SnapToCurrent();

    private void HookCombatEnded()
    {
        if (_combatEndedHooked || CombatManager.Instance == null)
        {
            return;
        }

        CombatManager.Instance.CombatEnded += OnCombatEnded;
        _combatEndedHooked = true;
    }

    private void UnhookCombatEnded()
    {
        if (!_combatEndedHooked)
        {
            return;
        }

        try
        {
            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.CombatEnded -= OnCombatEnded;
            }
        }
        catch
        {
            // 退出/热重载时 Instance 可能已不可用。
        }

        _combatEndedHooked = false;
    }

    public override void _ExitTree()
    {
        UnhookCombatEnded();
        UnhookCreature();
        KillFillTween();
        KillFlashTween();
        base._ExitTree();
    }

    /// <summary>
    /// 幂等挂载。非奥萝拉、非本地玩家一律返回 null（队友的热量不画）。
    /// </summary>
    public static AuroraHeatBar EnsureOn(NCreature creatureNode)
    {
        if (creatureNode?.Entity == null)
        {
            return null;
        }

        if (creatureNode.Entity.Player?.Character is not Characters.Aurora)
        {
            return null;
        }

        // 联机：队友的奥萝拉也画。虽然两张接力卡都只改出牌者自己的热量（队友热量不可操作），
        // 但看得见队友烧到哪一档，配合起来更有信息量；柱子挂在各自 NCreature 上，不会互相打架。
        var existing = creatureNode.GetNodeOrNull<AuroraHeatBar>(AuroraHeatBarConfig.NodeName);
        if (existing != null)
        {
            if (existing.BoundCreature != creatureNode.Entity)
            {
                existing.Bind(creatureNode.Entity);
            }

            return existing;
        }

        var bar = new AuroraHeatBar();
        creatureNode.AddChild(bar);
        bar.Bind(creatureNode.Entity);
        return bar;
    }
}
