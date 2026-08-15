using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace AuroraMod.AuroraCode.Cards.Rare;

/// <summary>
/// H-R01 三相指令核 / Tri-Phase Command Core（稀有，H 枢纽；能力）。每回合连锁激活时按当前热区触发：冷区得 4 剑势/温区强化最低模块 2/过载区所有模块各触发 1 次。升级费用 2→1。
/// 结算：经 <see cref="AuroraTriPhaseCommandCorePower"/>（IAuroraChainListener，Amount=层数）。用既有连锁激活事件（每回合至多一次），
/// 第 3 张手动牌结算越阈那刻读一次热区、只执行对应分支。自身作为第 3 张时先上身、随后激活 → 本回合可立即触发。温/过载区无模块则该分支无效果。
/// 跨流派构筑核心：D 负责激活、Heat 决定兑现方向（冷喂 B 剑势 / 温养 C 模块 / 过载即时释放 C 模块）。
/// </summary>
public class AuroraTriPhaseCommandCore() : AuroraCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override string ArtName => "tri_phase_command_core";

    protected override IEnumerable<AuroraMechanic> MechanicTips =>
        [AuroraMechanic.Chain, AuroraMechanic.Heat, AuroraMechanic.Momentum, AuroraMechanic.ModuleEnhancement];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        await AuroraPowerCmd.Apply<AuroraTriPhaseCommandCorePower>(choiceContext, creature, 1, creature, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);   // 2 → 1
    }
}
