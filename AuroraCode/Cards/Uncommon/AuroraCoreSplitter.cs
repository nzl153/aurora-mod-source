using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Cards.Uncommon;

/// <summary>
/// 31 裂芯斩 / Core Splitter（罕见，A 过热暴走）。造成 16 伤害;若悬停显示的下一次过热伤害≥12,改为造成 2 次。
/// 升级:每段 16→20。
/// 门槛 14→12——让罕见位也有可达成的小终结技,补前期输出(基础伤害 16/20 不变)。
/// 结算(每次真实结算读一次预告值决定段数):nextOverheatDamage = <see cref="HeatPower.ProjectedOverheatDamageFor"/>
/// —— <b>与热量悬停「预计过热伤害」完全同一 API</b>(见 HeatPower.AfterApplied 对 DynamicVars["NextOverheatDamage"] 的赋值),
/// 故牌面判据与玩家看到的数字永远一致。该 API 语义:无 Pending → 下一次过热的基础档(10/12/14/16);
/// 已 Pending → Max(已锁定峰值 LockedDamage, 实时预计),散热不降。≥12 → 2 段,否则 1 段。
/// 【勿改回 NextOverheatDamageFor】那个只返回「按已过热次数推的基础档」,不含红线附加/重复越线附加、也不读已锁定峰值,
/// 会导致红线锁定 ≥12 时悬停显示够门槛、实结算却只打 1 段。逐段独立结算(力量/易伤/过载/取整/锁定+2/格挡/伤害Cap),
/// 每段前守卫战斗/目标/死亡,不转移目标。本牌不改变热量/过热次数/预告值。
/// 双段是主体伤害形态,Echo 每次真实结算都重新读取并执行 1/2 段(不加 IsFirstInSeries 守卫)。
/// </summary>
public class AuroraCoreSplitter() : AuroraCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override string ArtName => "core_splitter";

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Heat, AuroraMechanic.Lock];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(16, ValueProp.Move),
        new DynamicVar("OverheatThreshold", 12m),
        new DynamicVar("HitCount", 2m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        var nextOverheat = HeatPower.ProjectedOverheatDamageFor(creature);
        var hits = nextOverheat >= (int)DynamicVars["OverheatThreshold"].BaseValue
            ? (int)DynamicVars["HitCount"].BaseValue
            : 1;

        var damage = (int)DynamicVars.Damage.BaseValue;
        for (var i = 0; i < hits; i++)
        {
            // 段间守卫:战斗已结束或目标已死则停手,不转移目标。
            if (!CombatManager.Instance.IsInProgress || cardPlay.Target == null || cardPlay.Target.IsDead)
            {
                break;
            }

            await CommonActions.CardAttack(this, cardPlay, cardPlay.Target, damage, ValueProp.Move).Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);   // 每段 16 → 20
    }
}
