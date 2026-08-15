using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace AuroraMod.AuroraCode.Cards.Uncommon;

/// <summary>
/// C-U01 哨戒阵列 / Sentry Array（罕见，C 悬浮模块；能力）。每回合第一次成功部署模块时，新模块获得 1 强化并立即触发 1 次。
/// 升级：强化 1→2（触发次数不变）。
/// 结算：经 <see cref="AuroraSentryArrayPower"/>（Amount=总强化，多张累加；触发固定 1 次），触发逻辑在该 Power 的成功部署监听里。
/// 只监听 DeployAsync 成功路径——模块轮转、满槽取消、部署失败均不触发；每回合门闩至多一次。
/// </summary>
public class AuroraSentryArray() : AuroraCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override string ArtName => "sentry_array";

    protected override IEnumerable<AuroraMechanic> MechanicTips =>
        [AuroraMechanic.AttackModule, AuroraMechanic.ShieldModule, AuroraMechanic.ModuleEnhancement];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("EnhancementAmount", 1m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        await AuroraPowerCmd.Apply<AuroraSentryArrayPower>(
            choiceContext, creature, (int)DynamicVars["EnhancementAmount"].BaseValue, creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["EnhancementAmount"].UpgradeValueBy(1m);   // 1 → 2
    }
}
