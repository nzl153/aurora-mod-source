using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace AuroraMod.AuroraCode.Helpers;

/// <summary>
/// 战斗立绘转向。默认奥萝拉面朝右（敌人一般在右侧）；打多部件 Boss（如帝皇螃蟹左右爪，
/// 目标可能在左侧）时，攻击出手前把 SpineSprite 的 scale.x 取负翻转朝向目标。
/// 卡卡罗特用静态图 <c>Sprite2D.FlipH</c>；我们用 spine → 翻 <c>Body(%Visuals).Scale.X</c> 正负。
/// 全程防御式：任何一步失败都静默跳过，绝不打断战斗。
/// </summary>
public static class AuroraFacing
{
    /// <summary>让 me 的立绘朝向 target（target 在右→面右，在左→面左）。</summary>
    public static void FaceTarget(Creature me, Creature target)
    {
        try
        {
            if (me == null || target == null || target.IsDead)
            {
                return;
            }

            var myNode = NCombatRoom.Instance?.GetCreatureNode(me);
            var targetNode = NCombatRoom.Instance?.GetCreatureNode(target);
            var body = myNode?.Body;
            if (body == null || targetNode == null)
            {
                return;
            }

            bool faceRight = targetNode.GlobalPosition.X >= myNode.GlobalPosition.X;
            float mag = Mathf.Abs(body.Scale.X);
            body.Scale = new Vector2(faceRight ? mag : -mag, body.Scale.Y);
        }
        catch
        {
            // 转向失败不影响战斗。
        }
    }

    /// <summary>回合结束等时机把朝向复位为面右（默认朝向），避免残留翻转。</summary>
    public static void ResetFacing(Creature me)
    {
        try
        {
            var body = NCombatRoom.Instance?.GetCreatureNode(me)?.Body;
            if (body == null)
            {
                return;
            }

            float mag = Mathf.Abs(body.Scale.X);
            body.Scale = new Vector2(mag, body.Scale.Y);
        }
        catch
        {
        }
    }
}
