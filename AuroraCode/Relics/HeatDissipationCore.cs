using System.Collections.Generic;
using System.Linq;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Relics;

/// <summary>
/// 散热核心 / Heat Dissipation Core —— 奥萝拉起始遗物（架构 §4.5）。
/// 己方回合结束时按最终热量区段触发一次：冷区获 4 格挡；温区先获 2 格挡再对随机敌 2 伤；过载区对随机敌 4 伤。
/// 三区段的「防御 / 混合 / 进攻」身份 + 冷区必须有收益不可改回。数值属首轮实测可调项。
/// </summary>
public class HeatDissipationCore : AuroraRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;
    protected override string ArtName => "heat_core";

    // 三区段数值（强化版覆盖）。冷=纯防御 / 温=混合 / 过载=纯进攻。
    protected virtual int ColdBlock => 4;
    protected virtual int WarmBlock => 2;
    protected virtual int WarmDamage => 2;
    protected virtual int OverloadDamage => 4;

    /// <summary>
    /// 战斗开始统一挂载连锁追踪器（架构 §7.1），使第 1 张牌起就计数。
    /// 挂在起始遗物上，是因为角色模型本身不在 hook 监听列表；常驻遗物是最可靠的战斗初始化宿主。
    /// 本引擎 Apply(0) 不创建 Power，故先 Apply(1) 再归零。子类（强化散热核心）继承本方法。
    /// </summary>
    public override async Task BeforeCombatStart()
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        if (creature.GetPower<ChainPower>() == null)
        {
            await AuroraMod.AuroraCode.Helpers.AuroraPowerCmd.Apply<ChainPower>(creature, 1, creature, null, silent: true);
            creature.GetPower<ChainPower>()?.ResetCount();
        }

        // 锁定消费器（架构 §9）：隐藏 Power，靠 AfterDamageGiven 在真实伤害段消费本人锁定并追加 +2。
        if (creature.GetPower<AuroraLockConsumePower>() == null)
        {
            await AuroraMod.AuroraCode.Helpers.AuroraPowerCmd.Apply<AuroraLockConsumePower>(creature, 1, creature, null, silent: true);
        }

        // 本回合攻击追踪器：隐藏 Power，供「若本回合未打出攻击」条件读取。Apply(1) 后归零。
        if (creature.GetPower<AuroraAttackTurnTracker>() == null)
        {
            await AuroraMod.AuroraCode.Helpers.AuroraPowerCmd.Apply<AuroraAttackTurnTracker>(creature, 1, creature, null, silent: true);
            creature.GetPower<AuroraAttackTurnTracker>()?.ResetFlag();
        }

        // 燃烧进军返手门闩：隐藏 Power，每回合最多返手一次 + 回合开始清武装。Apply(1) 后归零。
        if (creature.GetPower<AuroraBurningAdvanceTurnPower>() == null)
        {
            await AuroraMod.AuroraCode.Helpers.AuroraPowerCmd.Apply<AuroraBurningAdvanceTurnPower>(creature, 1, creature, null, silent: true);
            creature.GetPower<AuroraBurningAdvanceTurnPower>()?.ResetFlag();
        }

        // 反应炉重拳降费器：隐藏 Power + 过热监听，本场首次过热后把重拳降到不高于 1。Apply(1) 后归零。
        if (creature.GetPower<AuroraReactorPiledriverDiscountPower>() == null)
        {
            await AuroraMod.AuroraCode.Helpers.AuroraPowerCmd.Apply<AuroraReactorPiledriverDiscountPower>(creature, 1, creature, null, silent: true);
            creature.GetPower<AuroraReactorPiledriverDiscountPower>()?.ResetFlag();
        }
    }

    /// <summary>
    /// 己方回合结束时按热区结算一次。用 <c>BeforeSideTurnEnd</c> 而非 <c>AfterSideTurnEnd</c>，两个独立理由：
    /// ① 顺序：引擎 <c>Hook.BeforeTurnEnd</c> 整体跑完才进 <c>CombatManager.DoTurnEnd</c>
    ///    （手牌中感染/灼烧等状态牌在那里扣血）。挂 After 档会「先被感染扣血、后拿到格挡」，
    ///    格挡挡不住本回合的状态牌伤害——工坊反馈 #2 指出的正是这点。
    /// ② 规范：<c>AfterSideTurnEnd</c> 的官方注释明写「Enemy-damaging effects should NOT go in here.
    ///    Put them in BeforeSideTurnEnd instead.」本遗物温区/过载区正是打敌人伤害，原挂法违反该指引。
    /// 不用 <c>BeforeSideTurnEndEarly</c>：那档注释写着「You should usually use BeforeSideTurnEnd instead」。
    /// 副作用：温区/过载区的伤害同样提前，属预期（提前收束战斗）。
    /// 与延迟过热结算的先后不受影响——那个挂在更靠后的 <c>AfterSideTurnEndLate</c>，语义反而更牢。
    /// </summary>
    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        var creature = Owner?.Creature;
        if (creature == null || !CombatManager.Instance.IsInProgress || participants?.Contains(creature) != true)
        {
            return;
        }

        switch (HeatPower.GetZone(creature))
        {
            case HeatPower.HeatZone.Cold:
                // 冷区：纯防御。冷区必须有收益（§4.5）。
                Flash();
                await CreatureCmd.GainBlock(creature, ColdBlock, ValueProp.Unpowered, null);
                break;

            case HeatPower.HeatZone.Warm:
                // 温区：混合。先获格挡，再结算伤害；无合法敌人则保留格挡、跳过伤害。
                Flash();
                await CreatureCmd.GainBlock(creature, WarmBlock, ValueProp.Unpowered, null);
                {
                    var warmTarget = PickTarget(creature);
                    if (warmTarget != null)
                    {
                        await CreatureCmd.Damage(choiceContext, warmTarget, WarmDamage, ValueProp.Unpowered, creature);
                    }
                }

                break;

            case HeatPower.HeatZone.Overload:
            case HeatPower.HeatZone.Critical:
                // 过载（含红线 10+）：纯进攻。临界枚举已不再由 ZoneOf 返回，保留兜底。
                var target = PickTarget(creature);
                if (target != null)
                {
                    Flash();
                    await CreatureCmd.Damage(choiceContext, target, OverloadDamage, ValueProp.Unpowered, creature);
                }

                break;
        }
    }

    /// <summary>
    /// 回合末统一结算待处理过热（延迟过热改造）。用 <c>AfterSideTurnEndLate</c>：与散热核心区段效果所在的
    /// <c>BeforeSideTurnEnd</c> 是不同阶段，且整整隔了一个 <c>DoTurnEnd</c> → 语义严格为
    /// 「先散热核心区段效果、后过热结算」，不依赖 Power/Relic 遍历顺序。仅本人所在回合结束、战斗仍有效、且已锁定过热时结算；结算中心自带幂等（结算即移除待结算 Power）。
    /// 注意：若本回合结束前战斗已胜利收束，本钩子不会触发 → 已锁定过热自动免除（人性化，设计选择）。
    /// </summary>
    public override async Task AfterSideTurnEndLate(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        var creature = Owner?.Creature;
        if (creature == null || !CombatManager.Instance.IsInProgress || participants?.Contains(creature) != true)
        {
            return;
        }

        if (AuroraMod.AuroraCode.Powers.AuroraOverheatPendingPower.IsPending(creature))
        {
            await HeatPower.SettleOverheatAsync(choiceContext, creature, null);
        }
    }

    /// <summary>
    /// 重连/存档恢复的最小修正：回合开始时若热量已 ≥ 阈值却没有待结算 Power
    /// （断线重连可能丢债），补锁一笔。反向不处理：Pending 但 heat&lt;10 属正常（散下线不取消锁定），不在此清除。
    /// 只补锁、不在此结算。
    /// </summary>
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        var creature = Owner?.Creature;
        if (creature == null || player != creature.Player || !CombatManager.Instance.IsInProgress)
        {
            return;
        }

        // 统一走 HeatPower.RepairPendingStateAsync——不仅补锁 Pending，
        // 还把 LockedDamage 补到当前实时峰值（覆盖「Pending 在但 Locked 缺失」），避免散热后历史峰值回不来导致结算偏矮。
        await HeatPower.RepairPendingStateAsync(choiceContext, creature);
    }

    /// <summary>稳定顺序的存活可命中敌人里选一个：唯一候选直接选且不耗 RNG，多名候选一次 CombatTargets RNG（§4.5）。</summary>
    private Creature PickTarget(Creature creature)
    {
        var enemies = creature.CombatState?.HittableEnemies.Where(e => e.IsAlive).ToList();
        if (enemies is not { Count: > 0 })
        {
            return null;
        }

        return enemies.Count == 1 ? enemies[0] : Owner.RunState.Rng.CombatTargets.NextItem(enemies);
    }
}
