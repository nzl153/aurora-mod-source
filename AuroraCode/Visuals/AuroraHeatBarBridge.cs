using System;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace AuroraMod.AuroraCode.Visuals;

/// <summary>
/// 热量柱通知桥：一律 fire-and-forget，不 await，不进结算 Task 链
/// （与 <see cref="AuroraModuleVisualBridge"/> 同一纪律）。
/// </summary>
public static class AuroraHeatBarBridge
{
    /// <summary>
    /// 热量已变更，滑到新值。<paramref name="discharge"/> 为真＝过热结算清零，走快速泄压。
    /// </summary>
    public static void RequestAnimate(Creature creature, int heat, bool discharge = false)
    {
        CallDeferred(creature, bar => bar.AnimateTo(heat, discharge));
    }

    /// <summary>无过渡刷新（战斗初始化 / 重连 / 存档恢复）。</summary>
    public static void RequestSnap(Creature creature)
    {
        CallDeferred(creature, bar => bar.SnapToCurrent());
    }

    /// <summary>悬停手牌：显示「打出后会烧到哪」的预览段。<paramref name="delta"/> 为 0 即清除。</summary>
    public static void RequestPreview(Creature creature, int delta)
    {
        CallDeferred(creature, bar => bar.SetPreviewDelta(delta));
    }

    private static void CallDeferred(Creature creature, Action<AuroraHeatBar> action)
    {
        try
        {
            if (creature == null || Engine.GetMainLoop() is not SceneTree)
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

                    // 非奥萝拉 / 非本地玩家时 EnsureOn 返回 null，这里自然不做事。
                    var bar = AuroraHeatBar.EnsureOn(node);
                    if (bar != null)
                    {
                        action(bar);
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
