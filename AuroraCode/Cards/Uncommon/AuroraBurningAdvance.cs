using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Cards.Uncommon;

/// <summary>
/// 29 燃烧进军 / Burning Advance（罕见，A 过热暴走）。造成 8 伤害,积 1 热;本回合第一次由此换区时,本牌返回手牌、
/// 本回合费用变 0;再次打出后消耗。升级:伤害 8→11。
/// 因固定 +1 热,普通换区当且仅当打出前热量恰为 3(冷→温)或 6(温→过载);9→过热归零不算换区。
/// 返手判定放在 <see cref="TryResolveReturnToHand"/>(在 OnPlay 之前按打出前热量预判,可精确预知换区):
/// 已武装(ExhaustOnNextPlay)→ 交基类走消耗;否则门闩未用且热量==3/6 → 返回手牌;否则默认弃牌。
/// OnPlay 每次真实结算:1 段攻击 + 积 1 热;仅 IsFirstInSeries 且本次去向被判为手牌时武装
/// (置门闩、ExhaustOnNextPlay、SetThisTurn(0))。用 SetThisTurn 而非 …OrUntilPlayed:后者带 WhenPlayed 标志
/// 会被本次打出的 AfterCardPlayedCleanup 立即清除。ExhaustOnNextPlay 是引擎内置同步标志,联机/重连一致。
/// 1 费罕见位数值上调（对照原版 680 张卡解包统计：奥萝拉 1 费罕见攻击均值 7.2 / 中位 7，
/// 原版 9.4 / 8；格挡 6.1 / 6 对原版 8.4 / 7——该档是全卡池唯一明显洼地，而罕见奖励是玩家整局看得最多的三选一）。
/// </summary>
public class AuroraBurningAdvance() : AuroraCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override string ArtName => "burning_advance";

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Heat, AuroraMechanic.ZoneChange];

    // 牌文写「消耗」,补原生消耗悬停说明(不设 CanonicalKeywords,否则会永远消耗)。
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8, ValueProp.Move),
        new PowerVar<HeatPower>(1),
        new DynamicVar("ReturnCost", 0m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        // 1. 每次真实结算:单段攻击。
        var damage = (int)DynamicVars.Damage.BaseValue;
        await CommonActions.CardAttack(this, cardPlay, cardPlay.Target, damage, ValueProp.Move).Execute(choiceContext);

        // 2. 积 1 热(战斗结束/无效时 AddHeatAsync 内部自守卫)。
        await HeatPower.AddHeatAsync(choiceContext, creature, 1, this);

        // 3. 武装:仅首次结算,且本次去向已被 TryResolveReturnToHand 判为返回手牌。
        if (cardPlay.IsFirstInSeries && cardPlay.ResultPile == PileType.Hand)
        {
            creature.GetPower<AuroraBurningAdvanceTurnPower>()?.MarkReturnUsed();
            AssertMutable();
            ExhaustOnNextPlay = true;                  // 再次打出即消耗(引擎内置同步标志)
            EnergyCost.SetThisTurn((int)DynamicVars["ReturnCost"].BaseValue);  // 本回合 0 费(跨返手+重打存活)
        }
    }

    /// <summary>回合开始由 <see cref="AuroraBurningAdvanceTurnPower"/> 调用:清除未重打的武装。</summary>
    public void DisarmReturn()
    {
        if (!ExhaustOnNextPlay)
        {
            return;
        }

        AssertMutable();
        ExhaustOnNextPlay = false;
    }

    /// <summary>
    /// 判定本次打出后是否应返回手牌。返回 null = 交给基类决定。
    /// <b>不要改成先调用基类再比较</b>：基类实现带副作用（命中消耗分支时会把 ExhaustOnNextPlay 清掉），
    /// 提前调用会在"本应返回手牌"的路径上误清武装标志。
    /// 抽出来是为了让正式版与 beta 两个签名共用同一份判定逻辑，避免双分支漂移。
    /// </summary>
    private PileType? TryResolveReturnToHand()
    {
        // 已武装(第二次打出)或本身带消耗 → 交基类。
        if (ExhaustOnNextPlay || Keywords.Contains(CardKeyword.Exhaust))
        {
            return null;
        }

        // 门闩未用且打出前热量恰在边界(3 或 6)→ +1 必然换区 → 返回手牌。
        var creature = Owner?.Creature;
        if (creature != null && !AuroraBurningAdvanceTurnPower.HasUsedReturnThisTurn(creature))
        {
            var heat = HeatPower.GetHeat(creature);
            if (heat == 3 || heat == 6)
            {
                return PileType.Hand;
            }
        }

        return null;
    }

#if STS2_BETA
    protected override CardLocation GetResultLocationForCardPlay()
    {
        var pile = TryResolveReturnToHand();
        return pile.HasValue
            ? new CardLocation(Owner, pile.Value, CardPilePosition.Bottom)
            : base.GetResultLocationForCardPlay();
    }
#else
    protected override PileType GetResultPileTypeForCardPlay()
    {
        return TryResolveReturnToHand() ?? base.GetResultPileTypeForCardPlay();
    }
#endif

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);   // 8 → 11
    }
}
