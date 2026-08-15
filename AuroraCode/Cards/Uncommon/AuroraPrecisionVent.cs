using System.Collections.Generic;
using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Cards.Uncommon;

/// <summary>
/// 55 精密排热 / Precision Vent（罕见，枢纽，消耗）。0 费：最多散 2 热，抽 1。升级散 2→3。
/// 0 费节奏工具，低热仍是单次换手，不成永久循环件。散热+抽牌是完整主体，Echo 每次真实结算重复；最终进消耗堆。
/// </summary>
public class AuroraPrecisionVent() : AuroraCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override string ArtName => "precision_vent";
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Heat];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("VentMax", 2m),
        new DynamicVar("DrawCount", 1m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        var player = Owner;
        if (creature == null || player == null)
        {
            return;
        }

        // 散热（内部先触发冷却循环等监听），不取消 Pending。
        await HeatPower.VentUpToAsync(choiceContext, creature, (int)DynamicVars["VentMax"].BaseValue, this);

        // 无论实际散了几点，随后抽牌（原生抽牌语义，可洗牌）。
        if (CombatManager.Instance.IsInProgress)
        {
            await CardPileCmd.Draw(choiceContext, DynamicVars["DrawCount"].BaseValue, player);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["VentMax"].UpgradeValueBy(1m);   // 2 → 3
    }
}
