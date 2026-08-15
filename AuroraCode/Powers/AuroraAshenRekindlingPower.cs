using System.Collections.Generic;
using System.Threading.Tasks;
using AuroraMod.AuroraCode.Helpers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// A-R04 灰烬复燃 Power（A 过热稀有）—— 可见能力 + 濒死拦截（<c>ShouldDie</c>/<c>AfterPreventingDeath</c>）。
/// 当<b>自己的牌或过热</b>将使生命降至 0 时（<see cref="AuroraSelfHarm.SelfDamageActive"/> 作用域内，绝不拦敌人攻击），
/// 消耗 1 层（<see cref="Amount"/>）保留 1 点生命并获得 <c>EnergyGain</c> 能量：在行动阶段立即获得；回合末等不可行动窗口则登记到
/// 下回合开始发放（基础能量刷新后）。多层可拦截多个独立致死段；<see cref="Amount"/>=剩余层数。
/// </summary>
public sealed class AuroraAshenRekindlingPower : AuroraPower
{
    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => Amount > 0;
    protected override string IconName => "ashen_rekindling";

    // EnergyGain 恒为常量 2（无升级改写路径，DV 默认=常量→重连安全，仅作展示）。
    // 待发能量权威改存序列化的 AuroraAshenDeferredEnergyPower.Amount（DV 不进同步，回合末延迟窗口断线会丢）。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("EnergyGain", 2m),
    ];

    /// <summary>只拦「自损/过热」致死：非本人、无层数、或非自损作用域（=敌人攻击等）一律放行死亡。</summary>
    public override bool ShouldDie(Creature creature)
    {
        if (creature != Owner || (int)Amount <= 0 || !AuroraSelfHarm.SelfDamageActive)
        {
            return true;
        }

        return false;   // 自损作用域内且有层数 → 阻止死亡
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (creature != Owner || (int)Amount <= 0)
        {
            return;
        }

        // 原子消耗 1 层 + 保留 1 血。
        AssertMutable();
        SetAmount((int)Amount - 1);
        InvokeDisplayAmountChanged();
        await CreatureCmd.SetCurrentHp(creature, 1);
        Flash();

        // 能量：行动阶段立即获得；回合末等不可行动窗口登记延迟，下回合基础刷新后发放。
        var player = creature.Player;
        if (player == null)
        {
            return;
        }

        var energy = (int)DynamicVars["EnergyGain"].BaseValue;
        var ending = CombatManager.Instance != null
                     && (CombatManager.Instance.EndingPlayerTurnPhaseOne || CombatManager.Instance.EndingPlayerTurnPhaseTwo);
        if (ending)
        {
            // 登记到序列化的待发能量载体（权威=Amount，重连一致）；无 ctx 时用 ThrowingPlayerChoiceContext。
            await AuroraAshenDeferredEnergyPower.AddAsync(new ThrowingPlayerChoiceContext(), creature, energy);
        }
        else
        {
            await PlayerCmd.GainEnergy(energy, player);
        }
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
        {
            return;
        }

        var deferred = AuroraAshenDeferredEnergyPower.Pending(Owner);
        if (deferred > 0)
        {
            // 先发能量、成功后再移除待发载体——避免 GainEnergy 抛错时「已清但能量未到」漏发且重连回不来。
            await PlayerCmd.GainEnergy(deferred, player);
            await PowerCmd.Remove<AuroraAshenDeferredEnergyPower>(Owner);
        }
    }
}
