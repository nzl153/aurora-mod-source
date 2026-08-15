using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace AuroraMod.AuroraCode.Helpers;

/// <summary>奥萝拉攻击牌的命中演出档位（纯表现）。</summary>
public enum AuroraStrikeVfxKind
{
    /// <summary>素净紫刀光 + 轻震屏（默认）。</summary>
    Normal,

    /// <summary>紫刃齐射横扫，适合群体/一下打一片的牌。</summary>
    Flurry,

    /// <summary>重拳落地顿感，适合单发重击。</summary>
    Heavy,

    /// <summary>招牌大招：放大刀光 + 命中爆闪 + 强震屏。</summary>
    Ultimate,
}

/// <summary>
/// 奥萝拉近战命中特效：游戏原生大斩击粒子（<see cref="NBigSlashVfx"/>）染成幽紫（#a380ff，主色系）。
///
/// 两档演出，同一道紫刀光、区别在尺寸与分量：
///   · <see cref="PlayStrikeImpact"/>  普通攻击——素净小刀光 + 轻震屏，不叠爆闪；
///   · <see cref="PlayUltimateSlash"/> 大招（预留）——放大刀光 + 命中爆闪 + 强震屏，招牌级演出。
///
/// 由 <see cref="AuroraMod.AuroraCode.Patches.AuroraStrikeVfxPatch"/> 在「攻击牌实际造成伤害」时调用，
/// 时机已锁在命中帧，立即生成。纯表现层、复用游戏内置粒子：绝不影响玩法/伤害/netcode，异常静默。
/// </summary>
public static class AuroraStrikeVfx
{
    // 幽紫刀光。NBigSlashVfx 默认色本就是 #a380ff，这里显式写死以防日后基调改动。
    private static readonly Color SlashPurple = new("a380ff");
    // 命中爆闪核心（仅大招用）：偏亮的紫白，撞击瞬间更抢眼。
    private static readonly Color ImpactPurple = new("c9a8ff");

    // 普通攻击刀光尺寸（1.0 = 原生；嫌大/小改这里）。
    private const float StrikeScale = 0.6f;
    // 沿攻击方向再往敌人身上推进的水平偏移（像素）。嫌远/近改这里。
    private const float TowardEnemyOffset = 40f;
    // 大招刀光尺寸（比原生还大一点，招牌级）。
    private const float UltimateScale = 1.15f;

    /// <summary>普通攻击命中：一道素净的紫刀光 + 轻震屏（不叠爆闪，别抢大招风头）。</summary>
    public static void PlayStrikeImpact(Creature attacker, Creature target)
    {
        SpawnSlash(attacker, target, StrikeScale, withImpact: false, ShakeStrength.VeryWeak);
    }

    /// <summary>大招命中（预留）：放大紫刀光 + 命中爆闪 + 强震屏，招牌级演出。</summary>
    public static void PlayUltimateSlash(Creature attacker, Creature target)
    {
        SpawnSlash(attacker, target, UltimateScale, withImpact: true, ShakeStrength.Medium);
    }

    // 群伤飞刀锚点从怪物中心再往攻击者一侧多推的余量（像素）。嫌落点太靠前/后就调这个。
    private const float FlurryFrontMargin = 10f;

    /// <summary>
    /// 群体/横扫命中：一片幽紫能量刃呈扇形齐射掠过敌人（原生飞刀扫射染紫）。轻震屏，适合"一下打一片"。
    ///
    /// 位置完全沿用原版刀刃陷阱的做法：喂 target 给 <see cref="NDaggerSprayFlurryVfx.Create(Creature, Color, bool)"/>，
    /// 由它内部锚在怪物美术预设的 VfxSpawnPosition（原生特效锚点），和原版位置一致。
    /// 【教训】之前自己用 hitbox 边缘+偏移+递归提层去"修"，反而把飞刀挪到了怪物右后侧——问题从来是位置不是层级。
    /// </summary>
    public static void PlayFlurry(Creature attacker, Creature target)
    {
        try
        {
            if (target == null)
            {
                return;
            }

            var container = target.GetVfxContainer() ?? attacker?.GetVfxContainer();
            if (container == null)
            {
                return;
            }

            // 攻击者相对目标的位置：攻击者在目标左侧 → attackerRightOfTarget=false。
            var node = NCombatRoom.Instance?.GetCreatureNode(target);
            var attackerNode = attacker != null ? NCombatRoom.Instance?.GetCreatureNode(attacker) : null;
            bool attackerRightOfTarget = node != null && attackerNode != null
                && attackerNode.GlobalPosition.X > node.GlobalPosition.X;

            // 【关键】NDaggerSprayFlurryVfx 的 scale.x(第三参) 同时决定"朝向"和"铺展落点"，二者耦合。
            // 朝向必须朝向怪物(视觉从奥萝拉打向怪物)，否则看着像怪物倒射；但朝向怪物时刃会铺到怪物背后。
            // 解耦：方向保持"朝向怪物"，再单独把锚点沿攻击者方向平移半个身宽+余量，把落点拉到怪物正面(你这侧)。
            bool towardRight = !attackerRightOfTarget;   // 攻击者在左→刃朝右(打向怪物)；在右→朝左
            float dir = towardRight ? 1f : -1f;          // 攻击前进方向

            var flurry = NDaggerSprayFlurryVfx.Create(target, SlashPurple, towardRight);
            if (flurry != null)
            {
                container.AddChild(flurry);
                if (node != null)
                {
                    // 从原生锚点往【攻击者那一侧】(正面)平移半个 hitbox 宽 + 余量，
                    // 让朝向怪物的刃从正面扫入、而非糊到背后。
                    var pos = node.VfxSpawnPosition;
                    if (node.Hitbox != null)
                    {
                        var rect = node.Hitbox.GetGlobalRect();
                        if (rect.Size.X > 1f)
                        {
                            pos.X -= dir * (rect.Size.X * 0.5f + FlurryFrontMargin);
                        }
                    }
                    flurry.GlobalPosition = pos;
                }
            }

            NGame.Instance?.ScreenShake(ShakeStrength.VeryWeak, ShakeDuration.Short);
        }
        catch
        {
            // 纯表现：绝不因它中断战斗。
        }
    }

