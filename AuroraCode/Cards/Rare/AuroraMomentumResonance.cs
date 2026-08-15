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
/// B-R04 剑势共鸣 / Momentum Resonance（稀有，B 剑势；能力）。回合开始时若剑势少于 10 获得 2 剑势，否则抽 1 张牌。升级费用 1→0。
/// 分界 6→10（真值在 Power 的 Threshold）——拉长叠势阶段，让剑势累积到可兑现的爆发量。
/// 结算：经 <see cref="AuroraMomentumResonancePower"/>（Amount=层数，回合开始读一次剑势快照，少于 10 得 2×层数势、否则抽层数张）。
/// 把"是否清空剑势"变长期决策：保留高势持续抽牌（喂无月），清空则回到自动蓄势。打出时无即时收益。
/// </summary>
public class AuroraMomentumResonance() : AuroraCard(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override string ArtName => "momentum_resonance";

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Momentum];

    // 卡面数字改用占位符（值恒为 Power 一层时的收益，本牌只施加 1 层）。
    // 真值权威仍在 AuroraMomentumResonancePower，此处仅供牌面显示——A10 改分界时漏改的就是这层文案。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Threshold", 10m),
        new DynamicVar("MomentumGain", 2m),
        new DynamicVar("DrawCount", 1m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        await AuroraPowerCmd.Apply<AuroraMomentumResonancePower>(choiceContext, creature, 1, creature, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);   // 1 → 0
    }
}
