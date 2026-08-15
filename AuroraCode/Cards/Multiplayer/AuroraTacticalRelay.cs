using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace AuroraMod.AuroraCode.Cards.Multiplayer;

/// <summary>
/// MP-02 战术接力 / Tactical Relay（联机专属·稀有，D 连锁/H 枢纽；消耗）。目标队友抽 1 牌并获得 1 能量；若打出前你已连锁，改为该队友抽 2 牌+获得 1 能量并使你积 2 热。消耗。升级费用 1→0。
/// 已连锁档能量 2→1（原能量 2 过强）。结算：读出牌者打出前连锁快照一次 → 未连锁给目标抽1+能量1；已连锁改为抽2+能量1、随后<b>出牌者</b>积 2 热。高档完全替代低档（禁叠成抽3）。
/// 抽牌/能量给目标队友（其 Player），连锁读出牌者自己（不推进目标连锁）；目标失效则整张牌停止、出牌者不积热。<see cref="CardMultiplayerConstraint.MultiplayerOnly"/>。
/// </summary>
public class AuroraTacticalRelay() : AuroraCard(1, CardType.Skill, CardRarity.Rare, TargetType.AnyAlly)
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override string ArtName => "tactical_relay";

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Chain, AuroraMechanic.Heat];

    private const int HeatGain = 2;

    // 本系列（含 Echo/复制额外结算）共用的"打出前已连锁"快照：仅在 IsFirstInSeries 读取一次，Echo 段沿用。
    // 禁止在 Echo 段重新 GetIsChained——本牌作为第 3 张手动牌时 AfterCardPlayed 已把连锁 +1，Echo 重读会误升高档。
    private bool _seriesChained;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var self = Owner?.Creature;
        var target = cardPlay.Target;
        if (self == null || target == null || !target.IsAlive)
        {
            return;   // 目标失效：整张牌停止，出牌者不积热。
        }

        var targetPlayer = target.Player;
        if (targetPlayer == null)
        {
            return;
        }

        if (cardPlay.IsFirstInSeries)
        {
            _seriesChained = ChainPower.GetIsChained(self);   // 打出前连锁快照，本系列锁定
        }

        var special = _seriesChained;   // Echo 沿用首段快照，高档完全替代低档、不叠加

        if (special)
        {
            // 已连锁档改为「队友抽 2 + 能量 1」（原能量 2 过强，尤其升级 0 费后集中资源给主 C），出牌者仍积 2 热。
            await CardPileCmd.Draw(choiceContext, 2, targetPlayer);
            await PlayerCmd.GainEnergy(1, targetPlayer);
            await HeatPower.AddHeatAsync(choiceContext, self, HeatGain, this);   // 出牌者积热
        }
        else
        {
            await CardPileCmd.Draw(choiceContext, 1, targetPlayer);
            await PlayerCmd.GainEnergy(1, targetPlayer);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);   // 1 → 0
    }
}
