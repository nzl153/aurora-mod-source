using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Visuals;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// 热量 / Heat —— 奥萝拉的核心机制脊柱（单能量资源之外，血条下方的计数 Buff，绝非第二能量池）。
/// 区段：冷 0–3 / 温 4–6 / 过载 7+（10+ 为红线，仍属过载）。
/// 过载区：自己的强力攻击最终伤害 ×1.25（模块伤害非 powered attack，天然排除）。
/// <b>延迟过热·锁定峰值</b>：达到 10 不立即受伤，而是登记一笔待结算过热并<b>锁定伤害峰值</b>（<see cref="AuroraLockedOverheatDamagePower"/>），
/// 回合末/引爆统一结算；散热只降热量、绝不降低已锁定伤害、也不取消。基础伤害本场依次 10/12/14/16 封顶 16，红线每超 10 点 1 热 +2，同笔每重复越线 +4。胜利宽恕整笔免除。
/// </summary>
public sealed class HeatPower : AuroraPower
{
    // 过热阈值：热量首次越过它锁定一笔待结算过热（延迟过热改造）。
    public const int OverheatThreshold = 10;
    // 技术安全上限：热量可超过 10（红线），但钳制到 99 防溢出。玩法上是软上限。
    public const int SafeMaxHeat = 99;
    public const int OverheatDamage = 10;
    // 递增过热（v3 §9.1）：本场第 1/2/3/4+ 次过热造成 10/12/14/16，封顶 16。让 A 越烧越危险也越猛。
    public const int OverheatDamageStep = 2;
    public const int OverheatMaxDamage = 16;
    // 红线附加：结算时每超过阈值 1 点热量 +2 伤害（不封顶）。
    public const int RedlineBonusPerHeat = 2;
    // 重复越线附加：本次待结算期间每额外越线 1 次 +4 伤害（不封顶）。
    public const int RecrossBonusPerCross = 4;
    public const int IgniteMinHeat = 4;
    public const decimal OverloadDamageMultiplier = 1.25m;

    public enum HeatZone { Cold, Warm, Overload, Critical }

    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override string IconName => "heat";

