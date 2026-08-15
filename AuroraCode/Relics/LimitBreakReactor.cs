using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace AuroraMod.AuroraCode.Relics;

/// <summary>
/// R-04 破限反应堆 / Limit-Break Reactor（本应 Boss 级；RelicRarity 枚举无 Boss，暂用 Rare，见反馈）—— A 过热。
/// 每回合开始 +1 能量；每回合结束、在延迟过热统一结算前积 2 热。
/// 回合末积热挂 <see cref="AfterSideTurnEnd"/>（早于散热核心的 <see cref="AfterSideTurnEndLate"/> 过热结算）——若这 2 热越线则登记待结算过热、同回合末立即结算。
/// 玩家可在结束回合前主动散热，但回合末仍会重新积 2 热；战斗已胜利则回合末钩子不触发、不再积热。多件回合末积热正常累加，最终仍只统一结算现有待结算过热。
/// </summary>
public class LimitBreakReactor : AuroraRelic
{
    // TODO(反馈): 规格标 Boss 稀有度，但 RelicRarity 枚举仅 Starter/Common/Uncommon/Rare/Shop/Event/Ancient，无 Boss。
    // 暂用 Rare；若需真正 Boss 奖励池，须确认引擎 Boss 遗物注册路径（可能非 RelicRarity 而是奖励生成侧）。
    public override RelicRarity Rarity => RelicRarity.Rare;
    protected override string ArtName => "limit_break_reactor";

    private const int Energy = 1;
    private const int Heat = 2;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        var creature = Owner?.Creature;
        if (creature == null || player != creature.Player || !CombatManager.Instance.IsInProgress)
        {
            return;
        }

        Flash();
        await PlayerCmd.GainEnergy(Energy, player);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        var creature = Owner?.Creature;
        if (creature == null || !CombatManager.Instance.IsInProgress || participants?.Contains(creature) != true)
        {
            return;
        }

        // 回合末积 2 热：本钩子早于 AfterSideTurnEndLate 的过热结算 → 语义为"过热结算前积热"，越线则同回合末统一结算。
        Flash();
        await HeatPower.AddHeatAsync(choiceContext, creature, Heat, null);
    }
}
