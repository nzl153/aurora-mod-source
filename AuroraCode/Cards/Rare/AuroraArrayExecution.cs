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

namespace AuroraMod.AuroraCode.Cards.Rare;

/// <summary>
/// H-R03 阵列处决 / Array Execution（稀有，H 模块×连锁）。造成 14 伤害；若打出前已连锁且模块满槽，改为 42 伤害、使所有模块各触发 1 次、随后积 2 热。升级 14/42→17/52。
///
/// 强化档 28/34 → 42/52——修正<b>门槛与回报倒挂</b>。本牌是全卡池<b>唯一要求同时满足两套系统</b>
/// 的处决（已连锁 + 模块满 3 槽），达成难度高于任何单流派终结技；而改前每费兑现仅 21，是九张终结技里<b>垫底</b>
/// （编队突击只要满槽就 27/费）。上调后约 38/费，落在极限断裂(35)与拆械斩(46)之间——
/// 高于所有单条件小终端，但仍低于一刀两断(70/费)与湮灭指令(60/费)，因为本牌<b>不消耗模块也不消耗剑势</b>
/// （模块触发后仍留在场上），不像一刀两断那样把攒了整局的资源一次性烧掉，故不应达到其效率。
/// 基础档 14/17 <b>不动</b>：未达成条件时它只是一张平庸的 2 费攻击，这个惩罚正是高门槛卡的设计代价。
///
/// 结算：同时读打出前连锁 + 模块满槽（有效容量，辅助肩架后须 3/3）→ 强化档=单段 powered 42/52（非两段）→ 战斗仍在则全模块各触发 1 次（Unpowered）→ 仍在则积 2 热。
/// 攻击/触发结束战斗则停并跳过积热。Echo 完整重复但不推进连锁、每次重查满槽。C+D 招牌处决：要求同时完成阵列建设与连锁启动。
/// </summary>
public class AuroraArrayExecution() : AuroraCard(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override string ArtName => "array_execution";

    /// <summary>金框：已连锁且模块满槽时额外效果可触发（工坊反馈 #1，沿用原版 Dismantle/Spite 的金框语义）。</summary>
    protected override bool ShouldGlowGoldInternal => AuroraGlow.ChainedWithFullModules(this);

    protected override IEnumerable<AuroraMechanic> MechanicTips =>
        [AuroraMechanic.Chain, AuroraMechanic.AttackModule, AuroraMechanic.ShieldModule, AuroraMechanic.Heat];

    private const int HeatGain = 2;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(14, ValueProp.Move),
        new DynamicVar("EmpoweredDamage", 42m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        // 打出前一次性读：已连锁 && 模块满槽。special 含 IsFirstInSeries ⇒ Echo/复制的额外结算恒走基础档
        // （不推进连锁；强化档只在首次真实结算判定，不会因每次"重查满槽"而在额外结算里再进强化档）。
        var special = cardPlay.IsFirstInSeries
                      && ChainPower.GetIsChained(creature)
                      && AuroraModuleController.IsFull(creature);
        var dmg = special
            ? (int)DynamicVars["EmpoweredDamage"].BaseValue
            : (int)DynamicVars.Damage.BaseValue;

        await CommonActions.CardAttack(this, cardPlay, cardPlay.Target, dmg, ValueProp.Move).Execute(choiceContext);

        if (special)
        {
            if (CombatManager.Instance.IsInProgress)
            {
                await AuroraModuleController.TriggerAsync(choiceContext, creature);
            }

            if (CombatManager.Instance.IsInProgress)
            {
                await HeatPower.AddHeatAsync(choiceContext, creature, HeatGain, this);
            }
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);              // 14 → 17
        DynamicVars["EmpoweredDamage"].UpgradeValueBy(10m); // 42 → 52
    }
}
