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
/// D4 指令缓存 / Command Cache（罕见，D 指令连锁；能力）。每回合第一次激活连锁时抽 2 牌；升级触发时再获 3 格挡并积 1 热。
/// 升级 BlockOnTrigger 0→3、HeatGain 0→1。
/// 结算：经 <see cref="AuroraCommandCachePower.ApplyAsync"/> 三累计（抽牌数=Amount + 升级格挡 + 升级积热），
/// 触发逻辑在该 Power 的 <see cref="IAuroraChainListener"/> 连锁激活回调里，每回合门闩至多一次。
/// </summary>
public class AuroraCommandCache() : AuroraCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override string ArtName => "command_cache";

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Chain, AuroraMechanic.Heat];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("DrawCount", 2m),
        new DynamicVar("BlockOnTrigger", 0m),
        new DynamicVar("HeatGain", 0m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        await AuroraCommandCachePower.ApplyAsync(
            choiceContext, creature,
            (int)DynamicVars["DrawCount"].BaseValue,
            (int)DynamicVars["BlockOnTrigger"].BaseValue,
            (int)DynamicVars["HeatGain"].BaseValue,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["BlockOnTrigger"].UpgradeValueBy(3m);   // 0 → 3
        DynamicVars["HeatGain"].UpgradeValueBy(1m);         // 0 → 1
    }
}
