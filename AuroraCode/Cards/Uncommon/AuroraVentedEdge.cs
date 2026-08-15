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

namespace AuroraMod.AuroraCode.Cards.Uncommon;

/// <summary>
/// B-U?? 泄势斩 / Vented Edge（罕见，B 剑势）。造成 6 伤害；每 {PerStacks} 点剑势追加 {PerBonus} 点伤害，
/// 追加最多 {MaxBonus} 点。<b>不清空剑势。</b>升级：基础伤害 6→9。
///
/// 【为什么加这张】剑势的兑现口原本<b>全在稀有</b>（一刀两断 / 无月），
/// 玩家在抽到稀有之前，剑势除了每 3 点 +1 的底薪之外没有任何可见回报——
/// 这是「剑势没成型感」的第二个来源（第一个是产出普遍只有 2，已同批上调）。
/// 本卡补的就是<b>中期那一档</b>：罕见就能摸到的剑势倾泻口。
///
/// 【为什么不消耗剑势】架构 §8.1 明确规定剑势<b>只支持「清空全部」</b>，
/// 刻意不提供 Spend(N)/Consume(N)——<see cref="AuroraMomentumService"/> 里连这个方法都没有。
/// 原设想的「消耗一半剑势」需要新开部分消费 API，那是动架构，不该顺手做。
/// 改为纯读取式加成后还有两个额外好处：
/// 一是与稀有兑现口<b>不重叠</b>（一刀两断清空换爆发、无月不清空但要 2 费稀有，本卡是 1 费罕见的小口），
/// 二是<b>天然绕开手册 §6 的结算顺序陷阱</b>——本卡不修改那四个影响己方输出的钩子中的任何一个。
///
/// 【为什么封顶】不封顶的话高势局面下 1 费罕见会打出超过稀有兑现口的数字。
/// 封 {MaxBonus} 点（= {PerStacks}×{MaxBonus}/{PerBonus} 点剑势摸顶）把它稳定在罕见档位，
/// 高势的天花板依然属于一刀两断/无月。<b>只封本卡的追加段，剑势计数与底薪完全不受影响。</b>
///
/// 结算：读剑势快照 → 单段 powered 攻击（力量/过载只结算一次）。剑势全程不变。
/// </summary>
public class AuroraVentedEdge() : AuroraCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override string ArtName => "vented_edge";

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Momentum];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new AuroraScalingDamageVar(6, ValueProp.Move, c => BonusFor(
            MomentumPower.Get(c.Owner?.Creature),
            (int)c.DynamicVars["PerStacks"].BaseValue,
            (int)c.DynamicVars["PerBonus"].BaseValue,
            (int)c.DynamicVars["MaxBonus"].BaseValue)),
        new DynamicVar("PerStacks", 3m),
        new DynamicVar("PerBonus", 2m),
        new DynamicVar("MaxBonus", 12m),
    ];

    /// <summary>剑势追加伤害：每 perStacks 点剑势 +perBonus，总加成封顶 maxBonus。</summary>
    private static int BonusFor(int momentum, int perStacks, int perBonus, int maxBonus)
    {
        if (momentum <= 0 || perStacks <= 0)
        {
            return 0;
        }

        return System.Math.Min(momentum / perStacks * perBonus, maxBonus);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        // 只读剑势，不改动——故不触碰手册 §6 的四个钩子，无结算顺序陷阱。
        var bonus = BonusFor(
            AuroraMomentumService.Get(creature),
            (int)DynamicVars["PerStacks"].BaseValue,
            (int)DynamicVars["PerBonus"].BaseValue,
            (int)DynamicVars["MaxBonus"].BaseValue);

        var damage = (int)DynamicVars.Damage.BaseValue + bonus;
        await CommonActions.CardAttack(this, cardPlay, cardPlay.Target, damage, ValueProp.Move).Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);   // 6 → 9
    }
}
