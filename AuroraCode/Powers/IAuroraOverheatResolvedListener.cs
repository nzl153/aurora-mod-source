using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// 「过热完整结算后」监听（A 过热稀有地基，葬炉用）。<see cref="HeatPower.SettleOverheatAsync"/> 在一笔待结算过热
/// <b>真正结算完成</b>（受代价/伤害 → 角色存活 → 清热 → 安排宕机）之后派发一次，携带本场过热序号。
/// 不派发的情况：只创建 Pending / 红线积热 / 重复越线 / 散热 / 胜利宽恕 / 过热把角色打死（流程在清热前终止）。
/// 与既有 <see cref="IAuroraOverheatListener"/>（受伤前）区分：那个在过热伤害之前，这个在完整结算后且角色仍存活。
/// </summary>
public interface IAuroraOverheatResolvedListener
{
    Task OnOverheatResolvedAsync(PlayerChoiceContext ctx, Creature owner, int overheatIndex);
}
