using System.Collections.Generic;
using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace AuroraMod.AuroraCode.Cards.Uncommon;

/// <summary>
/// C-U04 模块轮转 / Module Rotation（罕见，C 悬浮模块；消耗）。选 1 枚模块轮转为另一类型、保留强化，抽 1 张牌。消耗。
/// 升级：轮转后使新模块立即触发 1 次（仍消耗）。
/// 结算：ChooseModuleAsync 同步选实例 → RotateAsync（保留 Value-BaseValue 强化量，走 AddAsync 不触发部署监听/哨戒）→
/// 升级版 TriggerInstance(轮转后新实例) → 战斗仍在进行则抽牌。无模块不弹窗仍抽牌并消耗。0 费抽牌因始终消耗满足反无限。
/// </summary>
public class AuroraModuleRotation() : AuroraCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override string ArtName => "module_rotation";

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<AuroraMechanic> MechanicTips =>
        [AuroraMechanic.AttackModule, AuroraMechanic.ShieldModule, AuroraMechanic.ModuleEnhancement];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("DrawCount", 1m),
        new DynamicVar("TriggerCount", 0m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        var player = Owner;
        if (creature == null || player == null)
        {
            return;
        }

        // 1. 有模块才选一枚轮转（无模块跳过、不弹窗）。
        if (AuroraModuleController.Count(creature) > 0)
        {
            var chosen = await AuroraModuleController.ChooseModuleAsync(choiceContext, creature, this);
            if (chosen != null)
            {
                var rotated = await AuroraModuleController.RotateAsync(choiceContext, creature, chosen, this);

                // 2. 升级版：触发轮转后的新类型模块 1 次。
                if ((int)DynamicVars["TriggerCount"].BaseValue > 0 && rotated != null)
                {
                    await AuroraModuleController.TriggerInstanceAsync(choiceContext, rotated);
                }
            }
        }

        // 3. 抽牌（战斗仍在进行；消耗由关键词处理）。
        if (CombatManager.Instance.IsInProgress)
        {
            await CardPileCmd.Draw(choiceContext, DynamicVars["DrawCount"].BaseValue, player);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["TriggerCount"].UpgradeValueBy(1m);   // 0 → 1
    }
}
