using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// A-R01 超频 Power（A 过热稀有）—— 可见能力 + <see cref="IAuroraOverheatCostModifier"/>。
/// 每层使攻击 +0.75 最终伤害，<b>仅在已锁定待结算过热(Pending)时生效</b>（由 <see cref="HeatPower.ModifyDamageMultiplicative"/> 门控合成，7~9 热无 Pending 只有基础 ×1.25）。
/// 结算过热时接管代价：能完整支付则不受过热伤害、改为每层失 <see cref="MaxHpLossPerStack"/>=2 最大生命（不走自损服务→不计极限断裂、不触发灰烬）；
/// 付不起（最大生命扣后 &lt;1）则返回 false 回退原 LockedDamage 过热伤害（可被格挡/灰烬拦截）。仍照常清热、宕机。胜利宽恕时不结算=不付最大生命。
/// </summary>
public sealed class AuroraOverclockPower : AuroraPower, IAuroraOverheatCostModifier
{
    public const decimal OverloadBonusPerStack = 0.75m;
    public const int MaxHpLossPerStack = 2;   // 每层过热代价失最大生命 1→2

    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => true;
    protected override string IconName => "overclock";

    public int Stacks => (int)Amount;

    /// <summary>过载区超频加成倍率 = 0.75 × 层数（供 HeatPower 合成到过载总倍率；无超频返回 0）。</summary>
    public static decimal OverloadBonus(Creature creature)
    {
        var power = creature?.GetPower<AuroraOverclockPower>();
        return power == null ? 0m : OverloadBonusPerStack * power.Stacks;
    }

    public async Task<bool> TryApplyOverheatCostAsync(PlayerChoiceContext ctx, Creature owner, int originalDamage)
    {
        if (owner != Owner || Stacks <= 0)
        {
            return false;
        }

        var cost = Stacks * MaxHpLossPerStack;
        // 只有能完整支付且支付后最大生命仍 ≥1 才替换；否则回退原过热伤害。
        if (owner.MaxHp - cost < 1)
        {
            return false;
        }

        Flash();
        // 最大生命代价不走自损服务：不计极限断裂、不触发灰烬（isFromCard=true 供原生识别为卡牌来源）。
        await CreatureCmd.LoseMaxHp(ctx, owner, cost, isFromCard: true);
        return true;
    }
}
