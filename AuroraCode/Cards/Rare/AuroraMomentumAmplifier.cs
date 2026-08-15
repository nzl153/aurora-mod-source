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
/// B-R03 势能增幅 / Momentum Amplifier（稀有，B 剑势；消耗）。获得 2 势；打出前每有 1 势再获得 1 势，额外最多 6。消耗。升级费用 1→0。
/// 结算：读打出前剑势 N → 合并一次获得 (2 + min(N, 6)) 势。最少 2、单次最多 8。不清空、不调热、不产能/抽牌。始终消耗。
/// Echo 每次重读当时剑势，额外部分仍各受 6 上限；消耗 + 加成上限阻止指数失控。压缩 B 的蓄势周期。
/// </summary>
public class AuroraMomentumAmplifier() : AuroraCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override string ArtName => "momentum_amplifier";

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Momentum];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("BaseGain", 2m),
        new DynamicVar("BonusCap", 6m),
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
