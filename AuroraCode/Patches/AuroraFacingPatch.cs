using System;
using AuroraMod.AuroraCode.Helpers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace AuroraMod.AuroraCode.Patches;

/// <summary>
/// 统一处理奥萝拉主动、带敌方目标的动作转向。
/// 卡牌在 OnPlayWrapper 入口按 Attack 类型触发；药水只要明确指定敌方目标就触发。
/// 无目标/AOE 不改变当前方向，已确定的方向保持到下一次有目标动作。
/// </summary>
[HarmonyPatch]
public static class AuroraFacingPatch
{
    [HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
    [HarmonyPrefix]
    public static void CardPlayPrefix(CardModel __instance, Creature target)
    {
        // 纯表现（音效 + 转向）：全程 try/catch 静默，任何异常绝不中断出牌/结算（对齐 AuroraStrikeVfx/DeathOffset）。
        try
        {
            var owner = __instance?.Owner;
            if (!IsAuroraOwner(owner))
            {
                return;
            }

            // 音效（纯表现）：攻击牌=打击音；技能牌=技能音（防御等，与护盾模块生效共用同一素材）。
            if (__instance.Type == CardType.Attack)
            {
                AuroraAudio.PlaySfx("attack_hit.wav");
            }
            else if (__instance.Type == CardType.Skill)
            {
                AuroraAudio.PlaySfx("skill.wav");
            }

            // 攻击牌的转向（仅在有敌方目标时）。Spine attack 动画本身有挥剑位移，不再额外挪立绘。
            // 命中特效已移交 AuroraStrikeVfxPatch（挂在实际造成伤害那一刻，永不漏触发）。
            if (__instance.Type == CardType.Attack && ShouldFaceTarget(owner, target))
            {
                AuroraFacing.FaceTarget(owner.Creature, target);
            }
        }
        catch
        {
            // 纯表现：绝不因音效/转向异常中断出牌。
        }
    }

    /// <summary>owner 是否为奥萝拉（按角色 Id 判定，与目标无关，供音效用）。</summary>
    private static bool IsAuroraOwner(Player owner)
    {
        var entry = owner?.Character?.Id.Entry;
        return owner?.Creature != null
            && !string.IsNullOrWhiteSpace(entry)
            && entry.Contains("aurora", StringComparison.OrdinalIgnoreCase);
    }

    [HarmonyPatch(typeof(PotionModel), nameof(PotionModel.OnUseWrapper))]
    [HarmonyPrefix]
    public static void PotionUsePrefix(PotionModel __instance, Creature target)
    {
        // 纯表现（转向）：全程 try/catch 静默，异常绝不中断用药。
        try
        {
            var owner = __instance?.Owner;
            if (!ShouldFaceTarget(owner, target))
            {
                return;
            }

            AuroraFacing.FaceTarget(owner.Creature, target);
        }
        catch
        {
            // 纯表现：绝不因转向异常中断用药。
        }
    }

    private static bool ShouldFaceTarget(Player owner, Creature target)
    {
        var actor = owner?.Creature;
        var entry = owner?.Character?.Id.Entry;
        return actor != null
            && target != null
            && !target.IsDead
            && target.Side != actor.Side
            && !string.IsNullOrWhiteSpace(entry)
            && entry.Contains("aurora", StringComparison.OrdinalIgnoreCase);
    }
}
