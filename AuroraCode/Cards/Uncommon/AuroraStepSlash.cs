using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Cards.Uncommon;

/// <summary>
/// D2 步进斩 / Step Slash（罕见，D 指令连锁）。0 费造成 3 伤害；若打出前<b>已连锁</b>，给予 2 层虚弱。升级伤害 3→5。
/// 2026-09-07 改：原为精确「第 4 张手动牌」序列节点（GetCount==3），玩家反馈该措辞与连锁定义混淆且第 5 张起失效不直观，
/// 改为与广播斩/序列打击一致的泛化「已连锁」判定。数值未动（伤害仍 3/5）。
/// 结算：读打出前连锁快照 → 单段 powered 攻击 → 若 IsFirstInSeries && 已连锁 && 目标存活则施 2 虚弱（击杀则不施）。Echo 额外结算只造成基础伤害、不重复施虚弱。
/// </summary>
public class AuroraStepSlash() : AuroraCard(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override string ArtName => "step_slash";

    /// <summary>金框：已连锁时额外效果可触发（工坊反馈 #1，沿用原版 Dismantle/Spite 的金框语义）。</summary>
    protected override bool ShouldGlowGoldInternal => AuroraGlow.Chained(this);

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Chain];

    // 虚弱是原生 Debuff，补一条原生悬停说明（对齐 AdaptiveArc）。
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<WeakPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(3, ValueProp.Move),
        new DynamicVar("WeakStacks", 2m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;

        // 打出前连锁快照，只读一次。Echo 段 IsFirstInSeries=false 天然不重复。
        var special = cardPlay.IsFirstInSeries && creature != null && ChainPower.GetIsChained(creature);

        await AuroraCardAttack.Create(this, cardPlay, cardPlay.Target,
            (int)DynamicVars.Damage.BaseValue, ValueProp.Move).Execute(choiceContext);

        var target = cardPlay.Target;
        if (special && target is { IsAlive: true })
        {
            await AuroraPowerCmd.Apply<WeakPower>(choiceContext, target, (int)DynamicVars["WeakStacks"].BaseValue, creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);   // 3 → 5
    }
}
