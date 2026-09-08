using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.DynamicVars;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Cards.Rare;

/// <summary>
/// 无月（稀有，保留）：基础10伤害，每点剑势额外2/3伤害，最多计算21剑势。
/// 读取上限与本轮剑势基础增伤达到上限所需的21点对齐；剑势被动由伤害钩子另行叠加。
/// </summary>
public class AuroraMoonlessBlade() : AuroraCard(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override string ArtName => "moonless_blade";

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Momentum];

    public override AuroraStrikeVfxKind StrikeVfx => AuroraStrikeVfxKind.Ultimate;   // 招牌终结技：大招紫刀光

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new AuroraScalingDamageVar(10, ValueProp.Move, c =>
            Math.Min(MomentumPower.Get(c.Owner?.Creature), (int)c.DynamicVars["MomentumCap"].BaseValue)
            * (int)c.DynamicVars["PerMomentum"].BaseValue),
        new DynamicVar("PerMomentum", 2m),
        new DynamicVar("MomentumCap", 21m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        var momentum = Math.Min(AuroraMomentumService.Get(creature), (int)DynamicVars["MomentumCap"].BaseValue);
        var dmg = (int)DynamicVars.Damage.BaseValue + momentum * (int)DynamicVars["PerMomentum"].BaseValue;
        await AuroraCardAttack.Create(this, cardPlay, cardPlay.Target, dmg, ValueProp.Move).Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["PerMomentum"].UpgradeValueBy(1m);   // 2 → 3（读取上限不变，恒为 21）
    }
}
