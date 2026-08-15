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
/// D2 步进斩 / Step Slash（罕见，D 指令连锁）。0 费造成 3 伤害；若打出前本回合<b>恰好已手动打出 3 张牌</b>（=激活连锁后的第一张，通常第 4 张手动牌），给予 2 层虚弱。升级伤害 3→5。
/// 从泛化「已连锁就给虚弱」改为精确「第 4 张手动牌」序列节点——连锁阈值=3，GetCount==3 即本卡是刚越阈值后的第一张；第 5 张及以后不再获得。Echo/复制不改变手动出牌计数。
/// 结算：读打出前手动出牌数快照 → 单段 powered 攻击 → 若 IsFirstInSeries && GetCount==3 && 目标存活则施 2 虚弱（击杀则不施）。Echo 额外结算只造成基础伤害、不重复施虚弱。
/// </summary>
public class AuroraStepSlash() : AuroraCard(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    // 连锁阈值 = 3；「恰好已手动打出 3 张」= 本卡是越过阈值后的第一张手动牌。
    private const int SequenceIndex = 3;

    protected override string ArtName => "step_slash";

    /// <summary>金框：本牌正好是本回合第 N 张手动牌时额外效果可触发（工坊反馈 #1，沿用原版 Dismantle/Spite 的金框语义）。</summary>
    protected override bool ShouldGlowGoldInternal => AuroraGlow.ChainCountIs(this, SequenceIndex);

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

        // 打出前手动出牌数快照：恰好 3（本卡=第 4 张手动牌）。Echo 段 IsFirstInSeries=false 天然不重复；
        // 排除自动打出——自动打出即使 Count==3 也不触发、也不推进连锁。
        var special = cardPlay.IsFirstInSeries && !cardPlay.IsAutoPlay
                      && creature != null && ChainPower.GetCount(creature) == SequenceIndex;

        await CommonActions.CardAttack(this, cardPlay, cardPlay.Target,
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
