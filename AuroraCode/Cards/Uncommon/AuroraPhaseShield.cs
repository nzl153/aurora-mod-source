using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Cards.Uncommon;

/// <summary>
/// 60 相变护层 / Phase Shield（罕见，枢纽；能力）。每回合第一次换区获得 4 格挡。升级 4→6。
/// 奖励在三区间主动移动，不奖励红线内堆热；每回合一次，避免微调/通量剑刷无限防御。
/// 结算：挂/叠 <see cref="AuroraPhaseShieldPower"/>，Amount += Block。触发逻辑全在该 Power 的换区监听里。
/// </summary>
public class AuroraPhaseShield() : AuroraCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override string ArtName => "phase_shield";

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Heat, AuroraMechanic.ZoneChange];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(4, ValueProp.Move),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        await AuroraPowerCmd.Apply<AuroraPhaseShieldPower>(
            choiceContext, creature, (int)DynamicVars.Block.BaseValue, creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);   // 4 → 6
    }
}
