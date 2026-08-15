using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using AuroraMod.AuroraCode.Cards.Token;
using AuroraMod.AuroraCode.Powers;
using AuroraMod.AuroraCode.Visuals;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace AuroraMod.AuroraCode.Helpers;

/// <summary>
/// 统一模块控制器（架构 §6.1 / §14）—— 唯一负责模块槽（基础 2 槽，可经辅助肩架至硬顶 3）的部署 / 替换 / 强化 / 触发 / 轮转 / 收回 / 查询。
///
/// 卡牌只调用这里的公开接口，绝不直接改 <see cref="AuroraModulePower"/> 的底层数值；
/// 每枚模块是一个独立 Instanced 实例（各带类型 / 强化值 / 来源），本控制器只做「实例集合」上的编排，
/// 不复制模块自身的触发规则（触发一律回落到 <see cref="AuroraModulePower.TriggerAsync"/>）。
///
/// 满槽替换走同步选牌 UI（架构 §6.1 / §12）：令牌卡必须经 <c>CombatState.CreateCard(..., player)</c>
/// 绑定 Owner，裸 <c>ToMutable()</c> 会让选牌屏以 null player 初始化牌堆而报错。
/// </summary>
internal static class AuroraModuleController
{
    // ---- 查询 ----

    public static IReadOnlyList<AuroraModulePower> Modules(Creature creature) => AuroraModulePower.All(creature);

    public static int Count(Creature creature) => AuroraModulePower.All(creature).Count;

    public static int CountOf(Creature creature, ModuleKind kind) =>
        AuroraModulePower.All(creature).Count(m => m.Kind == kind);

    /// <summary>本人权威当前容量 = clamp(2 + #76 额外容量, 2, 3)。所有容量判断的唯一来源。</summary>
    public static int Capacity(Creature creature) => AuroraModuleCapacityPower.CurrentCapacity(creature);

    /// <summary>
    /// 有效容量 = clamp(max(权威容量, 现有模块数), 硬上限 3)。
    /// 正常等于权威容量；仅当外部异常移除容量 Power 而场上仍有 3 枚时，临时取模块数以避免立即静默删模块，
    /// 且永远硬封顶 3、绝不新增第 4 枚（#76 §触发与兼容边界）。
    /// </summary>
    public static int EffectiveCapacity(Creature creature) =>
        Math.Min(Math.Max(Capacity(creature), Count(creature)), AuroraModuleCapacityPower.HardMaxSlots);

    public static bool IsFull(Creature creature) => Count(creature) >= EffectiveCapacity(creature);

    public static int TotalValue(Creature creature, ModuleKind? kind = null) =>
        AuroraModulePower.All(creature).Where(m => kind == null || m.Kind == kind).Sum(m => m.Value);

    // ---- 部署 / 替换 ----

    private static readonly LocString ReplacePrompt = new("combat_messages", "AURORAMOD_MODULE_REPLACE");
    private static readonly LocString ChooseTypePrompt = new("combat_messages", "AURORAMOD_MODULE_CHOOSE_TYPE");
    private static readonly LocString ChooseModulePrompt = new("combat_messages", "AURORAMOD_MODULE_CHOOSE");
    private static readonly LocString SelectModulePrompt = new("combat_messages", "AURORAMOD_MODULE_SELECT");

    /// <summary>
    /// 让拥有者同步选择自己的一枚模块（C-U04 模块轮转用）。0 枚返回 null；1 枚直接确定返回（不弹无意义单选窗，仍确定性、不耗 RNG）；
    /// ≥2 枚走同步选牌 UI（按稳定部署顺序、按索引同步）。
    /// </summary>
    public static async Task<AuroraModulePower> ChooseModuleAsync(PlayerChoiceContext ctx, Creature creature, CardModel source, LocString prompt = null)
    {
        if (creature == null)
        {
            return null;
        }

        var modules = AuroraModulePower.All(creature);
        if (modules.Count == 0)
        {
            return null;
        }

        if (modules.Count == 1)
        {
            return modules[0];
        }

        return await ChooseFromModulesAsync(ctx, creature, modules, prompt ?? ChooseModulePrompt, source);
    }

