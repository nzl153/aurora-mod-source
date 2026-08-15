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
/// 30 反应炉重拳 / Reactor Piledriver（罕见，A 过热暴走）。造成 18 伤害;本场战斗中过热之后,本牌本场费用不高于 1。
/// 升级:伤害 18→23。
/// 结算:只造成单段 powered 攻击,无调热/引爆/其它副效果。降费由起始遗物挂载的常驻隐藏 Power
/// <see cref="AuroraReactorPiledriverDiscountPower"/> 负责:本场首次过热时把本人牌堆所有本牌
/// SetThisCombat(1, reduceOnly:true)(reduce-only 不会把已 0 费顶回 1;本场持续;新进副本回合开始兜底再扫)。
/// Echo 每次重复完整攻击。
/// </summary>
public class AuroraReactorPiledriver() : AuroraCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override string ArtName => "reactor_piledriver";

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Heat];

    public override AuroraStrikeVfxKind StrikeVfx => AuroraStrikeVfxKind.Heavy;   // 反应炉重拳：重击顿感

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(18, ValueProp.Move),
        new DynamicVar("DiscountCost", 1m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
        {
            return;
        }

        var damage = (int)DynamicVars.Damage.BaseValue;
        await CommonActions.CardAttack(this, cardPlay, cardPlay.Target, damage, ValueProp.Move).Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5m);   // 18 → 23
    }
}
