using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace AuroraMod.AuroraCode.Cards.Rare;

/// <summary>
/// C-R04 自适应底盘 / Adaptive Chassis（稀有，C 悬浮模块；能力）。每回合第一次轮转模块、或满槽成功替换后，使操作后的模块 +3 强化并立即触发 1 次。升级：前 1 次→前 2 次。
/// 结算：经 <see cref="AuroraAdaptiveChassisPower"/>（Amount=每回合可生效次数，多张累加；升级每张 1→2）。只在操作成功后消耗次数——取消/失败/无模块/向空槽普通部署都不触发。
/// 与哨戒阵列错位：哨戒奖励部署、底盘奖励轮转与替换，让模块流有战斗中持续重构阵型的理由。
/// </summary>
public class AuroraAdaptiveChassis() : AuroraCard(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override string ArtName => "adaptive_chassis";

    protected override IEnumerable<AuroraMechanic> MechanicTips =>
        [AuroraMechanic.AttackModule, AuroraMechanic.ShieldModule, AuroraMechanic.ModuleEnhancement];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("MaxUses", 1m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        await AuroraPowerCmd.Apply<AuroraAdaptiveChassisPower>(
            choiceContext, creature, (int)DynamicVars["MaxUses"].BaseValue, creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MaxUses"].UpgradeValueBy(1m);   // 1 → 2
    }
}
