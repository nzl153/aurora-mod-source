using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.Helpers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Cards.Common;

/// <summary>
/// 24 反制姿态 / Countermeasure（普通，B 挑战协议·剑势）。造成 9 伤害并施加 1 层挑战协议；
/// 若打出前目标已有【由你施加】的挑战协议，获得 5 格挡与 3 剑势。升级：伤害 9→11，格挡 5→7。
/// 结算（协议归属快照 → 单段 powered 攻击 → 对存活目标施加协议 → 按快照发奖）：
/// 只识别本牌所有者亲自施加的协议，队友协议不算；不消费、不减少、不转移协议。
/// 本牌新施加的协议不能立刻兑现奖励。击杀已有协议的目标仍按快照发奖，但战斗结束后不发。
/// Echo 每次都造成伤害，施加协议与条件收益至多一次。
/// </summary>
public class AuroraCountermeasure() : AuroraCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override string ArtName => "countermeasure";

    protected override IEnumerable<AuroraMechanic> MechanicTips =>
        [AuroraMechanic.ChallengeProtocol, AuroraMechanic.Momentum];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(9, ValueProp.Move),
        new BlockVar(5, ValueProp.Move),
        new DynamicVar("ProtocolStacks", 1m),
        new DynamicVar("MomentumGain", 3m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        var target = cardPlay.Target;

        // 打出前读取一次协议归属快照；本牌随后施加的协议不能立即兑现奖励。
        var hadOwnProtocol = target != null
            && AuroraChallengeProtocolService.GetStacks(target, creature) > 0;
        var isPrimaryResolution = cardPlay.IsFirstInSeries;

        // 1. Echo 的每次结算都正常造成伤害。
        await AuroraCardAttack.Create(this, cardPlay, target, (int)DynamicVars.Damage.BaseValue, ValueProp.Move).Execute(choiceContext);

        // 2. 只在首次序列结算后，对仍存活的目标施加 1 层本人协议。
        if (isPrimaryResolution && target is { IsAlive: true })
        {
            await AuroraChallengeProtocolService.ApplyAsync(
                choiceContext,
                target,
                creature,
                (int)DynamicVars["ProtocolStacks"].BaseValue,
                this);
        }

        // 3. 打出前存在本人协议 → 获得格挡与剑势。
        // 【IsInProgress 守卫】多敌场合击杀单个目标、战斗仍在进行 → 照常发奖；
        // 若本段是收尾斩杀导致战斗结束，则不再 Apply/Modify 剑势 Power（本场已无意义，
        // 且与同 mod AuroraArrayExecution 的战后守卫惯例对齐）。
        if (isPrimaryResolution && hadOwnProtocol && CombatManager.Instance.IsInProgress)
        {
            await CreatureCmd.GainBlock(creature, (int)DynamicVars.Block.BaseValue, ValueProp.Move, cardPlay);
            await AuroraMomentumService.GainAsync(choiceContext, creature, (int)DynamicVars["MomentumGain"].BaseValue, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);   // 9 → 11
        DynamicVars.Block.UpgradeValueBy(2m);    // 5 → 7
    }
}
