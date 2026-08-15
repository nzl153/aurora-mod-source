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
/// H-R02 炉心淬锋 / Reactor Tempering（稀有，H 过热×剑势；能力）。每当一次过热完整结算且仍存活，获得 7 剑势。升级 7→10。
/// 5/7→7/10——把过热风险更充分地兑现为剑势爆发（费用/热量绑定/触发条件全不变）。
/// 结算：经 <see cref="AuroraReactorTemperingPower"/>（IAuroraOverheatResolvedListener，Amount=每次结算获得的剑势，多张线性叠加）。
/// 引爆即时结算/回合末结算/超频改失最大生命/灰烬保命后仍存活均触发；仅创建 Pending/红线/散热/胜利宽恕/过热致死不触发。A+B 招牌桥。
/// </summary>
public class AuroraReactorTempering() : AuroraCard(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override string ArtName => "reactor_tempering";

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Heat, AuroraMechanic.Momentum];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("MomentumGain", 7m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        await AuroraPowerCmd.Apply<AuroraReactorTemperingPower>(
            choiceContext, creature, (int)DynamicVars["MomentumGain"].BaseValue, creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MomentumGain"].UpgradeValueBy(3m);   // 7 → 10
    }
}