    public int Heat => Math.Clamp((int)Amount, 0, SafeMaxHeat);
    public override int DisplayAmount => Heat;
    protected override bool IsVisibleInternal => Heat > 0;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Heat", 0m),
        // 悬停显示「下一次过热将造成 X 点伤害」（架构 §4.4）。默认 10=本场第 1 次过热伤害。
        new DynamicVar("NextOverheatDamage", OverheatDamage),
    ];

    public HeatZone Zone => ZoneOf(Heat);

    // 区段（延迟过热改造）：10+（红线）仍归 Overload 危险子状态，享受过载增益且满足所有「处于过载区」判定；
    // 不再返回 Critical（保留枚举仅兼容旧 switch 分支），现有消费者读到的最高区段就是 Overload。
    public static HeatZone ZoneOf(int heat) =>
        heat <= 3 ? HeatZone.Cold :
        heat <= 6 ? HeatZone.Warm : HeatZone.Overload;

    /// <summary>红线：热量 ≥ 过热阈值 10（仍属过载，享受 ×1.25），一笔待结算过热已（或将）锁定。</summary>
    public static bool IsRedline(Creature creature) => GetHeat(creature) >= OverheatThreshold;

    public void Configure(int heat)
    {
        AssertMutable();
        DynamicVars["Heat"].BaseValue = Math.Clamp(heat, 0, SafeMaxHeat);
    }

    /// <summary>过载区：作为攻击方的强力攻击最终伤害 ×1.25。模块伤害走 Unpowered，不是 powered attack，自动排除。</summary>
    public override decimal ModifyDamageMultiplicative(Creature target, decimal amount, ValueProp props, Creature dealer, CardModel cardSource)
    {
        if (dealer != Owner || amount <= 0 || !props.IsPoweredAttack() || Zone != HeatZone.Overload)
        {
            return 1m;
        }

        // 过载区基础倍率恒为 ×1.25。超频额外 +0.75×层数【仅在已锁定待结算过热时生效】
        // 7~9 热无 Pending 只吃基础 ×1.25；10+ 或散回过载区但 Pending 仍在则叠加超频；Pending 结算后超频增伤立即停。
        var mult = OverloadDamageMultiplier;
        if (AuroraOverheatPendingPower.IsPending(Owner))
        {
            mult += AuroraOverclockPower.OverloadBonus(Owner);
        }

        return mult;
    }

    public override Task AfterApplied(Creature applier, CardModel cardSource)
    {
        DynamicVars["Heat"].BaseValue = Heat;
        // 每次热量 Power 上身/重建，刷新悬停「预计过热伤害」= 当前实际结算值：
        // 无 Pending 时 = 下一次基础伤害；已 Pending 时 = 已锁定峰值（含红线/重复越线，散热不降）。
        DynamicVars["NextOverheatDamage"].BaseValue = ProjectedOverheatDamageFor(Owner);
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    /// <summary>本场「下一次」过热将造成的伤害 = 第(已过热次数+1)次的递增伤害（10/12/14/16 封顶）。</summary>
    public static int NextOverheatDamageFor(Creature creature) =>
        OverheatDamageForIndex(AuroraOverheatCountPower.Get(creature) + 1);

    // ---------------- 静态操作 API（卡牌/遗物调用） ----------------

    public static int GetHeat(Creature creature) => (int)(creature?.GetPowerAmount<HeatPower>() ?? 0);

    public static HeatZone GetZone(Creature creature) => ZoneOf(GetHeat(creature));

    /// <summary>
    /// 绝对赋值（内部）。value≤0 则移除 Heat 计数。完成后按 <paramref name="reason"/> 派发一次热量变更通知
    /// （before==after 不派发）。<paramref name="reason"/> 决定是否算「换区」「实际散热」（见 <see cref="HeatChangeInfo"/>）。
    /// </summary>
    private static async Task SetHeatAsync(PlayerChoiceContext ctx, Creature creature, int value, CardModel cardSource,
        HeatChangeReason reason)
    {
        var before = GetHeat(creature);
        value = Math.Clamp(value, 0, SafeMaxHeat);

        await PowerCmd.Remove<HeatPower>(creature);
        if (value > 0)
        {
            var power = (HeatPower)ModelDb.Power<HeatPower>().ToMutable();
            power.Configure(value);
            await AuroraPowerCmd.Apply(ctx, power, creature, value, creature, cardSource);
        }

        await DispatchHeatChangeAsync(ctx, creature, before, value, reason, cardSource);
    }

    /// <summary>统一热量变更通知：before==after 不派发；快照迭代监听器；在同步 action 链内 await；不消耗 RNG。</summary>
    private static async Task DispatchHeatChangeAsync(PlayerChoiceContext ctx, Creature creature, int before, int after,
        HeatChangeReason reason, CardModel cardSource)
    {
        if (before == after || creature == null)
        {
            return;
        }

        // 热量柱（纯表现）：唯一的真实变更汇聚点，在此火发一次平滑滑动。
        // fire-and-forget、下一帧执行，绝不进结算链。注意不要改成在 AfterApplied 里 Snap——
        // SetHeatAsync 每次都会重建 Power，那样会先跳到新值，滑动动画就没了。
        AuroraHeatBarBridge.RequestAnimate(creature, after,
            discharge: reason == HeatChangeReason.OverheatClear);

        var info = new HeatChangeInfo(before, after, ZoneOf(before), ZoneOf(after), reason, cardSource);
        foreach (var power in creature.Powers.ToList())
        {
            if (power is IAuroraHeatChangeListener listener)
            {
                await listener.OnHeatChangedAsync(ctx, creature, info);
            }
        }
    }

    /// <summary>
    /// 积热（延迟过热改造）。热量可超过阈值 10 进入红线，<b>不立即受伤、不立即清零</b>；首次越过 10 时锁定一笔
    /// 待结算过热（<see cref="AuroraOverheatPendingPower"/>），本回合继续保有过载增益，回合末统一结算。
    /// 越线判定：delta&gt;0 且原热量 &lt;10、新热量 ≥10 才算一次越线（8→13 只算一次，不按跨了几点重复计数）。
    /// 已锁定后先散到 &lt;10 再升回 ≥10 → 越线次数 +1。已锁定后再升不重复计次。
    /// </summary>
    public static async Task AddHeatAsync(PlayerChoiceContext ctx, Creature creature, int delta, CardModel cardSource)
    {
        if (creature == null || delta == 0 || !CombatManager.Instance.IsInProgress)
        {
            return;
        }

        var current = GetHeat(creature);
        var next = Math.Clamp(current + delta, 0, SafeMaxHeat);

        // 负 delta（如打击过载 -1、战术收束温/过载 -2）：只降热量，不算越线、不碰待结算状态
        //（散下线不取消已锁定过热——与 VentUpTo/Vent 一致）。
        if (delta < 0)
        {
            await SetHeatAsync(ctx, creature, next, cardSource, HeatChangeReason.Vent);
            return;
        }

        // 正 delta 越线：从 <阈值 升到 ≥阈值。锁定/累加待结算过热（越线次数 +1），但不立即结算。
        if (current < OverheatThreshold && next >= OverheatThreshold)
        {
            await AuroraPowerCmd.Apply<AuroraOverheatPendingPower>(ctx, creature, 1, creature, cardSource, silent: true);
        }

        await SetHeatAsync(ctx, creature, next, cardSource, HeatChangeReason.Add);

        // 热量更新后，若已锁定待结算过热，LockedDamage 取 Max(当前, 实时预计)。
        // 之后散热只降热量/换区、绝不降低 LockedDamage；结算读 LockedDamage，不再事后抹债。
        if (AuroraOverheatPendingPower.IsPending(creature))
        {
            await AuroraLockedOverheatDamagePower.SetAtLeastAsync(ctx, creature, RawProjectedOverheatDamage(creature));
        }
    }

    /// <summary>散尽：降到 0，不触发过热。返回实际散去的热量（供收益计算）。</summary>
    public static async Task<int> VentAsync(PlayerChoiceContext ctx, Creature creature, CardModel cardSource)
    {
        var vented = GetHeat(creature);
        if (vented > 0)
        {
            await SetHeatAsync(ctx, creature, 0, cardSource, HeatChangeReason.Vent);
        }

        return vented;
    }

    /// <summary>
    /// 最多散去 <paramref name="maxAmount"/> 点热（原子；<b>不散尽、不触发过热</b>），返回实际散去量。
    /// 语义：<c>actual = min(max(currentHeat,0), max(maxAmount,0))</c>，只降 actual，走统一 <see cref="SetHeatAsync"/> 产生正确换区。
    /// <paramref name="creature"/> 无效、战斗已失效或 <paramref name="maxAmount"/> ≤ 0 时安全返回 0。原「散尽」<see cref="VentAsync"/> 语义保持不变。
    /// </summary>
    public static async Task<int> VentUpToAsync(PlayerChoiceContext ctx, Creature creature, int maxAmount, CardModel cardSource)
    {
        if (creature == null || maxAmount <= 0 || CombatManager.Instance == null || !CombatManager.Instance.IsInProgress)
        {
            return 0;
        }

        var current = GetHeat(creature);
        var actual = Math.Min(current, maxAmount);
        if (actual <= 0)
        {
            return 0;
        }

        await SetHeatAsync(ctx, creature, current - actual, cardSource, HeatChangeReason.Vent);
        return actual;
    }

    /// <summary>
    /// 引爆（延迟过热改造）：主动<b>立即</b>兑现过热，不跟普通积热一起延迟。返回是否成功。
    /// · 已有待结算过热：立即按当前热量/越线次数/完整公式结算。
    /// · 无待结算但热量 ≥4：先锁定一笔（越线次数=1）再立即结算；结算后可在本回合再次越线锁定新的。
    /// </summary>
    public static async Task<bool> IgniteAsync(PlayerChoiceContext ctx, Creature creature, CardModel cardSource)
    {
        if (creature == null || !CombatManager.Instance.IsInProgress)
        {
            return false;
        }

        if (!AuroraOverheatPendingPower.IsPending(creature))
        {
            if (GetHeat(creature) < IgniteMinHeat)
            {
                return false;
            }

            // 无待结算：锁定一笔（越线次数=1），并锁定峰值伤害，随后立即结算。
            await AuroraPowerCmd.Apply<AuroraOverheatPendingPower>(ctx, creature, 1, creature, cardSource, silent: true);
            await AuroraLockedOverheatDamagePower.SetAtLeastAsync(ctx, creature, RawProjectedOverheatDamage(creature));
        }

        await SettleOverheatAsync(ctx, creature, cardSource);
        return true;
    }

    /// <summary>本场第 index 次过热的<b>基础</b>伤害：10/12/14/16…封顶 16。</summary>
    public static int OverheatDamageForIndex(int index) =>
        Math.Min(OverheatDamage + OverheatDamageStep * (Math.Max(index, 1) - 1), OverheatMaxDamage);

    /// <summary>当前<b>实时</b>预计过热伤害（基础 + 红线附加 + 重复越线附加），随热量升降变化，供 LockedDamage 取 Max。</summary>
    private static int RawProjectedOverheatDamage(Creature creature)
    {
        var baseDamage = NextOverheatDamageFor(creature);
        var redline = Math.Max(0, GetHeat(creature) - OverheatThreshold) * RedlineBonusPerHeat;
        var recross = Math.Max(0, AuroraOverheatPendingPower.CrossCount(creature) - 1) * RecrossBonusPerCross;
        return baseDamage + redline + recross;
    }

    /// <summary>
    /// 只读：待结算状态下预计的实际结算伤害，供 UI/悬停。pending 时显示<b>已锁定峰值</b>（散热不降），
    /// 无待结算时返回下一次过热的基础伤害。绝不派发事件、修改状态或消费 RNG。
    /// </summary>
    public static int ProjectedOverheatDamageFor(Creature creature)
    {
        if (creature == null)
        {
            return OverheatDamage;
        }

        if (!AuroraOverheatPendingPower.IsPending(creature))
        {
            return NextOverheatDamageFor(creature);
        }

        return Math.Max(AuroraLockedOverheatDamagePower.Get(creature), RawProjectedOverheatDamage(creature));
    }

    /// <summary>
    /// 重连/存档恢复修复：单一事实源，避免遗物复制私有公式。
    /// ① 热量 ≥ 阈值却无 Pending → 补锁 Pending（断线可能丢债）；② 有 Pending → 按当前实时公式把 LockedDamage 补到至少 raw（只升不降）。
    /// 覆盖「Pending 在但 Locked 缺失/为 0」的补建，之后散热不再降低补建值。不 Settle、不改热量、不消耗 RNG。
    /// </summary>
    public static async Task RepairPendingStateAsync(PlayerChoiceContext ctx, Creature creature)
    {
        if (creature == null || CombatManager.Instance == null || !CombatManager.Instance.IsInProgress)
        {
            return;
        }

        if (GetHeat(creature) >= OverheatThreshold && !AuroraOverheatPendingPower.IsPending(creature))
        {
            await AuroraPowerCmd.Apply<AuroraOverheatPendingPower>(ctx, creature, 1, creature, null, silent: true);
        }

        if (AuroraOverheatPendingPower.IsPending(creature))
        {
            await AuroraLockedOverheatDamagePower.SetAtLeastAsync(ctx, creature, RawProjectedOverheatDamage(creature));
        }

        // 重连/存档恢复：热量柱无过渡对齐当前值，别播一段假的爬升。
        AuroraHeatBarBridge.RequestSnap(creature);
    }

    /// <summary>
    /// 统一过热结算中心（延迟过热改造）。一笔待结算状态<b>只派发一次</b>真正过热，不按每 10 热重复结算。
    /// 顺序：①先移除待结算 Power（幂等守卫）→ ②过热次数+1 取序号 → ③受伤前监听器 <see cref="IAuroraOverheatListener"/>
    /// （模块齐射/余热装甲加盾等）→ ④过热代价：<see cref="IAuroraOverheatCostModifier"/> 可接管（超频→改为失最大生命，
    /// 不走自损作用域）；未接管则走 <see cref="AuroraSelfHarm"/> 受最终可格挡伤害（套自损作用域→灰烬复燃可拦截致死、极限断裂累计掉血）
    /// → ⑤若致死则停（灰烬拦截会把血设回 1 并续走）→ ⑥存活则清零热量(OverheatClear) + 二次移除待结算 + 安排 1 张宕机
    /// → ⑦完整结算后事件 <see cref="IAuroraOverheatResolvedListener"/>（葬炉在此获永久力量）。
    /// 幂等：无待结算直接返回；开头即移除待结算 Power，回合末/引爆重复调用不会二次结算。
    /// 最终伤害 = <b>已锁定峰值 LockedDamage</b>（= Pending 期间「基础(10/12/14/16封顶)+max(0,heat−10)×2+max(0,越线次数−1)×4」的运行时最大值，散热不降）。
    /// </summary>
    public static async Task SettleOverheatAsync(PlayerChoiceContext ctx, Creature creature, CardModel cardSource)
    {
        if (creature == null || !AuroraOverheatPendingPower.IsPending(creature))
        {
            return;
        }

        // 结算伤害读【已锁定峰值】LockedDamage（散热不抹债）。兜底=当前实时公式（异常未记录时）。
        // 两者都在移除 Pending/Locked 前读取（RawProjected 依赖 Pending 的越线次数）。
        var locked = AuroraLockedOverheatDamagePower.Get(creature);
        var fallback = RawProjectedOverheatDamage(creature);

        // 先移除待结算 Power + 已锁定伤害 Power（幂等守卫：防远端/重连/回合末与引爆重复结算）。
        await PowerCmd.Remove<AuroraOverheatPendingPower>(creature);
        await PowerCmd.Remove<AuroraLockedOverheatDamagePower>(creature);

        // 1. 本场过热次数 +1，取当前序号（只 +1，无论越线几次）。
        await AuroraPowerCmd.Apply<AuroraOverheatCountPower>(ctx, creature, 1, creature, cardSource, silent: true);
        var index = AuroraOverheatCountPower.Get(creature);
        var finalDamage = Math.Max(locked, fallback);

        // 2. 过热监听：受伤前只派发一次（模块齐射、余热装甲加格挡等）。快照迭代防增删。
        foreach (var power in creature.Powers.ToList())
        {
            if (power is IAuroraOverheatListener listener)
            {
                await listener.OnOverheatAsync(ctx, creature, index);
            }
        }

        // 3. 过热代价（A 过热稀有地基）：超频等可接管，改为失最大生命；否则受可格挡过热伤害。
        //    常规伤害走 AuroraSelfHarm（套自损作用域→灰烬复燃可拦截致死；记录实际掉血→极限断裂累计）。
        var costHandled = false;
        foreach (var power in creature.Powers.ToList())
        {
            if (power is IAuroraOverheatCostModifier modifier)
            {
                costHandled = await modifier.TryApplyOverheatCostAsync(ctx, creature, finalDamage);
                if (costHandled)
                {
                    break;
                }
            }
        }

        if (!costHandled)
        {
            await AuroraSelfHarm.ApplyAsync(ctx, creature, finalDamage, ValueProp.Unpowered, null);
        }

        // 过热可能把玩家打死；死后不再在尸体上清热/挂待机宕机。
        // 灰烬复燃若拦截致死会把血设回 1、creature 仍存活，继续走清热/宕机/结算后事件。
        if (!creature.IsAlive)
        {
            return;
        }

        // 4-5. 清零热量 + 安排 1 张宕机。过热清零原因，绝不触发换区/散热类效果。
        await SetHeatAsync(ctx, creature, 0, cardSource, HeatChangeReason.OverheatClear);
        // 二次移除守卫：万一监听器/伤害结算期间又积热越线锁了新债，这里清掉，避免结算后残留脏 Pending / 已锁定伤害。
        await PowerCmd.Remove<AuroraOverheatPendingPower>(creature);
        await PowerCmd.Remove<AuroraLockedOverheatDamagePower>(creature);
        await AuroraPowerCmd.Apply<AuroraSystemCrashPendingPower>(ctx, creature, 1, creature, cardSource);

        // 6. 过热完整结算后事件（葬炉在此获得力量）。仅存活且完整结算才派发；快照迭代防增删。
        foreach (var power in creature.Powers.ToList())
        {
            if (power is IAuroraOverheatResolvedListener resolved)
            {
                await resolved.OnOverheatResolvedAsync(ctx, creature, index);
            }
        }
    }
}
