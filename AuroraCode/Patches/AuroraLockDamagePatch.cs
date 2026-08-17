using System;
using AuroraMod.AuroraCode.Helpers;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Patches;

/// <summary>
/// 锁定伤害中心（架构 §9，唯一集中点）—— 实现「乘法后<b>向下取整</b> → 锁定纯 +2 → 最终伤害上限(Cap)」。
///
/// 游戏的 <c>Hook.ModifyDamageInternal</c> 依次做 加法 → 乘法 → Cap（Cap 恒执行、无 flag），<b>本身不取整</b>
/// （取整发生在后续 (int) 转换）。故本 Postfix 先按 (int) 同规则 <c>Truncate</c> 取整，再 +2，再重算 Cap 取 min，
/// 等价于把 +2 插在「取整之后、Cap 之前」。该方法在<b>预览</b>与真实两路都会跑，本类只做<b>纯数值</b>（两路一致），
/// <b>绝不在此消费锁定</b>；真正的「消费 1 层」放在真实伤害路径的 <see cref="Powers.AuroraLockConsumePower"/>
/// (AfterDamageGiven，只在真实结算触发)。两者用同一判定：来源是奥萝拉本人的 powered attack 或攻击模块，
/// 且本人对该目标有正层锁定。
///
/// +2 是纯加法但插在乘区之后，故不吃力量/易伤/过载；重算 Cap 取 min 保证不绕过无实体等最终上限（L03）。
/// 攻击模块走 Unpowered，用 <see cref="ModuleDamageScope"/> 计数标记其为可消费来源（唯一 Unpowered 例外）。
/// </summary>
public static class AuroraLockDamagePatch
{
    public const int FlatBonus = 2;

    // 攻击模块伤害作用域计数（战斗单线程顺序结算；用计数以防嵌套安全）。
    private static int _moduleDepth;
    public static bool ModuleDamageActive => _moduleDepth > 0;

    /// <summary>包住攻击模块的一次伤害调用，把这段 Unpowered 标记为可消费锁定的来源。</summary>
    public readonly struct ModuleDamageScope : IDisposable
    {
        public static ModuleDamageScope Enter()
        {
            _moduleDepth++;
            return default;
        }

        public void Dispose()
        {
            if (_moduleDepth > 0)
            {
                _moduleDepth--;
            }
        }
    }

    /// <summary>是否是「可消费锁定」的来源：奥萝拉本人 powered attack，或攻击模块 Unpowered（唯一例外）。</summary>
    public static bool IsLockConsumingSource(ValueProp props) =>
        props.IsPoweredAttack() || ModuleDamageActive;

    /// <summary>纯数值判定：dealer 本人对 target 有正层锁定、来源可消费、且本段<b>取整后</b>伤害至少为 1。</summary>
    private static bool EligibleNumeric(Creature target, Creature dealer, ValueProp props, decimal flooredDamage)
    {
        if (target == null || dealer == null || flooredDamage < 1m)
        {
            return false;
        }

        if (!IsLockConsumingSource(props))
        {
            return false;
        }

        return AuroraLockService.GetStacks(target, dealer) > 0;
    }

    [HarmonyPatch(typeof(Hook), "ModifyDamageInternal")]
    public static class ModifyDamageInternalPatch
    {
        // 只在包含乘法阶段的完整合成上追加（All = 加法|乘法）；纯加法子调用不追加，避免错位。
#if STS2_BETA
        // beta v0.111.0：ModifyDamageInternal 的 combatState 由 CombatState 改成接口 ICombatState，
        // 并新增了 cardPlay。Harmony 按「参数名 + 类型」匹配，类型对不上会在启动时抛 HarmonyException，
        // 而 PatchAll 是一炸全停（整个 mod 的补丁会全部失效）——所以这里必须跟着改，不能只加参数。
        public static void Postfix(
            ref decimal __result,
            IRunState runState,
            MegaCrit.Sts2.Core.Combat.ICombatState combatState,
            Creature target,
            Creature dealer,
            ValueProp props,
            CardModel cardSource,
            MegaCrit.Sts2.Core.Entities.Cards.CardPlay cardPlay,
            ModifyDamageHookType modifyDamageHookType)
#else
        public static void Postfix(
            ref decimal __result,
            IRunState runState,
            CombatState combatState,
            Creature target,
            Creature dealer,
            ValueProp props,
            CardModel cardSource,
            ModifyDamageHookType modifyDamageHookType)
#endif
        {
            try
            {
                if (!modifyDamageHookType.HasFlag(ModifyDamageHookType.Multiplicative))
                {
                    return;
                }

                // 游戏 (int) 伤害向零截断；先按同规则取整，再判定与 +2，使 §9/L02「乘法后向下取整→锁定+2」精确成立。
                var floored = decimal.Truncate(__result);
                if (!EligibleNumeric(target, dealer, props, floored))
                {
                    return;
                }

                // 重算 Cap（与方法内 Cap 循环一致）：floored+2 后仍不得超过最终伤害上限（L03 不绕过无实体）。
                var cap = decimal.MaxValue;
                foreach (var model in runState.IterateHookListeners(combatState))
                {
#if STS2_BETA
                    var c = model.ModifyDamageCap(target, props, dealer, cardSource, cardPlay);
#else
                    var c = model.ModifyDamageCap(target, props, dealer, cardSource);
#endif
                    if (c < cap)
                    {
                        cap = c;
                    }
                }

                __result = Math.Min(floored + FlatBonus, cap);
            }
            catch (Exception e)
            {
                // 表现层/边角异常不得中断战斗结算，但机制异常必须记录（架构 §14）。
                GD.PushError($"[Aurora][Lock] ModifyDamageInternal Postfix 异常：{e}");
            }
        }
    }
}
