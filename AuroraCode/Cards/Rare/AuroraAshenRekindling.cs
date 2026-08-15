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
/// A-R04 灰烬复燃 / Ashen Rekindling（稀有，A 过热；能力）。获得 1 层灰烬复燃：当自己的牌或过热将使生命降至 0 时消耗 1 层，
/// 改为保留 1 生命并获得 2 能量（不在行动阶段则下回合开始时获得）。升级：费用 1→0。
/// 结算：经 <see cref="AuroraAshenRekindlingPower"/>（ShouldDie/AfterPreventingDeath 拦截，仅自损作用域、绝不拦敌人攻击）。
/// 多张/Echo 各 +1 层可多次救命；超频成功替代过热伤害不触发、超频付不起回退原伤害可触发；胜利宽恕不消耗层数。
/// </summary>
public class AuroraAshenRekindling() : AuroraCard(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override string ArtName => "ashen_rekindling";

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Heat];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("ChargesGranted", 1m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        await AuroraPowerCmd.Apply<AuroraAshenRekindlingPower>(
            choiceContext, creature, (int)DynamicVars["ChargesGranted"].BaseValue, creature, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);   // 1 → 0
    }
}
