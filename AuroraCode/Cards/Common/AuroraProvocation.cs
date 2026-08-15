using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Cards.Common;

/// <summary>
/// 11 挑衅 / Provocation（普通，B 挑战协议·剑势）。获得 5 格挡；对目标施加 2 层挑战协议；每实际新增 1 层获得 2 剑势。升级：格挡 5→8。
/// 删除「冷区且成功新增则积 1 热」——普通牌只教协议风险与剑势兑现，不再同时教冷区升温。
/// 协议 1→2 层——风险与收益同时加倍（敌人对你 +20% 而非 +10%，一次给 4 剑势），
/// 让协议在普通位就有分量；每施加者上限 3 天然自限（连打两张第二张 actualAdded=1，只给 2 势）。
/// 结算（格挡→施加协议→按实际新增得剑势）：协议是敌人身上的 Buff，走 <see cref="AuroraChallengeProtocolService.ApplyAsync"/>
/// （不经 Debuff/人工制品通道，每施加者对同敌上限 3）；剑势按服务返回的实际新增层数决定，满层则 actualAdded=0 只保留格挡。
/// </summary>
public class AuroraProvocation() : AuroraCard(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override string ArtName => "provocation";

    protected override IEnumerable<AuroraMechanic> MechanicTips =>
        [AuroraMechanic.ChallengeProtocol, AuroraMechanic.Momentum];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(5, ValueProp.Move),
        new DynamicVar("ProtocolStacks", 2m),
        new DynamicVar("MomentumPerStack", 2m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        // 1. 先获得格挡（即使协议满层也保留）。
        await CreatureCmd.GainBlock(creature, (int)DynamicVars.Block.BaseValue, ValueProp.Move, cardPlay);

        var target = cardPlay.Target;
        if (target == null)
        {
            return;
        }

        // 2. 施加协议，取实际新增层数。
        var stacks = (int)DynamicVars["ProtocolStacks"].BaseValue;
        var actualAdded = await AuroraChallengeProtocolService.ApplyAsync(choiceContext, target, creature, stacks, this);
        if (actualAdded <= 0)
        {
            return;
        }

        // 3. 按实际新增获得剑势。
        var momentum = actualAdded * (int)DynamicVars["MomentumPerStack"].BaseValue;
        await AuroraMomentumService.GainAsync(choiceContext, creature, momentum, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);   // 5 → 8
    }
}
