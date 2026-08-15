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
/// 61 冷却循环 / Cooling Cycle（罕见，枢纽；能力）。每回合第一次实际散热抽 1；升级：触发时再获 3 格挡。升级 TriggerBlock 0→3。
/// 奖励真实降热而非过热自动清零；每回合一次，避免 0 费散热牌无限过牌；没散热牌时基础打击/战术收束/通量剑也能触发。
/// 结算：经 <see cref="AuroraCoolingCyclePower.ApplyAsync"/> 双累计（抽牌数=Amount + 升级格挡），触发逻辑在该 Power 的散热监听里。
/// </summary>
public class AuroraCoolingCycle() : AuroraCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override string ArtName => "cooling_cycle";

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Heat];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("DrawCount", 1m),
        new DynamicVar("TriggerBlock", 0m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        await AuroraCoolingCyclePower.ApplyAsync(
            choiceContext, creature,
            (int)DynamicVars["DrawCount"].BaseValue,
            (int)DynamicVars["TriggerBlock"].BaseValue,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["TriggerBlock"].UpgradeValueBy(3m);   // 0 → 3
    }
}
