using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace AuroraMod.AuroraCode.Visuals;

/// <summary>
/// 攻击模块激光命中敌人时的小紫爆：复用游戏原生 <see cref="NFireBurstVfx"/>（支持染色 + 缩放），
/// 染成模块紫、缩小尺寸，落在敌人身体中心。与光束同帧生成（光束是瞬时整条画出，无飞行时间）。
/// 纯表现层、快闪即消：绝不影响玩法/伤害/netcode，异常静默。
/// </summary>
public static class AuroraModuleImpactVfx
{
    // 与模块激光同色系的紫（对齐能量球/光束）。
    private static readonly Color BurstPurple = new("b96bff");
    // 小爆尺寸（1.0 = 原生火爆；模块命中只要一小簇）。
    private const float ScaleFactor = 0.45f;

    public static void Spawn(Creature target)
    {
        try
        {
            if (target == null || target.IsDead)
            {
                return;
            }

            var node = NCombatRoom.Instance?.GetCreatureNode(target);
            var container = target.GetVfxContainer();
            if (node == null || container == null)
            {
                return;
            }

            var pos = HitboxCenter(node);
            var burst = NFireBurstVfx.Create(pos, ScaleFactor, BurstPurple);
            if (burst != null)
            {
                container.AddChild(burst);
                burst.GlobalPosition = pos;
            }
        }
        catch
        {
            // 纯表现：绝不因它中断战斗。
        }
    }

    private static Vector2 HitboxCenter(NCreature node)
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
