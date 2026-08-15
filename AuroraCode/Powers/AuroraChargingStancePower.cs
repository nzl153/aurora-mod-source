using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuroraMod.AuroraCode.Helpers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// #34 蓄能架式 Power—— 可见能力 Power。回合结束时若本回合未打出攻击牌：
/// 获得 <see cref="Amount"/>（=每回合基础剑势，多张合并累加）点剑势并积 <c>HeatGain</c> 点热量；
/// 若积热结算后处于温区，额外获得 <c>WarmBonus</c> 点剑势。
/// 设计偏离：旧稿积 2 热，延迟过热下龟缩流被动积热是真实时钟，降为 +1（HeatGain 固定值，多张不叠）。
/// 时序挂普通 <see cref="AfterSideTurnEnd"/> 阶段，早于 HeatDissipationCore 的 AfterSideTurnEndLate 过热结算——
/// 若本次积热把你推过 10，当回合末就会结算过热，这是延迟过热的既定行为，不规避。
/// </summary>
public sealed class AuroraChargingStancePower : AuroraPower
{
    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => true;
    protected override string IconName => "charging_stance";

    // HeatGain/WarmBonus 全 mod 恒为常量 1，唯一升级路径只改 MomentumGain(=Amount)、不碰这两者（见 AuroraChargingStance.OnUpgrade）。
    // 故它们是常量权威、DV 仅作展示；DV 默认值(1/1)=常量，重连后即便 DV 丢失回落默认仍正确。无需搬 Amount。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("HeatGain", 1m),
        new DynamicVar("WarmBonus", 1m),
    ];

    private int HeatGain => (int)DynamicVars["HeatGain"].BaseValue;
    private int WarmBonus => (int)DynamicVars["WarmBonus"].BaseValue;

    /// <summary>叠加每回合基础剑势(=Amount)；温区额外剑势/积热为固定触发参数（多张覆盖为同值，不随层数叠加）。</summary>
    public static async Task ApplyAsync(PlayerChoiceContext ctx, Creature creature, int momentum, int warmBonus, int heatGain, CardModel source)
    {
        await AuroraPowerCmd.Apply<AuroraChargingStancePower>(ctx, creature, momentum, creature, source, silent: true);
        var power = creature.GetPower<AuroraChargingStancePower>();
        power?.SetTriggerParams(warmBonus, heatGain);
    }

    private void SetTriggerParams(int warmBonus, int heatGain)
    {
        AssertMutable();
        DynamicVars["WarmBonus"].BaseValue = warmBonus;
        DynamicVars["HeatGain"].BaseValue = heatGain;
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != Owner.Side || Amount <= 0)
        {
            return;
        }

        // 只在自己确实参与本次回合结算时触发（对齐 AttackModulePower / HeatDissipationCore）。
        if (participants?.Contains(Owner) != true)
        {
            return;
        }

        // 条件：本回合未打出攻击牌。
        if (AuroraAttackTurnTracker.HasPlayedAttackThisTurn(Owner))
        {
            return;
        }

        // 与 HeatDissipationCore 对齐，战斗已结束则不再产势/积热。
        if (!CombatManager.Instance.IsInProgress)
        {
            return;
        }

        Flash();

        // 顺序：先给基础剑势 → 积热 → 按结算后区段补温区剑势。
        await AuroraMomentumService.GainAsync(choiceContext, Owner, (int)Amount, null);

        if (HeatGain > 0)
        {
            await HeatPower.AddHeatAsync(choiceContext, Owner, HeatGain, null);
        }

        if (WarmBonus > 0 && HeatPower.GetZone(Owner) == HeatPower.HeatZone.Warm)
        {
            await AuroraMomentumService.GainAsync(choiceContext, Owner, WarmBonus, null);
        }
    }
}
