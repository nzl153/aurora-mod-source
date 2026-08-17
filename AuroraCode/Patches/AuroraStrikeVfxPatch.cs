using System;
using System.Collections.Generic;
using System.Linq;
using AuroraMod.AuroraCode.Helpers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Patches;

/// <summary>
/// 奥萝拉攻击牌的紫刀光命中特效——挂在「实际造成伤害」这一刻，而非「出牌」瞬间。
///
/// 此前特效绑在 <c>CardModel.OnPlayWrapper</c> 的可空 <c>target</c> 参数上，遇到自动目标/多段/
/// 目标解析时机不一致时会「出伤害但没特效」。改为 postfix 核心伤害命令
/// <see cref="CreatureCmd"/>.Damage(...targets, amount, props, dealer, cardSource)：
///   · 模块伤害走 cardSource=null → 自动排除（模块有自己的激光特效）；
///   · 只在 cardSource 是「攻击牌」且施加者是奥萝拉时炸刀光 → 精准锁定攻击命中，
///     天然覆盖多段、群体、自动目标，永不漏也永不误触。
///
/// 纯表现层：不改伤害/结算/netcode，异常静默。
/// </summary>
[HarmonyPatch]
public static class AuroraStrikeVfxPatch
{
#if STS2_BETA
    // beta v0.111.0：CreatureCmd.Damage 的 (…, Creature dealer, CardModel cardSource) 这个重载被删了，
    // 换成末尾带 CardPlay? 的版本。attribute 里的类型数组不参与重载解析，所以编译期发现不了——
    // 只有启动时 Harmony 抛 Undefined target method，且 PatchAll 一炸全停，整个 mod 的补丁一起失效。
    [HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Damage), new[]
    {
        typeof(PlayerChoiceContext),
        typeof(IEnumerable<Creature>),
        typeof(decimal),
        typeof(ValueProp),
        typeof(Creature),
        typeof(CardModel),
        typeof(MegaCrit.Sts2.Core.Entities.Cards.CardPlay),
    })]
    [HarmonyPostfix]
#else
    [HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Damage), new[]
    {
        typeof(PlayerChoiceContext),
        typeof(IEnumerable<Creature>),
        typeof(decimal),
        typeof(ValueProp),
        typeof(Creature),
        typeof(CardModel),
    })]
    [HarmonyPostfix]
#endif
    public static void DamagePostfix(IEnumerable<Creature> targets, Creature dealer, CardModel cardSource)
    {
        try
        {
            // 只认「攻击牌」造成的伤害；模块（cardSource=null）与非攻击一律不炸刀光。
            if (cardSource == null || cardSource.Type != CardType.Attack)
            {
                return;
            }

            // 施加者必须是奥萝拉本人。
            var entry = dealer?.Player?.Character?.Id.Entry;
            if (string.IsNullOrWhiteSpace(entry)
                || !entry.Contains("aurora", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // 命中演出档位：普通刀光 / 大招爆闪 / 群体紫刃齐射 / 单发重拳。由卡牌声明，默认 Normal。
            var kind = cardSource is AuroraMod.AuroraCode.Cards.AuroraCard ac
                ? ac.StrikeVfx
                : AuroraStrikeVfxKind.Normal;

            // 过载区出刀 → 命中侧叠一小簇灼热火花，呼应「过载=灼热」。按施加者当前热区判定，覆盖所有攻击牌。
            bool overload = AuroraMod.AuroraCode.Powers.HeatPower.GetZone(dealer)
                is AuroraMod.AuroraCode.Powers.HeatPower.HeatZone.Overload
                or AuroraMod.AuroraCode.Powers.HeatPower.HeatZone.Critical;

            foreach (var target in targets)
            {
                // 不排除已死目标：致命一击时 target.IsDead 已为真，但这一下仍要出刀光。
                if (target != null && target.Side != dealer.Side)
                {
                    switch (kind)
                    {
                        case AuroraStrikeVfxKind.Ultimate:
                            AuroraStrikeVfx.PlayUltimateSlash(dealer, target);
                            break;
                        case AuroraStrikeVfxKind.Flurry:
                            AuroraStrikeVfx.PlayFlurry(dealer, target);
                            break;
                        case AuroraStrikeVfxKind.Heavy:
                            AuroraStrikeVfx.PlayHeavy(dealer, target);
                            break;
                        default:
                            AuroraStrikeVfx.PlayStrikeImpact(dealer, target);
                            break;
                    }

                    if (overload)
                    {
                        AuroraStrikeVfx.PlayOverloadEmber(dealer, target);
                    }
                }
            }
        }
        catch
        {
            // 纯表现：绝不因它中断战斗。
        }
    }
}
