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
/// A-R01 超频 / Overclock（稀有，A 过热；能力）。获得 1 层超频：每层使过载区攻击 +75% 最终伤害（加法叠加，1层=×2）；
/// 结算过热时不再受过热伤害、改为每层失 1 最大生命；付不起则照常承受原过热伤害。仍清热、生成宕机。升级：费用 3→2。
/// 结算：经 <see cref="AuroraOverclockPower"/>（IAuroraOverheatCostModifier 接管代价 + 供 HeatPower 合成过载倍率）。
/// 最大生命代价不计极限断裂、不触发灰烬；胜利宽恕不扣最大生命。
/// </summary>
public class AuroraOverclock() : AuroraCard(3, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override string ArtName => "overclock";

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Heat, AuroraMechanic.SystemCrash];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        await AuroraPowerCmd.Apply<AuroraOverclockPower>(choiceContext, creature, 1, creature, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);   // 3 → 2
    }
}
