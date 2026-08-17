using System;
using System.Threading.Tasks;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Helpers;

/// <summary>
/// 奥萝拉「自损」统一服务（A 过热稀有地基）。所有由奥萝拉<b>自己的牌或过热</b>造成的对自身伤害都走这里：
/// ① 套自损作用域 <see cref="SelfDamageScope"/> —— 供灰烬复燃的 <c>ShouldDie</c> 识别「这是自损/过热致死，非敌人攻击」，
///    只拦自损、绝不拦敌人攻击（引擎 ShouldDie 不带伤害来源，故用作用域标记区分）；
/// ② 记录实际掉血到 <see cref="AuroraSelfHarmTrackerPower"/> —— 供极限断裂「本场实际自损累计」读取。
/// 战斗单线程顺序结算，用 depth 计数、嵌套安全（照 <see cref="Patches.AuroraLockDamagePatch.ModuleDamageScope"/>）。
/// </summary>
internal static class AuroraSelfHarm
{
    private static int _selfDepth;

    /// <summary>当前是否处于奥萝拉自损/过热伤害作用域（灰烬 ShouldDie 据此只拦自损，不拦敌人攻击）。</summary>
    public static bool SelfDamageActive => _selfDepth > 0;

    /// <summary>包住一次奥萝拉自损/过热伤害调用，把这段标记为「自损来源」。</summary>
    public readonly struct SelfDamageScope : IDisposable
    {
        public static SelfDamageScope Enter()
        {
            _selfDepth++;
            return default;
        }

        public void Dispose()
        {
            if (_selfDepth > 0)
            {
                _selfDepth--;
            }
        }
    }

    /// <summary>
    /// 施加一次「自损」伤害：套自损作用域施伤（dealer=自己），随后按实际掉血累计到自损追踪器。
    /// <paramref name="props"/> 由调用方决定（卡牌自损=Unblockable|Unpowered；过热=Unpowered 可格挡）。amount≤0 直接返回。
    /// 若灰烬复燃在此拦截致死并把血设回 1，则记录的实际掉血只到 1（before-1），符合极限断裂「只计生命条真实下降」。
    /// </summary>
    public static async Task ApplyAsync(PlayerChoiceContext ctx, Creature creature, int amount, ValueProp props, CardModel cardSource)
    {
        if (creature == null || amount <= 0)
        {
            return;
        }

        var before = creature.CurrentHp;
        using (SelfDamageScope.Enter())
        {
#if STS2_BETA
            // beta v0.111.0：删掉了 (ctx,target,amount,props,dealer,cardSource) 这个 6 参数重载，
            // 改为末尾带 CardPlay? 的 7 参数版。自伤没有对应的一次打出，传 null（与正式版行为一致）。
            await CreatureCmd.Damage(ctx, creature, amount, props, creature, cardSource, null);
#else
            await CreatureCmd.Damage(ctx, creature, amount, props, creature, cardSource);
#endif
        }

        var lost = before - creature.CurrentHp;
        if (lost > 0)
        {
            await AuroraSelfHarmTrackerPower.RecordAsync(ctx, creature, lost);
        }
    }
}
