using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace AuroraMod.AuroraCode.Cards.Common;

/// <summary>
/// 16 部署：壁垒 / Deploy: Bulwark（普通，C）。部署 1 枚基础值 5 的护盾模块。升级：成功部署后使本次新部署的那一枚立即触发 1 次。
/// 只调 <see cref="AuroraModuleController"/>；满槽走既有同步替换 UI（本牌不感知）。不调热、不锁定。
/// 升级版「立即触发」取得本次新实例，读部署完成后的实际 Value 触发（不算强化，下回合仍正常触发）；
/// 部署 / 替换选择失败返回 null 时不新增、不触发旧模块。
///
/// 【升级范式：行为解锁，非数值提升】<b>刻意与镜像卡 <see cref="AuroraDeployBlade"/> 不同</b>——
/// 部署：刃 升级提模块基础值（4→5），本牌升级改为解锁「立即触发 1 次」。
/// 理由：护盾模块在<b>回合开始</b>给格挡，本回合部署要等下回合才生效，升级前存在一个空窗；
/// 「立即触发」正是补掉这个空窗，比 +1 格挡更贴合该模块的时序痛点。
/// 故本牌<b>没有 OnUpgrade 覆写</b>：升级效果完全由 OnPlay 里的 <c>IsUpgraded</c> 分支驱动，
/// 不存在需要提升的 DynamicVar（全面审核 P2：原先留有空的 OnUpgrade 空壳，已删除）。
/// </summary>
public class AuroraDeployBulwark() : AuroraCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override string ArtName => "deploy_bulwark";

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.ShieldModule];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("ModuleValue", ShieldModulePower.BaseBlock),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        var value = (int)DynamicVars["ModuleValue"].BaseValue;
        var module = await AuroraModuleController.DeployAsync(choiceContext, creature, ModuleKind.Shield, this, value);

        // 升级：仅在部署成功（取得确切新实例）时，立即触发本次新模块 1 次。
        if (IsUpgraded && module != null)
        {
            await AuroraModuleController.TriggerInstanceAsync(choiceContext, module);
        }
    }
}
