using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.DynamicVars;

/// <summary>
/// 会把「条件加值」算进卡面预览的伤害变量。
///
/// 【解决什么】奥萝拉有一批卡的伤害在 OnPlay 里才算出来（一刀两断=8+每势×5、编队突击=7+每模块×4…），
/// 卡面 <c>{Damage}</c> 永远只显示基础值，玩家得自己心算。本类让卡面像原版遇到易伤那样，
/// <b>直接显示打出去的真实伤害</b>。
///
/// 【为什么安全 —— 这是引擎认可的正道，不是 hack】
/// 只写 <see cref="DynamicVar.PreviewValue"/>，<b>绝不碰 <c>BaseValue</c></b>。
/// 联机同步的卡牌字段只有 {Id, 升级等级, 附魔, SavedProperties, 入队楼层}，
/// <c>PreviewValue</c> 物理上不进校验和，因此不可能导致 desync。
/// 实现完全复刻原版 <see cref="DamageVar.UpdateCardPreview"/> 的管线（附魔加算→附魔乘算→
/// <see cref="Hook.ModifyDamage"/> 全局钩子），只是把基数从 <c>BaseValue</c> 换成 <c>BaseValue + 条件加值</c>，
/// 因此力量/易伤/过载×1.25/锁定+2 等全部照常叠加在加值之上，预览与真实结算一致。
///
/// 【canonical 安全】卡牌图鉴/主菜单里的卡是 canonical 模板，访问 <c>card.Owner</c> 会抛
/// <c>CanonicalModelException</c> 直接崩百科。故计算加值前先判 <c>card.CombatState</c>
/// （该属性在 canonical 下安全返回 null，不抛），再套 try/catch 兜底 —— 任何异常都退回加值 0，
/// 最坏结果只是卡面显示基础值，永不崩。
///
/// 【职责边界】本类<b>只管显示</b>。真实伤害仍由各卡自己的 OnPlay 计算，
/// 两边读的是同一份状态（剑势/模块数/协议层数），故天然一致；<b>绝不能</b>让卡牌改从本类取值，
/// 否则就变成"渲染期参与结算"，正是要避免的事。
/// </summary>
public sealed class AuroraScalingDamageVar : DamageVar
{
    private readonly Func<CardModel, Creature, int> _bonus;

    /// <param name="bonus">
    /// 读当前状态算出「本牌此刻的额外伤害」。只允许纯读取，禁止改任何状态。
    /// 返回负数会被钳到 0（加值只增不减，避免预览低于牌面基础值）。
    /// </param>
    public AuroraScalingDamageVar(decimal damage, ValueProp props, Func<CardModel, int> bonus)
        : base(damage, props)
    {
        _bonus = (card, _) => bonus(card);
    }

    /// <param name="bonus">
    /// 同上，但额外拿到<b>当前预览的目标</b>——加值取决于目标身上状态（如挑战反斩读目标的协议层数）时用这个重载。
    /// 目标可能为 <c>null</c>（尚未指向敌人时的预览），实现方须自行容忍。
    /// </param>
    public AuroraScalingDamageVar(decimal damage, ValueProp props, Func<CardModel, Creature, int> bonus)
        : base(damage, props)
    {
        _bonus = bonus;
    }

    public AuroraScalingDamageVar(string name, decimal damage, ValueProp props, Func<CardModel, int> bonus)
        : base(name, damage, props)
    {
        _bonus = (card, _) => bonus(card);
    }

    public AuroraScalingDamageVar(string name, decimal damage, ValueProp props, Func<CardModel, Creature, int> bonus)
        : base(name, damage, props)
    {
        _bonus = bonus;
    }

    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature target, bool runGlobalHooks)
    {
        // 基数 = 牌面基础值 + 当前条件加值（后者在 canonical/异常时为 0）
        var effective = BaseValue + SafeBonus(card, target);

        var num = effective;
        var enchantment = card.Enchantment;
        if (enchantment != null)
        {
            num += enchantment.EnchantDamageAdditive(num, Props);
            num *= enchantment.EnchantDamageMultiplicative(num, Props);
            if (!card.IsEnchantmentPreview)
            {
                EnchantedValue = num;
            }
        }

        if (runGlobalHooks)
        {
            num = Hook.ModifyDamage(card.Owner.RunState, card.CombatState, target, card.Owner.Creature,
                effective, Props, card, ModifyDamageHookType.All, previewMode, out IEnumerable<AbstractModel> _);
        }

        PreviewValue = num;
    }

    /// <summary>算条件加值；无战斗态（图鉴/主菜单）或任何异常一律返回 0，保证永不崩。</summary>
    private int SafeBonus(CardModel card, Creature target)
    {
        try
        {
            if (card?.CombatState == null)
            {
                return 0;
            }

            return Math.Max(0, _bonus(card, target));
        }
        catch (Exception e)
        {
            GD.PushError($"[Aurora][Preview] 条件加值计算异常，已按 0 处理：{e}");
            return 0;
        }
    }
}
