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
/// A-R02 葬炉 / Burial Furnace（稀有，A 过热；能力）。每当你结算一次过热后仍存活，获得 3 点力量。升级：3→4。
/// 2/3→3/4——A 流派唯一的成长源，提高 Boss 战的持续输出斜率（触发条件完全不变）。
/// 结算：经 <see cref="AuroraBurialFurnacePower"/>（IAuroraOverheatResolvedListener，完整结算后派发；一笔 Pending 只一次）。
/// 引爆/回合末结算存活、灰烬救命后、超频替代伤害后都触发；创建Pending/红线/重复越线/散热/胜利宽恕/过热致死不触发。多张线性叠加。
/// </summary>
public class AuroraBurialFurnace() : AuroraCard(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override string ArtName => "burial_furnace";

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Heat];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("StrengthPerOverheat", 3m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        await AuroraPowerCmd.Apply<AuroraBurialFurnacePower>(
            choiceContext, creature, (int)DynamicVars["StrengthPerOverheat"].BaseValue, creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["StrengthPerOverheat"].UpgradeValueBy(1m);   // 3 → 4
    }
}