    /// <summary>
    /// 让拥有者<b>可取消地</b>选择一枚模块（C-R06 拆械斩「你可以移除一枚模块」用）。0 枚返回 null；
    /// 即便只有 1 枚也走同步网格（minSelect=0），让玩家能取消不选（取消/无有效玩家/结果为空 → null）。
    /// </summary>
    public static async Task<AuroraModulePower> ChooseModuleOptionalAsync(PlayerChoiceContext ctx, Creature creature, CardModel source)
    {
        if (creature == null)
        {
            return null;
        }

        var modules = AuroraModulePower.All(creature);
        if (modules.Count == 0)
        {
            return null;
        }

        return await ChooseFromModulesAsync(ctx, creature, modules, SelectModulePrompt, source, minSelect: 0);
    }

    /// <summary>
    /// 部署一枚指定类型的模块。未满槽直接新增；满槽时由该奥萝拉玩家<b>同步选择</b>替换其中一枚（架构 §6.1 / §12）。
    /// <paramref name="value"/> 为空时用该类型基础值。
    ///
    /// 满槽选择走 <see cref="CardSelectCmd.FromSimpleGrid"/> + 游戏原生同步器（按索引同步）：本地弹窗、远端等结果、
    /// 重连一致；候选按模块<b>实例</b>的稳定部署顺序（<see cref="AuroraModulePower.All"/>）解析，不按类型/显示名。
    /// 被替换模块<b>离场前先激发一次</b>（按其当前强化值），再 <see cref="PowerCmd.Remove"/>；强化量不继承给新模块。
    /// 新模块是独立实例。
    /// 选择失败（无有效玩家/结果为空/索引无效/被选实例已不在场）则<b>放弃本次部署</b>，绝不本地随机或默认兜底。
    /// </summary>
    public static async Task<AuroraModulePower> DeployAsync(
        PlayerChoiceContext ctx,
        Creature creature,
        ModuleKind kind,
        CardModel source,
        int? value = null)
    {
        if (creature == null)
        {
            return null;
        }

        // 容量统一读本人权威有效容量（受 #76 影响），不再让调用者传 maxSlots 绕过硬上限。
        var modules = AuroraModulePower.All(creature);
        var wasReplacement = false;
        if (modules.Count >= EffectiveCapacity(creature))
        {
            var victim = await ChooseReplacementAsync(ctx, creature, modules, source);
            if (victim == null)
            {
                // 同步选择未产出有效目标：不替换、不新增（不掩盖为最弱兜底）。
                return null;
            }

            // 退役激发（工坊反馈 #5，2026-07-29）：被替换的模块离场前按其<b>当前强化值</b>触发一次，
            // 语义对齐原版充能球的「被挤出时激发」——满槽替换不再是纯粹的损失，而是「打完最后一发再退役」。
            // 走既有 TriggerAsync，与被动触发、主动触发完全同一条实现（自带 Value<=0 守卫、确定性选敌、不耗额外 RNG）。
            // 时机必须在 Remove 之前：Remove 之后 Owner 已置空，攻击模块取不到 CombatState 会静默失效。
            // 只在「满槽替换」这一条路径上激发；轮转（RotateAsync，模块换型不算离场）与收回（Recall*，玩家主动拆械）
            // 都<b>不</b>激发，语义各自独立，别顺手加上去。
            await victim.TriggerAsync(ctx);

            await PowerCmd.Remove(victim);
            AuroraModuleVisualBridge.RequestRebuild(creature);
            wasReplacement = true;
        }

        var added = await AddAsync(ctx, creature, kind, value ?? BaseValueFor(kind), source);

        // 仅真实新增成功后才播部署动画并派发「成功部署」事件（AddAsync 落地失败不播、不派发）。
        if (added != null)
        {
            AuroraModuleVisualBridge.RequestDeployAnim(creature);
            await DispatchDeployedAsync(ctx, creature, added);

            // 满槽替换在部署之外，额外算一次「轮转/替换」重构事件（C-R04 自适应底盘）；向空槽的普通部署不派发。
            if (wasReplacement)
            {
                await DispatchRotatedAsync(ctx, creature, added);
            }
            else if (IsFull(creature))
            {
                // 向空槽的普通部署恰好填满最后一格 → 派发「填满」事件（R-03 满载耦合器）；满槽替换不派发。
                await DispatchFilledAsync(ctx, creature);
            }
        }

        return added;
    }

