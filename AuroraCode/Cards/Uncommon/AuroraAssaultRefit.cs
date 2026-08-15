using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace AuroraMod.AuroraCode.Cards.Uncommon;

/// <summary>
/// C-U02 强袭重装 / Assault Refit（罕见，C 悬浮模块）。部署 1 枚攻击模块；若成功，使所有攻击模块各触发 2 次，然后积 1 热。
/// 升级：费用 3→2。
/// 结算：DeployAsync 攻击模块 → 成功才继续 → 按 TriggerCount 轮、每轮 TriggerAsync(Attack) 触发全体攻击模块（含刚部署的，每轮重读存活敌人/锁定）
/// → 战斗仍在进行则积 1 热。满槽替换取消则不齐射不积热。模块触发走中心 Unpowered 路径（不吃过载、不算攻击、不推连锁）。
/// 与哨戒阵列共存时：部署阶段先由哨戒强化+触发新模块 1 次，再执行本卡两轮齐射。胜利宽恕：模块齐射结束战斗则不积热。
/// </summary>
public class AuroraAssaultRefit() : AuroraCard(3, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override string ArtName => "assault_refit";

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.AttackModule, AuroraMechanic.Heat];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("TriggerCount", 2m),
        new DynamicVar("HeatGain", 1m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        // 1. 部署 1 枚攻击模块（满槽走同步替换）；只有成功才继续。
        var deployed = await AuroraModuleController.DeployAsync(choiceContext, creature, ModuleKind.Attack, this);
        if (deployed == null)
        {
            return;
        }

        // 2. 使所有攻击模块各触发 TriggerCount 次（每轮重读当前攻击模块与战场；段间战斗结束则停手）。
        var rounds = (int)DynamicVars["TriggerCount"].BaseValue;
        for (var i = 0; i < rounds; i++)
        {
            if (!CombatManager.Instance.IsInProgress)
            {
                break;
            }

            await AuroraModuleController.TriggerAsync(choiceContext, creature, ModuleKind.Attack);
        }

        // 3. 战斗仍在进行才积热（胜利宽恕）。
        if (CombatManager.Instance.IsInProgress)
        {
            await HeatPower.AddHeatAsync(choiceContext, creature, (int)DynamicVars["HeatGain"].BaseValue, this);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);   // 3 → 2
    }
}
