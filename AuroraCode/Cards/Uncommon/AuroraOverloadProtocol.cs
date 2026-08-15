using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace AuroraMod.AuroraCode.Cards.Uncommon;

/// <summary>
/// D1 过载协议 / Overload Protocol（罕见，D 指令连锁）。抽 2；若打出前已连锁，额外抽 1 并积 1 热。<b>费用恒为 1</b>；升级：连锁额外抽 1→2。
/// 删除原「升级 1→0」——两张升级过载协议 + 快速指令/步进斩/微调 曾可零能量无限抽循环，改为始终 1 费即杜绝。
/// 结算：基础抽牌每次都发（Echo 会重复，符合原版抽牌牌复制行为）；连锁额外抽+积热用 IsFirstInSeries+打出前连锁快照守卫。
/// </summary>
public class AuroraOverloadProtocol() : AuroraCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override string ArtName => "overload_protocol";

    /// <summary>金框：已连锁时额外效果可触发（工坊反馈 #1，沿用原版 Dismantle/Spite 的金框语义）。</summary>
    protected override bool ShouldGlowGoldInternal => AuroraGlow.Chained(this);

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Chain, AuroraMechanic.Heat];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("DrawCount", 2m),
        new DynamicVar("ChainedDraw", 1m),
        new DynamicVar("HeatGain", 1m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        var player = Owner;
        if (creature == null || player == null)
        {
            return;
        }

        var special = cardPlay.IsFirstInSeries && ChainPower.GetIsChained(creature);   // 打出前连锁快照

        await CardPileCmd.Draw(choiceContext, DynamicVars["DrawCount"].BaseValue, player);

        if (special)
        {
            await CardPileCmd.Draw(choiceContext, DynamicVars["ChainedDraw"].BaseValue, player);
            await HeatPower.AddHeatAsync(choiceContext, creature, (int)DynamicVars["HeatGain"].BaseValue, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["ChainedDraw"].UpgradeValueBy(1m);   // 连锁额外抽 1 → 2（费用保持 1，不再 0 费）
    }
}