    /// <summary>成功部署派发：快照迭代监听器，在同步 action 链内 await；不消耗 RNG。仅 DeployAsync 成功路径调用（轮转不经此）。</summary>
    private static async Task DispatchDeployedAsync(PlayerChoiceContext ctx, Creature creature, AuroraModulePower newModule)
    {
        foreach (var power in creature.Powers.ToList())
        {
            if (power is Powers.IAuroraDeployListener listener)
            {
                await listener.OnModuleDeployedAsync(ctx, creature, newModule);
            }
        }
    }

    /// <summary>轮转/满槽替换派发：快照迭代监听器，在同步 action 链内 await；不消耗 RNG。RotateAsync 成功 + DeployAsync 满槽替换成功调用。</summary>
    private static async Task DispatchRotatedAsync(PlayerChoiceContext ctx, Creature creature, AuroraModulePower resultModule)
    {
        foreach (var power in creature.Powers.ToList())
        {
            if (power is Powers.IAuroraModuleRotateListener listener)
            {
                await listener.OnModuleRotatedAsync(ctx, creature, resultModule);
            }
        }
    }

    /// <summary>「槽位被填满」派发：向拥有者的 Powers 与 Relics（遗物需要此事件）快照迭代；同步 await；不消耗 RNG。</summary>
    private static async Task DispatchFilledAsync(PlayerChoiceContext ctx, Creature creature)
    {
        foreach (var power in creature.Powers.ToList())
        {
            if (power is Powers.IAuroraModuleFilledListener listener)
            {
                await listener.OnModuleSlotsFilledAsync(ctx, creature);
            }
        }

        var relics = creature.Player?.Relics;
        if (relics != null)
        {
            foreach (var relic in relics.ToList())
            {
                if (relic is Powers.IAuroraModuleFilledListener listener)
                {
                    await listener.OnModuleSlotsFilledAsync(ctx, creature);
                }
            }
        }
    }

    /// <summary>
    /// #76 专用：让本人同步在「攻击 / 护盾」中选 1 种模块再部署。候选按稳定索引（[攻击, 护盾]）两端一致同步。
    /// 取消选择则容量保留、跳过部署（不本地随机、不默认某型）。满槽时由 <see cref="DeployAsync"/> 内部走既有替换 UI。
    /// </summary>
    public static async Task DeployChosenTypeAsync(PlayerChoiceContext ctx, Creature creature, CardModel source)
    {
        var kind = await ChooseModuleTypeAsync(ctx, creature, source);
        if (kind == null)
        {
            return;   // 取消选择：容量保留、跳过部署。
        }

        await DeployAsync(ctx, creature, kind.Value, source);
    }

    /// <summary>
    /// 让本人在「攻击 / 护盾」中同步选 1 种类型，返回所选 <see cref="ModuleKind"/>；取消/无有效玩家则返回 null。
    /// 候选按稳定索引（[攻击, 护盾]）两端一致同步、不耗 RNG。供 #76 部署选型与 C-R05 阵列统制轮转选型复用。
    /// </summary>
    public static async Task<ModuleKind?> ChooseModuleTypeAsync(PlayerChoiceContext ctx, Creature creature, CardModel source)
    {
        var player = creature?.Player;
        var combat = creature?.CombatState;
        if (player == null || combat == null)
        {
            return null;
        }

        // 稳定顺序：索引 0 = 攻击，索引 1 = 护盾。两端各自据此构建 → 按索引同步解析到同一型。
        var attackToken = combat.CreateCard<AuroraAttackModuleToken>(player);
        attackToken.DynamicVars["Value"].BaseValue = AttackModulePower.BaseDamage;
        var shieldToken = combat.CreateCard<AuroraShieldModuleToken>(player);
        shieldToken.DynamicVars["Value"].BaseValue = ShieldModulePower.BaseBlock;
        var options = new List<CardModel> { attackToken, shieldToken };

        var prefs = new CardSelectorPrefs(ChooseTypePrompt, 1, 1);
        var chosen = (await CardSelectCmd.FromSimpleGrid(ctx, options, player, prefs)).ToList();
        if (chosen.Count == 0)
        {
            return null;
        }

        var index = options.IndexOf(chosen[0]);
        if (index < 0)
        {
            GD.PushError("[Aurora][Module] 模块类型选择索引无效，跳过。");
            return null;
        }

        return index == 0 ? ModuleKind.Attack : ModuleKind.Shield;
    }

