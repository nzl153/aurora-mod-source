using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// 模块「成功部署」监听接口（batch C-U01 哨戒阵列）。<see cref="Helpers.AuroraModuleController.DeployAsync"/>
/// 在一次<b>真正成功新增</b>模块后（<paramref name="newModule"/> 非空）、于同步 <see cref="PlayerChoiceContext"/>
/// action 链内 await 派发一次，携带本次新部署的确切实例。
/// 约定：只在 DeployAsync 成功路径派发——满槽替换取消/部署失败不派发；<b>模块轮转(RotateAsync 走 AddAsync)不派发</b>；
/// 快照派发；不消耗 RNG；每个拥有者独立。用于哨戒阵列（首次部署强化+触发新模块），后续 C 遗物/能力复用同一事件。
/// </summary>
public interface IAuroraDeployListener
{
    Task OnModuleDeployedAsync(PlayerChoiceContext ctx, Creature owner, AuroraModulePower newModule);
}
