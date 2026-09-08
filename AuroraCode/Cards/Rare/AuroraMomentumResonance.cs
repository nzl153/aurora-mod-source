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
/// 剑势共鸣（稀有能力）：回合开始剑势不足10时，每层获得3剑势；否则每层抽1张。
/// 升级费用1→0。打出时无即时收益，实际回合效果由对应Power执行。
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
        new DynamicVar("MomentumGain", 3m),
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
