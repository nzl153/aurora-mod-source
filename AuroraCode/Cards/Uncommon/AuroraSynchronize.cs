using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.Helpers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Cards.Uncommon;

/// <summary>
/// 41 同步 / Synchronize（罕见，C 悬浮模块）。获得 5 格挡，<b>选择 1 枚模块使其触发 2 次</b>。升级：格挡 5→8，触发次数不变。
/// 从「所有模块各触发 1 次」改为单模块聚焦（原效果与联防协议/交叉火力/阵列统制重复）。
/// 结算：先获得格挡；若有模块则走同步 PlayerChoiceContext 选 1 枚 → 触发 2 次（均 Unpowered）；无模块不弹选择、只得格挡不空牌；战斗结束后停止剩余触发。只触发本人模块。
/// </summary>
public class AuroraSynchronize() : AuroraCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    private const int TriggerCount = 2;

    protected override string ArtName => "synchronize";

    protected override IEnumerable<AuroraMechanic> MechanicTips =>
        [AuroraMechanic.AttackModule, AuroraMechanic.ShieldModule];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(5, ValueProp.Move),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        // 1. 基础格挡 5/8。
        await CreatureCmd.GainBlock(creature, (int)DynamicVars.Block.BaseValue, ValueProp.Move, cardPlay);

        // 2. 有模块才弹选择（无模块只得格挡、不空牌）；选中的一枚触发 2 次，每次触发前查战斗仍在进行。
        if (AuroraModuleController.Count(creature) <= 0)
        {
            return;
        }

        var module = await AuroraModuleController.ChooseModuleAsync(choiceContext, creature, this);
        if (module == null)
        {
            return;
        }

        for (var i = 0; i < TriggerCount; i++)
        {
            if (!CombatManager.Instance.IsInProgress)
            {
                break;
            }

            await AuroraModuleController.TriggerInstanceAsync(choiceContext, module);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);   // 5 → 8
    }
}
