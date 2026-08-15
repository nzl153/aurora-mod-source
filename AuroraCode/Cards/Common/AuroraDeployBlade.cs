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
/// 15 部署：利刃 / Deploy: Blade（普通，C 悬浮武装入门）。部署 1 枚攻击模块（基础值 4）。升级：新模块初始值 5（即自带 1 强化）。
/// 只调 <see cref="AuroraModuleController.DeployAsync"/>；卡牌绝不直接 Apply/Remove/Modify 模块 Power。不调热、不立即触发、不算攻击牌。
/// 满槽时由 DeployAsync 内部走同步选择替换（本牌不感知）。
/// </summary>
public class AuroraDeployBlade() : AuroraCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override string ArtName => "deploy_blade";

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.AttackModule];

    // ModuleValue 仅供卡面显示与部署值来源：4（升级 5）。控制器据此建模块，不由本牌事后改 Power。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("ModuleValue", AttackModulePower.BaseDamage),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        var value = (int)DynamicVars["ModuleValue"].BaseValue;
        await AuroraModuleController.DeployAsync(choiceContext, creature, ModuleKind.Attack, this, value);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["ModuleValue"].UpgradeValueBy(1m);   // 4 → 5
    }
}
