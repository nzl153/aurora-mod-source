using System.Linq;
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

namespace AuroraMod.AuroraCode.Cards.Rare;

/// <summary>
/// C-R05 阵列统制 / Array Unification（稀有，C 悬浮模块；消耗）。选攻击或护盾，将所有模块轮转为该类型（保留各自强化），随后所有模块各触发 2 次。消耗。升级费用 1→0。
/// 结算：同步选型（不耗 RNG）→ 已是该型的不动，其余逐枚 <see cref="AuroraModuleController.RotateAsync"/>（保留强化，最先 1~2 次轮转可正常触发自适应底盘）→
/// 全部轮转完再进行两轮全模块触发（Unpowered、不算部署/不触发哨戒）。无模块不弹选择、直接无效果消耗；中途战斗结束则停手。
/// 一次性阵型终结：把长期强化押注进攻(全攻击)或防守(全护盾)。
/// </summary>
public class AuroraArrayUnification() : AuroraCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override string ArtName => "array_unification";

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<AuroraMechanic> MechanicTips =>
        [AuroraMechanic.AttackModule, AuroraMechanic.ShieldModule, AuroraMechanic.ModuleEnhancement];

    private const int TriggerRounds = 2;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        // 无模块：不弹选择，直接无效果消耗。
        if (AuroraModuleController.Count(creature) == 0)
        {
            return;
        }

        var kind = await AuroraModuleController.ChooseModuleTypeAsync(choiceContext, creature, this);
        if (kind == null)
        {
            // 取消选择：本牌仍消耗（Exhaust 由关键词在打出后自动处理，无法在此撤回）。有模块才弹选择，故取消是有意放弃、非误触。
            return;
        }

        // 已是所选类型的保持不变；其余逐枚轮转为所选类型（保留强化）。快照后再轮转，避免边改边遍历。
        var toRotate = AuroraModuleController.Modules(creature).Where(m => m.Kind != kind.Value).ToList();
        foreach (var module in toRotate)
        {
            if (!CombatManager.Instance.IsInProgress)
            {
                break;
            }

            await AuroraModuleController.RotateAsync(choiceContext, creature, module, this);
        }

        // 全部轮转完成后再进行两轮全模块触发。
        for (var i = 0; i < TriggerRounds; i++)
        {
            if (!CombatManager.Instance.IsInProgress)
            {
                break;
            }

            await AuroraModuleController.TriggerAsync(choiceContext, creature);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);   // 1 → 0
    }
}
