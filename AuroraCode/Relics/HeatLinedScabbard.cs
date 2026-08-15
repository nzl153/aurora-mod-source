using System.Threading.Tasks;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace AuroraMod.AuroraCode.Relics;

/// <summary>
/// R-02 热衬剑鞘 / Heat-Lined Scabbard（罕见，B 剑势）—— 战斗开始获得 4 剑势并积 4 热。
/// 顺序固定：先 4 剑势 → 再 4 热（从 0 热开战直接进温区，主动放弃冷区开局）。热量走标准积热/换区流程，与其它开战积热正常累加。
/// 实现时序：<b>本场首个回合开始时</b>一次性发放。用隐藏 <see cref="AuroraScabbardArmedPower"/> 标记（属战斗态、会被序列化并在重连恢复），
/// 而非遗物私有 bool——故断线重连后不会二次发放。战斗态天然每场重置，无需 BeforeCombatStart 复位。
/// 之所以不在 <see cref="MegaCrit.Sts2.Core.Entities.Relics.RelicModel.BeforeCombatStart"/> 直接积热：那一刻 <see cref="CombatManager"/>.IsInProgress
/// 可能尚未置真、<see cref="HeatPower.AddHeatAsync"/> 会因 !IsInProgress 早退吞掉积热；首回合开始时 IsInProgress 恒真且有真实 ctx，最稳。
/// </summary>
public class HeatLinedScabbard : AuroraRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    protected override string ArtName => "heat_lined_scabbard";

    private const int Momentum = 4;
    private const int Heat = 4;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        var creature = Owner?.Creature;
        if (creature == null || player != creature.Player
            || AuroraScabbardArmedPower.IsArmed(creature) || !CombatManager.Instance.IsInProgress)
        {
            return;
        }

        // 隐藏标记置位（Apply(1)），重连恢复后 IsArmed 为真 → 不二次发放。
        await AuroraPowerCmd.Apply<AuroraScabbardArmedPower>(choiceContext, creature, 1, creature, null, silent: true);
        Flash();
        await AuroraMomentumService.GainAsync(choiceContext, creature, Momentum, null);
        await HeatPower.AddHeatAsync(choiceContext, creature, Heat, null);
    }
}
