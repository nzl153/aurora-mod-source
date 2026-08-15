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
/// 19 快速指令 / Rapid Command（普通，D 指令连锁）。造成 4 伤害；若打出时位于手牌最右侧，改为造成 6 伤害并积 1 热。
/// 升级：普通 4→6，最右侧 6→8，积热不变。
/// 结算：读手牌位置快照（<see cref="AuroraHandPositionSnapshot"/>，在 OnPlayWrapper prefix 于移出手牌前捕获）；
/// 仅「手动首段打出且位于最右侧」走 6/8 分支——该分支只替换本段基础伤害（不是 4 再追加 2），伤害后积 1 热；
/// 否则走 4/6 普通分支不调热。自动打出/复制/额外结算一律普通分支。
/// </summary>
public class AuroraRapidCommand() : AuroraCard(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override string ArtName => "rapid_command";

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Heat];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(4, ValueProp.Move),
        new DynamicVar("RightDamage", 6m),
        new PowerVar<HeatPower>(1m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;

        // 最右侧奖励仅限「手动、本系列首段、打出时确为手牌最右侧」；自动/复制/额外结算走普通分支。
        var rightmost = AuroraHandPositionSnapshot.WasRightmost(this)
            && cardPlay.IsFirstInSeries
            && !cardPlay.IsAutoPlay;

        var damage = rightmost
            ? (int)DynamicVars["RightDamage"].BaseValue
            : (int)DynamicVars.Damage.BaseValue;

        // 单段 powered 攻击（最右侧只是替换本段基础值，不拆成两段）。
        await CommonActions.CardAttack(this, cardPlay, cardPlay.Target, damage, ValueProp.Move).Execute(choiceContext);

        // 最右侧分支伤害后积 1 热（即使击杀目标仍积）。
        if (rightmost && creature != null)
        {
            await HeatPower.AddHeatAsync(choiceContext, creature, 1, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);              // 4 → 6
        DynamicVars["RightDamage"].UpgradeValueBy(2m);      // 6 → 8
    }
}
