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
/// 一刀两断（稀有）：造成8 + 剑势×4/5伤害，再清空剑势。
/// 必须先攻击后清空，让实际伤害与包含剑势被动的预览一致。
/// </summary>
public class AuroraSeverInOneStroke() : AuroraCard(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override string ArtName => "sever_in_one_stroke";

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Momentum];

    public override AuroraStrikeVfxKind StrikeVfx => AuroraStrikeVfxKind.Ultimate;   // 招牌终结技：大招紫刀光

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new AuroraScalingDamageVar(8, ValueProp.Move, c =>
            MomentumPower.Get(c.Owner?.Creature) * (int)c.DynamicVars["PerMomentum"].BaseValue),
        new DynamicVar("PerMomentum", 4m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        // 先攻击再清空，确保伤害钩子仍能读取剑势，与卡面预览一致。
        var snapshot = AuroraMomentumService.Get(creature);
        var dmg = (int)DynamicVars.Damage.BaseValue + snapshot * (int)DynamicVars["PerMomentum"].BaseValue;
        await AuroraCardAttack.Create(this, cardPlay, cardPlay.Target, dmg, ValueProp.Move).Execute(choiceContext);

        // 清空放在最后：本卡与攻击流程都不会改动剑势，故快照与实际清空量必然一致。
        await AuroraMomentumService.ClearAllAsync(choiceContext, creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["PerMomentum"].UpgradeValueBy(1m);   // 4 → 5
    }
}