    // 反应炉重拳·火箭飞拳素材。缺图时自动回退纯粒子版。
    private const string FistTexPath = "res://Aurora/Images/Vfx/reactor_fist.png";
    private const float FistFlightDistance = 620f;   // 从出刀者一侧画面外飞入的距离
    private const float FistFlightTime = 0.16f;       // 飞入耗时（快而有力）
    private const float FistScale = 0.55f;            // 512 贴图 × 0.55 ≈ 280px 到位尺寸
    private const float FistStartScaleMul = 0.6f;     // 起始略小，飞近放大成"冲来"感

    /// <summary>
    /// 单发重击命中：优先播放紫钢机甲火箭飞拳（<see cref="PlayReactorFist"/>）；缺拳头素材时回退原生纯粒子冲击。
    /// </summary>
    public static void PlayHeavy(Creature attacker, Creature target)
    {
        if (ResourceLoader.Exists(FistTexPath))
        {
            PlayReactorFist(attacker, target);
        }
        else
        {
            PlayHeavyParticles(attacker, target);
        }
    }

    /// <summary>
    /// 火箭飞拳：紫钢机甲拳从出刀者一侧画面外高速飞入 → 砸中敌人 hitbox 中心 → 叠原生冲击波粒子 + 强震屏 → 微缩淡出。
    /// 纯 Sprite2D + Tween，纯命中侧、不锚在奥萝拉身上。异常静默。朝左打的怪水平翻转 scale.x。
    /// </summary>
    public static void PlayReactorFist(Creature attacker, Creature target)
    {
        try
        {
            if (target == null)
            {
                return;
            }

            var tex = ResourceLoader.Load<Texture2D>(FistTexPath);
            if (tex == null)
            {
                PlayHeavyParticles(attacker, target);
                return;
            }

            var node = NCombatRoom.Instance?.GetCreatureNode(target);
            var container = target.GetVfxContainer() ?? attacker?.GetVfxContainer();
            if (node == null || container == null)
            {
                return;
            }

            var attackerNode = attacker != null ? NCombatRoom.Instance?.GetCreatureNode(attacker) : null;
            bool goingRight = attackerNode == null
                || node.GlobalPosition.X >= attackerNode.GlobalPosition.X;
            float dir = goingRight ? 1f : -1f;   // 贴图默认朝右；朝左打则翻转

            var endPos = HitboxCenter(node);
            var startPos = endPos - new Vector2(dir * FistFlightDistance, 0f);

            var sprite = new Sprite2D { Texture = tex, Centered = true, ZIndex = 50 };
            container.AddChild(sprite);
            sprite.GlobalPosition = startPos;
            sprite.Scale = new Vector2(dir * FistScale * FistStartScaleMul, FistScale * FistStartScaleMul);

            var tween = sprite.CreateTween();
            tween.TweenProperty(sprite, "global_position", endPos, FistFlightTime)
                .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
            tween.Parallel().TweenProperty(sprite, "scale", new Vector2(dir * FistScale, FistScale), FistFlightTime)
                .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
            // 命中：只留强震屏。原本叠的原生重钝击粒子(NHeavyBluntVfx)自带"从上往下砸"的锤击感，
            // 与这里水平从左往右的飞拳方向打架、显得违和，故去掉，只靠飞拳本体+震屏表现分量。
            tween.TweenCallback(Callable.From(() =>
            {
                try
                {
                    NGame.Instance?.ScreenShake(ShakeStrength.Strong, ShakeDuration.Short);
                }
                catch
                {
                    // 震屏失败不影响后续淡出。
                }
            }));
            tween.TweenProperty(sprite, "modulate:a", 0f, 0.12f);
            tween.TweenCallback(Callable.From(() => sprite.QueueFree()));
        }
        catch
        {
            // 纯表现：绝不因它中断战斗。
        }
    }

