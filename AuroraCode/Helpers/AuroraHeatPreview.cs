using System;
using AuroraMod.AuroraCode.Cards.Common;
using AuroraMod.AuroraCode.Cards.Rare;
using AuroraMod.AuroraCode.Cards.Uncommon;
using MegaCrit.Sts2.Core.Models;

namespace AuroraMod.AuroraCode.Helpers;

/// <summary>
/// 「打出前会烧到哪」预览取值器（纯只读表现层）。
///
/// 只读取已核对为固定即时积热/散热的卡，沿用 <c>CanonicalVars</c> 的命名约定——
/// <list type="bullet">
/// <item><c>PowerVar&lt;HeatPower&gt;</c> → 键名 <c>"HeatPower"</c></item>
/// <item><c>DynamicVar("HeatGain")</c></item>
/// <item><c>DynamicVar("VentMax")</c> / <c>"VentAmount"</c> → 负向</item>
/// </list>
///
/// <b>刻意的局限，别当 bug</b>：
/// ① 条件卡（「根据打出前所在区段…」「若已连锁则…」）无法静态预览，一律返回 0 不猜；
///    宁可不显示，也绝不显示一个会骗人的预览段。
/// ② 能力的延迟触发不属于打出时的热量增量，不参与。
/// ③ <c>TargetHeat</c>（设定到某值）语义不是增量，不参与。
///
/// 绝不消费 RNG、不改 Power/Card 状态、不触发任何结算副作用。
/// </summary>
internal static class AuroraHeatPreview
{
    private const string PowerVarKey = "HeatPower";
    private const string GainKey = "HeatGain";

    private static readonly string[] VentKeys = ["VentMax", "VentAmount"];

    /// <summary>
    /// 这张卡打出后热量的预计变化量。取不到 / 是条件卡 / 任何异常 → 0（不显示预览）。
    /// </summary>
    public static int ResolveDelta(CardModel card)
    {
        if (card is not (AuroraReactorThrust or AuroraSearingCut or AuroraTemperedByHeat
            or AuroraVentShield or AuroraAblativeArmor or AuroraBurningAdvance or AuroraCrossfire
            or AuroraHeatFedFocus or AuroraPrecisionVent or AuroraThermodynamicConversion or AuroraLimitFracture))
        {
            return 0;
        }

        try
        {
            var vars = card.DynamicVars;
            if (vars == null)
            {
                return 0;
            }

            var delta = 0;

            // 积热：PowerVar<HeatPower> 与裸 HeatGain 两种写法都认，取到一个就够（不叠加）。
            if (TryRead(vars, PowerVarKey, out var byPowerVar))
            {
                delta += byPowerVar;
            }
            else if (TryRead(vars, GainKey, out var byGain))
            {
                delta += byGain;
            }

            // 散热：向下走。
            foreach (var key in VentKeys)
            {
                if (TryRead(vars, key, out var vent))
                {
                    delta -= vent;
                    break;
                }
            }

            return delta;
        }
        catch (Exception)
        {
            // canonical 实例 / 牌库预览等场景可能抛异常，一律按「无预览」处理。
            return 0;
        }
    }

    private static bool TryRead(
        MegaCrit.Sts2.Core.Localization.DynamicVars.DynamicVarSet vars, string key, out int value)
    {
        value = 0;
        if (!vars.TryGetValue(key, out var v) || v == null)
        {
            return false;
        }

        value = (int)v.BaseValue;
        return value != 0;
    }
}