    /// <summary>
    /// 满槽时让拥有者同步选择替换哪一枚模块。返回被选中的模块实例；无法选择则返回 null（调用方须放弃部署）。
    /// 候选令牌卡按 <paramref name="modules"/> 的稳定顺序构建，两端顺序一致 → <see cref="CardSelectCmd.FromSimpleGrid"/>
    /// 的按索引同步在单人 / 远端 / 重连下解析到同一枚。
    /// </summary>
    private static Task<AuroraModulePower> ChooseReplacementAsync(
        PlayerChoiceContext ctx, Creature creature, IReadOnlyList<AuroraModulePower> modules, CardModel source) =>
        ChooseFromModulesAsync(ctx, creature, modules, ReplacePrompt, source);

    /// <summary>
    /// 通用「从本人模块里同步选一枚」：按稳定部署顺序建只读令牌网格、按索引同步解析回实例（单人/远端/重连一致）。
    /// 供满槽替换与模块轮转复用。无有效玩家/空列表/结果为空/索引无效/被选实例已离场则返回 null（调用方须放弃该操作、不本地兜底）。
    /// </summary>
    private static async Task<AuroraModulePower> ChooseFromModulesAsync(
        PlayerChoiceContext ctx, Creature creature, IReadOnlyList<AuroraModulePower> modules, LocString prompt, CardModel source, int minSelect = 1)
    {
        var player = creature.Player;
        if (player == null || modules.Count == 0)
        {
            GD.PushError($"[Aurora][Module] 模块选择缺少有效玩家或模块（player={player?.NetId}, count={modules.Count}）。");
            return null;
        }

        // 按稳定部署顺序为每枚模块建一张只读令牌卡（Value 反映其当前生效值），供选择界面区分。
        var tokens = new List<CardModel>(modules.Count);
        foreach (var module in modules)
        {
            var token = CreateTokenFor(module, player);
            if (token == null)
            {
                return null;
            }

            tokens.Add(token);
        }

        var prefs = new CardSelectorPrefs(prompt, minSelect, 1);
        var chosen = (await CardSelectCmd.FromSimpleGrid(ctx, tokens, player, prefs)).ToList();
        if (chosen.Count == 0)
        {
            return null;   // minSelect=0 时玩家可取消不选 → null（拆械斩退回基础伤害）。
        }

        // 按令牌索引映射回模块实例（不按类型/名称重查）。
        var index = tokens.IndexOf(chosen[0]);
        if (index < 0 || index >= modules.Count)
        {
            GD.PushError($"[Aurora][Module] 模块选择索引无效（index={index}, count={modules.Count}）。");
            return null;
        }

        var picked = modules[index];
        // 选择期间战斗可能推进：确认被选实例仍挂在本人身上。
        if (picked == null || picked.Owner != creature)
        {
            GD.PushError("[Aurora][Module] 被选模块已不在场，放弃操作。");
            return null;
        }

        return picked;
    }

    /// <summary>
    /// 为一枚模块建一张对应类型的只读令牌卡（Value=当前生效值）。仅用于满槽选择界面，不入牌堆。
    /// 必须用 CombatState.CreateCard 绑定 Owner；裸 ToMutable 会导致 NSimpleCardSelectScreen 以 null player 初始化。
    /// </summary>
    private static CardModel CreateTokenFor(AuroraModulePower module, Player player)
    {
        var combat = player.Creature?.CombatState ?? module.Owner?.CombatState;
        if (combat == null)
        {
            GD.PushError("[Aurora][Module] CreateTokenFor 缺少 CombatState。");
            return null;
        }

        CardModel token = module.Kind == ModuleKind.Attack
            ? combat.CreateCard<AuroraAttackModuleToken>(player)
            : combat.CreateCard<AuroraShieldModuleToken>(player);
        token.DynamicVars["Value"].BaseValue = module.Value;
        return token;
    }

