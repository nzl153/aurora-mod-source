using Godot;

namespace AuroraMod.AuroraCode.Visuals;

/// <summary>
/// 热量柱视觉集中配置（工坊反馈：热度条不够直观）。布局与配色只改这里，禁止散落硬编码。
/// 形态：角色<b>左侧一根竖柱</b>，自下而上填充，蓝 → 黄 → 红。
///
/// 为什么是竖的、为什么在侧边（定案理由，别随手改回横条）：
/// ① 横条贴在血条下方会被读成「第二条血」；竖柱形状与血条完全不同，不会误读。
/// ② 血条那一列里还有 <c>NPowerContainer</c>，它塞满会自动换成两行并重新居中——
///    奥萝拉 Buff 极多（模块/连锁/剑势/锁定/挑战协议…），中后期必然两行，横条会被挤掉。
/// ③ 温度计本来就是竖着烧的，蓝在底红在顶不需要额外解释。
/// </summary>
public static class AuroraHeatBarConfig
{
    public const string NodeName = "AuroraHeatBar";

    // ---- 量程 ----

    /// <summary>柱满 = 过热阈值 10。热量无硬上限，10+ 仍是满柱，靠脉动表达「已欠一笔待结算过热」。</summary>
    public const float FullScaleHeat = 10f;

    /// <summary>冷/温分界（热量 4 起为温区）。刻痕位置 = 4/10。</summary>
    public const float WarmTickHeat = 4f;

    /// <summary>温/过载分界（热量 7 起为过载区）。刻痕位置 = 7/10。</summary>
    public const float OverloadTickHeat = 7f;

    // ---- 尺寸与位置（NCreature 局部空间，原点在脚下）----

    public const float BarWidth = 13f;

    /// <summary>柱高 = 包围盒高 × 此比例，夹在上下限之间。</summary>
    public const float BarHeightRatio = 0.40f;
    public const float BarHeightMin = 105f;
    public const float BarHeightMax = 180f;

    /// <summary>柱右边缘距包围盒左边缘的水平间距（放左侧，故向左偏移）。</summary>
    public const float GapFromBounds = 34f;

    /// <summary>柱底相对包围盒底的上移量（抬离地面，避免压脚下阴影）。</summary>
    public const float BottomLift = 40f;

    /// <summary>包围盒不可用时的兜底：柱底中心（相对 NCreature 原点 = 脚下）。</summary>
    public static readonly Vector2 FallbackBottomCenter = new(-120f, -60f);

    public const float BorderThickness = 1.5f;
    public const float TickThickness = 2f;

    // ---- 配色 ----

    /// <summary>空槽底色（半透明深色，空柱时也能看出量程）。</summary>
    public static readonly Color TrackColor = new(0.07f, 0.05f, 0.12f, 0.72f);
    public static readonly Color BorderColor = new(0.62f, 0.45f, 0.88f, 0.85f);
    public static readonly Color TickColor = new(0.85f, 0.80f, 0.95f, 0.45f);

    /// <summary>渐变三档：底 = 冷蓝，中 = 温黄，顶 = 过载红。</summary>
    public static readonly Color ColdColor = new(0.24f, 0.58f, 1.00f);
    public static readonly Color WarmColor = new(1.00f, 0.84f, 0.22f);
    public static readonly Color HotColor = new(1.00f, 0.24f, 0.14f);

    /// <summary>红线（10+）时柱体压向的暗红，配合脉动。</summary>
    public static readonly Color RedlineColor = new(0.78f, 0.09f, 0.06f);

    // ---- 动画 ----

    /// <summary>填充滑动时长（秒）。要「慢慢移动」的手感，别调太短。</summary>
    public const float FillDuration = 0.45f;

    /// <summary>
    /// 过热结算清零时的「泄压」时长。必须远快于 <see cref="FillDuration"/>：
    /// 过热是暴力事件，柱子应该炸空，而不是温柔地流下去。
    /// </summary>
    public const float DischargeDuration = 0.14f;

    /// <summary>红线呼吸脉动周期（秒）与亮度振幅。</summary>
    public const float PulseSeconds = 0.9f;
    public const float PulseAmplitude = 0.28f;

    // ---- 换区闪光（3↔4、6↔7 是玩法上最关键的事件，必须有「咔哒」感）----

    public const float ZoneFlashDuration = 0.34f;

    /// <summary>闪光时柱体向此色混合的最大比例。</summary>
    public const float ZoneFlashStrength = 0.75f;
    public static readonly Color ZoneFlashColor = new(1f, 1f, 1f);

    /// <summary>闪光时边框加粗到的倍数（顿挫感）。</summary>
    public const float ZoneFlashBorderScale = 2.6f;

    // ---- 打出前预览（悬停手牌时叠一段幽灵）----

    /// <summary>预览段透明度。</summary>
    public const float PreviewAlpha = 0.42f;

    /// <summary>预览会越过红线时，幽灵段改用此色（警告）。</summary>
    public static readonly Color PreviewDangerColor = new(1f, 0.30f, 0.20f);

    /// <summary>散热预览（幽灵在当前填充<b>之下</b>，表示将被散掉的一段）用色。</summary>
    public static readonly Color PreviewVentColor = new(0.55f, 0.85f, 1f);

    /// <summary>预览段的描边宽度（让幽灵段边界清晰，不至于和实心段糊在一起）。</summary>
    public const float PreviewEdgeThickness = 1.5f;

    // ---- 红线脉动随热量加快（10 与 25 不该长得一样）----

    /// <summary>每超过阈值 1 点热量，脉动周期缩短的比例。</summary>
    public const float PulseSpeedUpPerHeat = 0.05f;

    /// <summary>脉动周期下限（秒），防止烧到很高时闪成频闪灯。</summary>
    public const float PulseSecondsMin = 0.34f;

    /// <summary>按当前热量取脉动周期。</summary>
    public static float PulsePeriodFor(int heat, int threshold)
    {
        var over = Mathf.Max(0, heat - threshold);
        var period = PulseSeconds / (1f + over * PulseSpeedUpPerHeat);
        return Mathf.Max(period, PulseSecondsMin);
    }

    /// <summary>按填充比例 t∈[0,1] 取柱体颜色：蓝 →（中点）黄 → 红。</summary>
    public static Color FillColorFor(float t)
    {
        t = Mathf.Clamp(t, 0f, 1f);
        return t <= 0.5f
            ? ColdColor.Lerp(WarmColor, t / 0.5f)
            : WarmColor.Lerp(HotColor, (t - 0.5f) / 0.5f);
    }
}
