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
/// H-R04 势能灌注 / Momentum Infusion（稀有，H 剑势×模块；消耗）。清空全部剑势，每清空 3 势使所有模块 +1 强化（最多 +4），随后使所有模块各触发 1 次。消耗。升级费用 1→0。
/// 结算：<see cref="AuroraMomentumService.ClearAllAsync"/> 取清空量 N → 强化量 min(N÷3, 4) 给每枚现存模块（非分配）→ 所有模块各触发 1 次（Unpowered）。
/// 少于 3 势强化 0 但仍清空并触发；无模块仍清空并消耗、无替代收益。战斗中途结束停剩余触发。B+C 资源转轨核心（把剑势永久投资进整套模块）。
/// </summary>
public class AuroraMomentumInfusion() : AuroraCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override string ArtName => "momentum_infusion";

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<AuroraMechanic> MechanicTips =>
        [AuroraMechanic.Momentum, AuroraMechanic.AttackModule, AuroraMechanic.ShieldModule, AuroraMechanic.ModuleEnhancement];

    private const int PerMomentum = 3;
    private const int MaxEnhance = 4;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        var cleared = await AuroraMomentumService.ClearAllAsync(choiceContext, creature, this);
        var enhance = Math.Min(cleared / PerMomentum, MaxEnhance);

        // 每枚现存模块各获得完整强化量（enhance=0 时 EnhanceAll 内部安全跳过）。
        await AuroraModuleController.EnhanceAllAsync(choiceContext, creature, enhance, null);

        // 无论强化多少，随后所有模块各触发 1 次。
        if (CombatManager.Instance.IsInProgress)
        {
            await AuroraModuleController.TriggerAsync(choiceContext, creature);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);   // 1 → 0
    }
}
