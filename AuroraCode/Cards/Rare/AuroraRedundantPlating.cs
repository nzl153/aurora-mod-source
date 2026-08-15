using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace AuroraMod.AuroraCode.Cards.Rare;

/// <summary>
/// C-R?? 冗余装甲 / Redundant Plating（稀有，C 悬浮模块；能力）。回合开始时保留一半格挡。升级：费用 2→1。
/// 结算全在 <see cref="AuroraRedundantPlatingPower"/>（Single 层，多张不叠加）。
/// 与装甲冲撞是配套的一对：本卡负责把格挡留住，装甲冲撞负责把格挡换成伤害。
/// 保留一半而非全留，是为了让这对组合有界（稳态 2×，见 Power 注释）。
/// </summary>
public class AuroraRedundantPlating() : AuroraCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override string ArtName => "redundant_plating";

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.ShieldModule];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        await AuroraPowerCmd.Apply<AuroraRedundantPlatingPower>(choiceContext, creature, 1, creature, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);   // 2 → 1
    }
}