    /// <summary>
    /// 底层新增（不做满槽检查；轮转 / 内部复用）。返回本次新增的确切模块实例（按施加前后集合差取，
    /// 不受引擎是否克隆 Power 影响）；异常未落地时返回 null。
    /// </summary>
    private static async Task<AuroraModulePower> AddAsync(PlayerChoiceContext ctx, Creature creature, ModuleKind kind, int value, CardModel source)
    {
        var before = new HashSet<AuroraModulePower>(AuroraModulePower.All(creature));
        var power = CreateFor(kind);
        power.PrimeValue(value);
        await AuroraPowerCmd.Apply(ctx, power, creature, value, creature, source);
        return AuroraModulePower.All(creature).FirstOrDefault(m => !before.Contains(m));
    }

    // ---- 强化 ----

    /// <summary>强化一枚模块（架构 §6.3：每点强化只强化一枚）。指定类型时取该类型最弱者，否则取全场最弱者。</summary>
    public static async Task EnhanceOneAsync(PlayerChoiceContext ctx, Creature creature, int amount, ModuleKind? kind, CardModel source)
    {
        if (amount == 0)
        {
            return;
        }

        var pool = AuroraModulePower.All(creature).Where(m => kind == null || m.Kind == kind).ToList();
        if (pool.Count == 0)
        {
            return;
        }

        var target = pool.OrderBy(m => m.Value).First();
        await EnhanceInstanceAsync(ctx, target, amount, creature, source);
    }

    /// <summary>
    /// 强化「已获得强化量最少」的一枚模块（#18 战地维护 / 后续 #43 现场校准复用）。
    /// 与 <see cref="EnhanceOneAsync"/> 的差异：那里按当前 <see cref="AuroraModulePower.Value"/> 比大小
    /// （攻击基础 4 / 护盾基础 5，会把「同强化量」误判成攻击更弱）；这里按
    /// <c>Value - BaseValue</c>（真实强化量）比较，并列时按 <see cref="AuroraModulePower.All"/> 的
    /// 稳定部署顺序取最早一枚（LINQ OrderBy 稳定排序）。无模块 / amount≤0 返回 null。
    /// 不弹选择窗、不耗 RNG、只强化一枚（两槽 / 三槽都是一枚）。
    /// </summary>
    public static async Task<AuroraModulePower> EnhanceLeastEnhancedAsync(PlayerChoiceContext ctx, Creature creature, int amount, CardModel source)
    {
        if (creature == null || amount <= 0)
        {
            return null;
        }

        var pool = AuroraModulePower.All(creature);
        if (pool.Count == 0)
        {
            return null;
        }

        var target = pool.OrderBy(m => m.Value - m.BaseValue).First();
        await EnhanceInstanceAsync(ctx, target, amount, creature, source);
        return target;
    }

    /// <summary>强化全部模块（仅牌文明确写「所有模块」时；架构 §6.3）。</summary>
    public static async Task EnhanceAllAsync(PlayerChoiceContext ctx, Creature creature, int amount, CardModel source)
    {
        if (amount == 0)
        {
            return;
        }

        foreach (var module in AuroraModulePower.All(creature))
        {
            await EnhanceInstanceAsync(ctx, module, amount, creature, source);
        }
    }

    /// <summary>强化指定的一枚模块实例（C-U01 哨戒阵列强化「本次新部署的那一枚」用）。实例为空/已离场/amount≤0 安全跳过。</summary>
    public static async Task EnhanceSpecificAsync(PlayerChoiceContext ctx, AuroraModulePower module, int amount, CardModel source)
    {
        if (module == null || module.Owner == null || amount <= 0)
        {
            return;
        }

        await EnhanceInstanceAsync(ctx, module, amount, module.Owner, source);
    }

