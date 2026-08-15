using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// 模块「轮转 / 满槽替换」监听接口。与 <see cref="IAuroraDeployListener"/> 严格错位：
/// 部署监听奖励「向空槽新增」，本监听奖励「已成型阵列上的重构」。
/// <see cref="Helpers.AuroraModuleController.RotateAsync"/> 成功轮转、以及 <see cref="Helpers.AuroraModuleController.DeployAsync"/>
/// 在<b>满槽替换</b>成功新增后，携带操作后的确切实例 <paramref name="resultModule"/> 于同步 action 链内 await 派发一次。
/// 约定：向空槽的普通部署不派发本事件（那走部署监听）；取消/失败不派发；快照派发；不消耗 RNG；每个拥有者独立。
/// </summary>
public interface IAuroraModuleRotateListener
{
    Task OnModuleRotatedAsync(PlayerChoiceContext ctx, Creature owner, AuroraModulePower resultModule);
}
