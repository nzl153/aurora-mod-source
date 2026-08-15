using AuroraMod.AuroraCode.Powers;
using Godot;

namespace AuroraMod.AuroraCode.Visuals;

/// <summary>
/// 模块悬浮视觉集中配置（定盘通知 §三）。路径与布局只改这里，禁止散落硬编码。
/// 布局：仿原版机器人充能球，在角色<b>头顶排成一道弧</b>，按 <see cref="AuroraModulePower.All"/> 的
/// 部署顺序<b>从左到右</b>依次排开（不分攻防：slot0 最左、slotN-1 最右）。
/// 弧位是 (槽位索引, 总数, 包围盒) 的确定函数——各客户端各自据同序 All() 本地渲染，无新增同步面、零玩法状态。
/// Buff / Power 图标保留，本层只做旁路展示。
/// </summary>
public static class AuroraModuleVisualConfig
{
    public const string ManagerNodeName = "AuroraModuleVisualManager";

    public const string AttackTexturePath = "res://Aurora/Images/Modules/attack_module.png";
    public const string ShieldTexturePath = "res://Aurora/Images/Modules/shield_module.png";

    /// <summary>模块渲染目标边长（像素）。1254 纹理 → ~96px。适当放大，实机可调。</summary>
    public const float TargetEdgePixels = 96f;

    // ---- 弧形布局：仿 NOrbManager.TweenLayout（角度均分 + (cos,sin)*radius），锚定角色头顶 ----

    /// <summary>弧的半角展开（度）。左端 = 上方 -Spread，右端 = 上方 +Spread。越大模块间距越宽。</summary>
    public const float ArcSpreadDeg = 54f;

    /// <summary>弧半径 = 包围盒高 × 此比例（模块悬于头顶上方约此距离）。</summary>
    public const float ArcRadiusRatio = 0.38f;

    /// <summary>弧半径下限（窄/矮 Hitbox 兜底）。</summary>
    public const float ArcRadiusMinPixels = 80f;

    /// <summary>弧圆心纵向：从包围盒顶向下的比例（略入头部，令模块悬于头顶上方）。</summary>
    public const float ArcPivotYRatio = 0.05f;

    /// <summary>Godot 屏幕“正上方”角度（y 向下，故为 -90°）。</summary>
    private const float UpDeg = -90f;

    /// <summary>包围盒不可用时的兜底圆心（相对 NCreature 原点=脚下；Aurora 屏上约 250px 高）。</summary>
    public static readonly Vector2 FallbackPivot = new(0f, -230f);

    public const float DeployFromScale = 0.65f;
    public const float DeployDuration = 0.25f;
    public const float IdleBobPixels = 4f;
    public const float IdleBobSeconds = 1.6f;
    public const float IdleBreathScale = 0.02f;
    public const float TriggerPunchScale = 1.18f;
    public const float TriggerDuration = 0.18f;
    public const float EnhanceFlashDuration = 0.22f;
    public const float RemoveDuration = 0.2f;

    // ---- 攻击模块开火光束（纯表现，从模块位置射向目标，快闪即消） ----
    public const float BeamDuration = 0.24f;
    public const float BeamCoreWidth = 7f;
    public const float BeamGlowWidth = 18f;
    public static readonly Color BeamCoreColor = new(0.94f, 0.74f, 1f, 1f);
    public static readonly Color BeamGlowColor = new(0.58f, 0.24f, 0.96f, 0.5f);

    /// <summary>按纹理最长边缩放到 <see cref="TargetEdgePixels"/>。</summary>
    public static float ScaleForTexture(Texture2D tex)
    {
        if (tex == null)
        {
            return TargetEdgePixels / 1254f;
        }

        var edge = Mathf.Max(tex.GetWidth(), tex.GetHeight());
        return edge > 0 ? TargetEdgePixels / edge : TargetEdgePixels / 1254f;
    }

    public static string TexturePathFor(ModuleKind kind) =>
        kind == ModuleKind.Attack ? AttackTexturePath : ShieldTexturePath;

    /// <summary>
    /// 头顶弧上第 <paramref name="slotIndex"/>/<paramref name="totalCount"/> 个模块的位置（NCreature-local 空间）。
    /// 左→右按部署顺序均分角度；单个居中头顶。<paramref name="boundsValid"/> 为假时用兜底圆心。
    /// </summary>
    public static Vector2 AnchorFor(Rect2 bounds, bool boundsValid, int slotIndex, int totalCount)
    {
        Vector2 pivot;
        float radius;
        if (boundsValid)
        {
            pivot = new Vector2(
                bounds.Position.X + bounds.Size.X * 0.5f,
                bounds.Position.Y + bounds.Size.Y * ArcPivotYRatio);
            radius = Mathf.Max(bounds.Size.Y * ArcRadiusRatio, ArcRadiusMinPixels);
        }
        else
        {
            pivot = FallbackPivot;
            radius = ArcRadiusMinPixels;
        }

        var count = Mathf.Max(totalCount, 1);
        var t = count == 1 ? 0.5f : (float)slotIndex / (count - 1);
        var ang = Mathf.DegToRad(UpDeg + Mathf.Lerp(-ArcSpreadDeg, ArcSpreadDeg, t));
        return pivot + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * radius;
    }
}
