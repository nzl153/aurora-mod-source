using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// 过热监听接口。任何实现它的 Power（模块、能力牌产生的 Power 等）会在过热结算时、
/// 在承受过热伤害<b>之前</b>被 <see cref="HeatPower.SettleOverheatAsync"/> 逐个回调。
/// 这是设计文档 §2.2「过热按顺序结算：1.触发所有'过热时'效果」的落地点。
///
/// 监听器约定：
/// - 派发用的是快照，本轮过热开始后新挂的 Listener 不会触发（预期行为）。
/// - 回调时自己可能已被别的监听器移除；若逻辑依赖"自己仍挂在身上"，请自检。
/// - 回调发生在过热伤害<b>之前</b>；不要假设回调之后 owner 一定还活着。
/// </summary>
public interface IAuroraOverheatListener
{
    /// <param name="overheatIndex">本场战斗第几次过热（从 1 起），供递增/阈值类效果使用。</param>
    Task OnOverheatAsync(PlayerChoiceContext ctx, Creature owner, int overheatIndex);
}
