using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace AuroraMod.AuroraCode.Cards.Uncommon;

/// <summary>
/// H-U36 热态路由 / Thermal Routing（罕见，枢纽；能力）—— 收尾第 36 张罕见（回到 13攻/16技/7能）。
/// 回合开始时按当前热区分流：冷区 4 格挡 / 温区抽 1 / 过载区 1 能量。升级：费用 2→1。
/// 结算：经 <see cref="AuroraThermalRoutingPower"/>（回合开始触发，多层线性叠加）。打出当回合不触发，从下个本人回合起生效。
/// 与已有枢纽能力错位：余热装甲(过热前盾)/相变护层(换区盾)/冷却循环(散热抽牌)/本卡(回合开始按区段分流)。
/// </summary>
public class AuroraThermalRouting() : AuroraCard(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override string ArtName => "thermal_routing";

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Heat];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        await AuroraPowerCmd.Apply<AuroraThermalRoutingPower>(choiceContext, creature, 1, creature, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);   // 2 → 1
    }
}