    private static async Task EnhanceInstanceAsync(PlayerChoiceContext ctx, AuroraModulePower module, int amount, Creature applier, CardModel source)
    {
        await PowerCmd.ModifyAmount(ctx, module, amount, applier, source);
        module.Refresh();
        // 强化视觉挂在模块拥有者身上（不是 applier）；未来强化队友模块时不能找错节点。
        AuroraModuleVisualBridge.RequestEnhance(module.Owner, module);
    }

    // ---- 触发 ----

    /// <summary>主动触发模块（不吃过载、不推进连锁）。指定类型只触发该类型；否则全部。</summary>
    public static async Task TriggerAsync(PlayerChoiceContext ctx, Creature creature, ModuleKind? kind = null)
    {
        foreach (var module in AuroraModulePower.All(creature).Where(m => kind == null || m.Kind == kind))
        {
            await module.TriggerAsync(ctx);
        }
    }

    /// <summary>
    /// 控制器级单实例触发入口（供 #16 升级版「立即触发本次新部署的那一枚」）。
    /// 读取该实例部署完成后的实际 <see cref="AuroraModulePower.Value"/>，走既有模块触发规则；不算强化、不改基础值。
    /// 实例为空或已不在场则安全跳过（升级版此时不得触发旧模块作补偿）。
    /// </summary>
    public static async Task TriggerInstanceAsync(PlayerChoiceContext ctx, AuroraModulePower module)
    {
        if (module == null || module.Owner == null)
        {
            return;
        }

        await module.TriggerAsync(ctx);
    }

    // ---- 轮转类型（保留强化量） ----

    /// <summary>
    /// 把一枚模块轮转到另一类型，保留其强化量（当前值 - 原类型基础值），架构 §6.1。返回轮转后的新实例（供 C-U04 升级版立即触发）。
    /// 走 <see cref="AddAsync"/> 而非 <see cref="DeployAsync"/>：轮转语义不是「部署」，<b>不触发成功部署监听（哨戒阵列）</b>。
    /// </summary>
    public static async Task<AuroraModulePower> RotateAsync(PlayerChoiceContext ctx, Creature creature, AuroraModulePower module, CardModel source)
    {
        if (module == null)
        {
            return null;
        }

        var upgrade = module.Value - module.BaseValue;
        var newKind = module.Kind == ModuleKind.Attack ? ModuleKind.Shield : ModuleKind.Attack;
        var newValue = Math.Max(BaseValueFor(newKind) + upgrade, 0);

        await PowerCmd.Remove(module);
        var added = await AddAsync(ctx, creature, newKind, newValue, source);
        AuroraModuleVisualBridge.RequestRebuild(creature);

        // 轮转成功 → 派发「轮转/替换」重构事件（C-R04 自适应底盘）；轮转不派发部署监听（哨戒）。
        if (added != null)
        {
            await DispatchRotatedAsync(ctx, creature, added);
        }

        return added;
    }

    // ---- 收回 ----

    /// <summary>收回一枚指定类型的模块（最弱者优先）；无该类型则不动。</summary>
    public static async Task RecallOneAsync(Creature creature, ModuleKind kind)
    {
        var target = AuroraModulePower.All(creature).Where(m => m.Kind == kind).OrderBy(m => m.Value).FirstOrDefault();
        if (target != null)
        {
            await PowerCmd.Remove(target);
            AuroraModuleVisualBridge.RequestRebuild(creature);
        }
    }

    public static async Task RecallAsync(AuroraModulePower module)
    {
        if (module != null)
        {
            var owner = module.Owner;
            await PowerCmd.Remove(module);
            AuroraModuleVisualBridge.RequestRebuild(owner);
        }
    }

    // ---- 内部 ----

    private static int BaseValueFor(ModuleKind kind) =>
        kind == ModuleKind.Attack ? AttackModulePower.BaseDamage : ShieldModulePower.BaseBlock;

    private static AuroraModulePower CreateFor(ModuleKind kind) => kind == ModuleKind.Attack
        ? (AuroraModulePower)ModelDb.Power<AttackModulePower>().ToMutable()
        : (AuroraModulePower)ModelDb.Power<ShieldModulePower>().ToMutable();
}
