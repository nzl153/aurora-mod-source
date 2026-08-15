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
/// B-R01 一刀两断 / Sever in One Stroke（稀有，B 剑势）。清空全部剑势，造成 8 + 每清空 1 势×4 伤害。升级每势 4→5。
/// 每势 3/4→4/5——提高 B 唯一无上限倾泻终结技的 Boss 兑现（基础伤害 8 不变）。
/// 结算顺序由「先清空再打」改为<b>「读快照 → 打 → 再清空」</b>。旧顺序会在攻击结算前把剑势归零，
/// 使剑势被动底薪（每 4 势 +1，<see cref="MomentumPower.ModifyDamageAdditive"/>）读到 0 而失效——
/// 20 势时实际 88 / 卡面预览 93 对不上，且这份损失玩家看不见（攒越多亏越多的隐形陷阱）。
/// 新顺序让底薪照常生效，与不清空的 <see cref="AuroraMoonlessBlade"/> 行为一致。
/// 结算：读剑势快照 N（<b>绝不清空后读 0</b>）→ 合并为单段 powered 攻击（力量/过载只结算一次）→ 最后清空。
/// 本卡与攻击流程都不改动剑势，故快照与实际清空量必然一致。0 势仍造成 8 基础伤害（覆盖旧稿 0 势 0 伤）。
/// Echo 额外结算时剑势已被首段清空，后续通常只 8 基础伤害。B 唯一无上限剑势倾泻终结技。
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

        // 【顺序：读快照 → 攻击 → 再清空】不可改回「先清空再打」。
        // 旧顺序会让剑势在攻击结算前归零，导致剑势被动底薪（每 4 势 +1，见 MomentumPower.ModifyDamageAdditive）
        // 读到 0 而失效——20 势时实际伤害 88、卡面预览 93，对不上，且这份损失玩家完全看不见
        // （攒得越多亏得越多的隐形陷阱）。改为攻击后再清空：底薪照常生效，与不清空的无月行为一致。
        var snapshot = AuroraMomentumService.Get(creature);
        var dmg = (int)DynamicVars.Damage.BaseValue + snapshot * (int)DynamicVars["PerMomentum"].BaseValue;
        await CommonActions.CardAttack(this, cardPlay, cardPlay.Target, dmg, ValueProp.Move).Execute(choiceContext);

        // 清空放在最后：本卡与攻击流程都不会改动剑势，故快照与实际清空量必然一致。
        await AuroraMomentumService.ClearAllAsync(choiceContext, creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["PerMomentum"].UpgradeValueBy(1m);   // 4 → 5
    }
}