    /// <summary>缺拳头素材时的回退：原生重拳纯粒子冲击 + 中等震屏。</summary>
    private static void PlayHeavyParticles(Creature attacker, Creature target)
    {
        try
        {
            if (target == null)
            {
                return;
            }

            var node = NCombatRoom.Instance?.GetCreatureNode(target);
            var container = target.GetVfxContainer() ?? attacker?.GetVfxContainer();
            if (node == null || container == null)
            {
                return;
            }

            var pos = HitboxCenter(node);
            var blunt = NHeavyBluntVfx.Create(pos);
            if (blunt != null)
            {
                container.AddChild(blunt);
                blunt.GlobalPosition = pos;
            }

            NGame.Instance?.ScreenShake(ShakeStrength.Medium, ShakeDuration.Short);
        }
        catch
        {
            // 纯表现：绝不因它中断战斗。
        }
    }

    // 过载灼热命中：紫偏橙的小火簇，只在过载区出刀时叠在敌人身上（纯命中侧，不上奥萝拉身）。
    private static readonly Color OverloadEmber = new("ff7a3c");
    private const float OverloadEmberScale = 0.45f;

    /// <summary>
    /// 过载区攻击的灼热命中点缀：在敌人身体中心生成一小簇紫橙火花，呼应「过载=灼热·×1.25」的核心机制。
    /// 素净、小尺寸、不叠震屏，只作命中侧点缀，绝不喧宾夺主。仅由 AuroraStrikeVfxPatch 在过载区攻击命中时叠加。
    /// </summary>
    public static void PlayOverloadEmber(Creature attacker, Creature target)
    {
        try
        {
            if (target == null)
            {
                return;
            }

            var node = NCombatRoom.Instance?.GetCreatureNode(target);
            var container = target.GetVfxContainer() ?? attacker?.GetVfxContainer();
            if (node == null || container == null)
            {
                return;
            }

            var pos = HitboxCenter(node);
            var ember = NFireBurstVfx.Create(pos, OverloadEmberScale, OverloadEmber);
            if (ember != null)
            {
                container.AddChild(ember);
                ember.GlobalPosition = pos;
            }
        }
        catch
        {
            // 纯表现：绝不因它中断战斗。
        }
    }

    private static void SpawnSlash(Creature attacker, Creature target, float scale, bool withImpact, ShakeStrength shake)
    {
        try
        {
            // 不按血量门槛拦：致命一击时怪已 CurrentHp<=0，但仍要出刀光（死亡动画期间节点与
            // 共享战斗 VFX 容器都还在）。节点/容器取不到时下面自然跳过。
            if (target == null)
            {
                return;
            }

            var node = NCombatRoom.Instance?.GetCreatureNode(target);
            // 容器兜底：致命一击时敌人自己的 VFX 容器可能已随死亡拆除返回 null，
            // 退回攻击者（奥萝拉，仍存活、容器稳定）的容器，位置仍锚敌人身体 → 击杀也稳出刀光。
            var container = target.GetVfxContainer() ?? attacker?.GetVfxContainer();
            if (node == null || container == null)
            {
                return;
            }

            // 刀光朝向 = 攻击方向：攻击者在目标左侧 → 向右劈。
            var attackerNode = attacker != null ? NCombatRoom.Instance?.GetCreatureNode(attacker) : null;
            bool facingRight = attackerNode == null
                || node.GlobalPosition.X >= attackerNode.GlobalPosition.X;

            // 锚在敌人身体中心（hitbox 中心），而非 VfxSpawnPosition（那点偏在怪身前）；
            // 再沿攻击方向往敌人身上推进一点，避免落在身前的空隙里。
            var pos = HitboxCenter(node) + new Vector2((facingRight ? 1f : -1f) * TowardEnemyOffset, 0f);

            // ① 主刀光：原生大斩击粒子，染幽紫，朝攻击方向劈过敌人身体（按 scale 缩放）。
            var slash = NBigSlashVfx.Create(pos, facingRight, SlashPurple);
            if (slash != null)
            {
                container.AddChild(slash);
                slash.GlobalPosition = pos;   // 入树后重设，确保定位准确。
                slash.Scale = new Vector2((facingRight ? 1f : -1f) * scale, scale);
            }

            // ② 命中爆闪：仅大招用，紫白核心在接触点炸开。
            if (withImpact)
            {
                var impact = NBigSlashImpactVfx.Create(pos, facingRight ? 0f : 180f, ImpactPurple);
                if (impact != null)
                {
                    container.AddChild(impact);
                    impact.GlobalPosition = pos;
                    impact.Scale = new Vector2(scale, scale);
                }
            }

            // ③ 镜头震动：普通攻击轻、大招强。
            NGame.Instance?.ScreenShake(shake, ShakeDuration.Short);
        }
        catch
        {
            // 纯表现：绝不因它中断战斗。
        }
    }

    /// <summary>取生物节点 Hitbox 的全局中心（落在身体上）；无效时退回节点原点。</summary>
    private static Vector2 HitboxCenter(MegaCrit.Sts2.Core.Nodes.Combat.NCreature node)
    {
        if (node.Hitbox != null)
        {
            var rect = node.Hitbox.GetGlobalRect();
            if (rect.Size.X > 1f && rect.Size.Y > 1f)
            {
                return rect.GetCenter();
            }
        }

        return node.GlobalPosition;
    }
}
