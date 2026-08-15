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
/// C-R07 聚焦矩阵 / Focused Matrix（稀有，C 悬浮模块；消耗）。选一枚模块，使其获得等同已部署模块数量的强化，再触发 (模块数量+1) 次。消耗。升级：额外 +1 强化。
/// 结算：读已部署模块数 N（≤3）→ 无模块不弹选择、无效果消耗 → 同步选一枚 → 强化 N（升级 N+1）→ 连续触发 N+1 次（只触发被选那枚，Unpowered）。
/// 战斗中途结束停剩余触发、已得强化保留。Echo 每次重读数量并重新同步选。辅助肩架三槽后的集中收益牌，与阵列统制（全阵列齐动）错位。
/// </summary>
public class AuroraFocusedMatrix() : AuroraCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override string ArtName => "focused_matrix";

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<AuroraMechanic> MechanicTips =>
        [AuroraMechanic.AttackModule, AuroraMechanic.ShieldModule, AuroraMechanic.ModuleEnhancement];

    private const int MaxModules = 3;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("BonusEnhance", 0m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        var n = Math.Min(AuroraModuleController.Count(creature), MaxModules);
        if (n == 0)
        {
            return;   // 无模块：不弹选择、无效果消耗。
        }

        var chosen = await AuroraModuleController.ChooseModuleAsync(
            choiceContext, creature, this, new MegaCrit.Sts2.Core.Localization.LocString("combat_messages", "AURORAMOD_MODULE_SELECT"));
        if (chosen == null)
        {
            // 取消选择(≥2 枚时可取消)：本牌仍消耗，Exhaust 由关键词自动处理、无法在此撤回。
            return;
        }

        // 强化 N（升级 N+1），再连续触发 N+1 次（只触发被选那枚）。
        await AuroraModuleController.EnhanceSpecificAsync(
            choiceContext, chosen, n + (int)DynamicVars["BonusEnhance"].BaseValue, null);

        var triggers = n + 1;
        for (var i = 0; i < triggers; i++)
        {
            if (!CombatManager.Instance.IsInProgress)
            {
                break;
            }

            await AuroraModuleController.TriggerInstanceAsync(choiceContext, chosen);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["BonusEnhance"].UpgradeValueBy(1m);   // 0 → 1
    }
}
