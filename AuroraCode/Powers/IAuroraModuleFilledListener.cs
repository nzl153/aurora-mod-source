using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// 模块「槽位由未满被填满」监听（满载耦合器）。<see cref="Helpers.AuroraModuleController.DeployAsync"/>
/// 在一次<b>向空槽的普通部署</b>成功新增后、且新增后正好满槽时派发一次（携带拥有者）。
/// 约定：满槽替换（满→满）不派发本事件（那走 <see cref="IAuroraModuleRotateListener"/>）；轮转不派发；
/// 部署前已满则不会走到这里。派发给拥有者的 Powers 与 Relics（遗物需要此事件）。快照派发、不消耗 RNG。
/// </summary>
public interface IAuroraModuleFilledListener
{
    Task OnModuleSlotsFilledAsync(PlayerChoiceContext ctx, Creature owner);
}
