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
/// 势能灌注（稀有，消耗）：最多消耗12剑势，每3点使所有模块获得1强化，随后全部触发1次。
/// 不足3点仍消耗并触发；无模块时仍支付剑势。升级费用1→0，强化与触发规则保持不变。
/// </summary>
public class AuroraMomentumInfusion() : AuroraCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override string ArtName => "momentum_infusion";

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<AuroraMechanic> MechanicTips =>
        [AuroraMechanic.Momentum, AuroraMechanic.AttackModule, AuroraMechanic.ShieldModule, AuroraMechanic.ModuleEnhancement];

    private const int PerMomentum = 3;
    private const int MaxEnhance = 4;
    private const int MaxMomentumSpent = PerMomentum * MaxEnhance;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("MaxMomentumSpent", MaxMomentumSpent),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        var consumed = await AuroraMomentumService.ConsumeUpToAsync(choiceContext, creature, MaxMomentumSpent, this);
        var enhance = Math.Min(consumed / PerMomentum, MaxEnhance);

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
