shader_type canvas_item;

// 奥萝拉入场过场：由原生 NTransition 驱动的阈值展开过场（紫黑机甲主题）。
// NTransition 会把 shader_parameter/threshold 从 0→1（FadeOut，覆盖）或 1→0（FadeIn，收回）tween，
// 本 shader 按灰度遮罩 transitionTex 决定屏幕先覆盖哪、后覆盖哪：白色最先、黑色最后。
// 纯表现，不含任何逻辑。

uniform float threshold : hint_range(0.0, 1.0) = 0.0;
uniform sampler2D transitionTex;            // 灰度遮罩：只读红/亮度通道
uniform float edgeWidth = 0.028;            // 展开前沿发光边宽度
uniform vec4 baseColor : source_color = vec4(0.063, 0.031, 0.114, 1.0);   // #10081D 已覆盖区（近黑深紫）
uniform vec4 edgeColor : source_color = vec4(0.659, 0.302, 1.0, 1.0);     // #A84DFF 前沿发光紫
uniform vec4 coreColor : source_color = vec4(0.918, 0.851, 1.0, 1.0);     // #EAD9FF 前沿高光紫白

void fragment() {
    float m = texture(transitionTex, UV).r;   // 灰度

    // 关键：把展开顺序压进 [0, 1-edgeWidth]，留出 edgeWidth 头量，
    // 保证最黑的角落也在 threshold 到 1 之前完全盖满 → threshold=1 时整屏 100% 不透明，
    // 切场景瞬间不会从没盖严处透出未加载完的画面。
    float span = 1.0 - edgeWidth;
    float rev = (1.0 - m) * span;              // 白(m=1)→rev 0 最先覆盖；黑(m=0)→rev span 最后
    float body = smoothstep(rev, rev + edgeWidth, threshold);

    // edge：展开前沿的发光带（body≈0.5 处最亮）。
    float edge = body * (1.0 - body) * 4.0;

    vec3 col = mix(baseColor.rgb, edgeColor.rgb, edge);
    col = mix(col, coreColor.rgb, edge * 0.5);

    float a = clamp(body + edge * 0.6, 0.0, 1.0);
    COLOR = vec4(col, a);
}
