using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.Helpers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Cards.Common;

/// <summary>
/// 14 自适应电弧 / Adaptive Arc（普通，枢纽，浅接锁定）。造成 7 伤害；若目标将攻击随后给予 1 虚弱，否则随后施加 2 锁定。
/// 升级：伤害 7→9，虚弱/锁定层数不变。
/// 结算（读意图→攻击→按快照施加）：伤害前只读一次目标意图（含至少一个伤害段=将攻击，与 #08 同判据）；单段 powered
/// 攻击（正常消费目标已有本人锁定）；伤害后若目标存活，攻击意图→施 1 虚弱，否则→ AuroraLockService 施 2 层本人锁定。
/// 两分支互斥；击杀则都不施加；虚弱/锁定均是会被人工制品阻挡的 Debuff。
/// </summary>
public class AuroraAdaptiveArc() : AuroraCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override string ArtName => "adaptive_arc";

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Lock];

    // 虚弱是原生 Debuff，补一条原生悬停说明（对齐 BreachingThrust 的易伤提示做法）。
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<WeakPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8, ValueProp.Move),
        new PowerVar<WeakPower>(1m),
        new DynamicVar("LockStacks", 2m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;

        // 1. 伤害前只读一次目标攻击意图（Attack/DeathBlow 视为将攻击，与 #08 灼热切割同判据）。
        var willAttack = cardPlay.Target?.Monster?.IntendsToAttack ?? false;

        // 2. 单段 powered 攻击（正常消费目标已有本人锁定）。
        var damage = (int)DynamicVars.Damage.BaseValue;
        await CommonActions.CardAttack(this, cardPlay, cardPlay.Target, damage, ValueProp.Move).Execute(choiceContext);

        // 3. 伤害后按快照二选一施加（目标存活才施；击杀则都不施）。
        var target = cardPlay.Target;
        if (creature == null || target == null || !target.IsAlive)
        {
            return;
        }

        if (willAttack)
        {
            await AuroraPowerCmd.Apply<WeakPower>(choiceContext, target, 1m, creature, this);
        }
        else
        {
            await AuroraLockService.ApplyAsync(choiceContext, target, creature, (int)DynamicVars["LockStacks"].BaseValue, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);   // 7 → 9
    }
}
