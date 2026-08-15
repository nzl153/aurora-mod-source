using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// 过热代价改写（A 过热稀有地基，超频用）。<see cref="HeatPower.SettleOverheatAsync"/> 在「受伤前监听」之后、
/// 施加过热伤害之前查询：若某 Power 接管了本次代价（如超频改为按层失最大生命），返回 <c>true</c> → 结算中心<b>跳过</b>常规过热伤害；
/// 返回 <c>false</c> → 照常受可格挡过热伤害（并可被灰烬复燃拦截、计入极限断裂）。
/// <paramref name="originalDamage"/> = 本次原定过热伤害（供改写方参考或回退）。
/// 约定：接管方（超频）自行保证「付不起最大生命则不扣、返回 false 回退原伤害」；其自身的最大生命代价<b>不</b>走自损服务
/// （不计极限断裂、不触发灰烬）。同时存在多个改写器时，第一个返回 true 的接管，其余跳过。
/// </summary>
public interface IAuroraOverheatCostModifier
{
    Task<bool> TryApplyOverheatCostAsync(PlayerChoiceContext ctx, Creature owner, int originalDamage);
}
