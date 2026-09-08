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
/// 势能增幅（稀有，消耗）：获得3 + min(当前剑势, 8)点剑势；升级费用1→0。
/// 每次结算重新读取剑势，额外获取上限独立计算。
/// </summary>
public class AuroraMomentumAmplifier() : AuroraCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override string ArtName => "momentum_amplifier";

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Momentum];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("BaseGain", 3m),
        new DynamicVar("BonusCap", 8m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        var n = AuroraMomentumService.Get(creature);   // 打出前剑势快照
        var bonus = Math.Min(n, (int)DynamicVars["BonusCap"].BaseValue);
        await AuroraMomentumService.GainAsync(choiceContext, creature, (int)DynamicVars["BaseGain"].BaseValue + bonus, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);   // 1 → 0
    }
}
