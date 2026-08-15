using System;
using AuroraMod.AuroraCode.Powers;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace AuroraMod.AuroraCode.Visuals;

/// <summary>
/// 模块视觉通知桥：一律 fire-and-forget，不 await，不进结算 Task 链。
/// </summary>
public static class AuroraModuleVisualBridge
{
    public static void RequestRebuild(Creature creature)
    {
        CallDeferred(creature, manager => manager.RebuildFromPowers());
    }

    public static void RequestEnhance(Creature creature, AuroraModulePower module)
    {
        CallDeferred(creature, manager =>
        {
            manager.RebuildFromPowers();
            manager.NotifyEnhanced(module);
        });
    }

    /// <summary>
    /// 模块触发通知。<paramref name="target"/> 非空时（攻击模块）在延迟帧解析其 Hitbox 中心，供画光束；
    /// 护盾模块传 null 只做脉冲。目标坐标在延迟帧才取，避免拿到过期位置。
    /// </summary>
    public static void RequestTrigger(Creature creature, AuroraModulePower module, Creature target = null)
    {
        CallDeferred(creature, manager =>
        {
            Vector2? targetGlobal = null;
            if (target != null)
            {
                var targetNode = NCombatRoom.Instance?.GetCreatureNode(target);
                if (targetNode != null)
                {
                    targetGlobal = ResolveHitboxCenter(targetNode);
                }
            }

            manager.NotifyTriggered(module, targetGlobal);

            // 攻击模块激光命中处补一记小紫爆（与光束同帧，落在敌人身上）。
            if (target != null)
            {
                AuroraModuleImpactVfx.Spawn(target);
            }
        });
    }

    /// <summary>取生物节点 Hitbox 的全局中心（含缩放）；无效时退回节点原点。</summary>
    private static Vector2 ResolveHitboxCenter(NCreature node)
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

    /// <summary>
    /// 部署成功后播 Cast 触发（Aurora GenerateAnimator 已把 Cast→skill）。同步 void，不卡结算。
    /// </summary>
    public static void RequestDeployAnim(Creature creature)
    {
        try
        {
            if (creature == null)
            {
                return;
            }

            var tree = Engine.GetMainLoop() as SceneTree;
            if (tree == null)
            {
                return;
            }

            Callable.From(() =>
            {
                try
                {
                    var node = NCombatRoom.Instance?.GetCreatureNode(creature);
                    node?.SetAnimationTrigger("Cast");
                }
                catch (Exception)
                {
                    // 纯表现层：失败不得影响战斗。
                }
            }).CallDeferred();
        }
        catch (Exception)
        {
        }
    }

    private static void CallDeferred(Creature creature, Action<AuroraModuleVisualManager> action)
    {
        try
        {
            if (creature == null)
            {
                return;
            }

            var tree = Engine.GetMainLoop() as SceneTree;
            if (tree == null)
            {
                return;
            }

            // 下一帧执行：不阻塞当前 Power/卡牌结算。
            Callable.From(() =>
            {
                try
                {
                    var node = NCombatRoom.Instance?.GetCreatureNode(creature);
                    if (node == null)
                    {
                        return;
                    }

                    var manager = AuroraModuleVisualManager.EnsureOn(node);
                    if (manager != null)
                    {
                        action(manager);
                    }
                }
                catch (Exception)
                {
                    // 纯表现层：失败不得影响战斗。
                }
            }).CallDeferred();
        }
        catch (Exception)
        {
        }
    }
}
