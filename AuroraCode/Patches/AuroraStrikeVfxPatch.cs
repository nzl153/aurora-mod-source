using System;
using AuroraMod.AuroraCode.Helpers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;

namespace AuroraMod.AuroraCode.Patches;

/// <summary>
/// 奥萝拉攻击牌的刀光在扣血前播放，致死一击时目标节点仍在。
/// <see cref="Hook.BeforeDamageReceived"/> 在 0.111.1 和 0.113.x 的签名一致，无需条件编译。
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.BeforeDamageReceived))]
public static class AuroraStrikeVfxPatch
{
    [HarmonyPrefix]
    public static void Prefix(Creature target, Creature dealer, CardModel cardSource)
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
        catch
        {
            // 纯表现：绝不因它中断战斗。
        }
    }
}
