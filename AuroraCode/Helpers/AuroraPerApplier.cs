using System.Linq;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace AuroraMod.AuroraCode.Helpers;

/// <summary>
/// InstancedPerApplier 归属工具（架构 §8.2 / §9）——挑战协议与锁定共用。
/// 业务身份一律用 <c>Applier.Player.NetId</c>，绝不拿裸 <see cref="Creature"/> 引用当字典键。
/// </summary>
internal static class AuroraPerApplier
{
    /// <summary>取一个生物的稳定业务身份（玩家 NetId）；非玩家或空返回 null。</summary>
    public static ulong? NetIdOf(Creature creature) =>
        creature != null && creature.IsPlayer ? creature.Player?.NetId : null;

    /// <summary>两个施加者是否同一玩家（按 NetId；任一方无合法 NetId 视为不同）。</summary>
    public static bool SameApplier(Creature a, Creature b)
    {
        var na = NetIdOf(a);
        var nb = NetIdOf(b);
        return na.HasValue && nb.HasValue && na.Value == nb.Value;
    }

    /// <summary>在 target 身上找 applier 施加的那一枚每施加者 Power 实例（无则 null）。</summary>
    public static T FindInstance<T>(Creature target, Creature applier) where T : PowerModel =>
        target?.Powers.OfType<T>().FirstOrDefault(p => SameApplier(p.Applier, applier));
}
