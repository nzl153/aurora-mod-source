using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuroraMod.AuroraCode.Helpers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// C-R03 自律兵装 Power—— 可见能力 Power。回合结束时若模块槽位<b>已满</b>：
/// 使所有模块额外触发 <see cref="Amount"/> 轮（每层自律兵装各 1 轮），每轮结束积 1 点热量。
/// 满槽判定读本人权威有效容量（<see cref="AuroraModuleController.IsFull"/>），辅助肩架扩到 3 槽后须 3/3 才满。
/// 模块的原生回合末触发另行发生，本能力给的是额外轮次；护盾模块也在回合末被手动触发。
/// 时序挂普通 <see cref="AfterSideTurnEnd"/>，早于 HeatDissipationCore 的过热结算——若这里的积热把你推过 10，
/// 当回合末就统一结算过热（延迟过热既定行为，同一回合末不拖延）。
/// </summary>
public sealed class AuroraAutonomousArsenalPower : AuroraPower
{
    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => true;
    protected override string IconName => "autonomous_arsenal";

    private const int HeatPerRound = 1;

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != Owner.Side || Amount <= 0)
        {
            return;
        }

        if (participants?.Contains(Owner) != true)
        {
            return;
        }

        // 与 HeatDissipationCore/蓄能架式对齐，战斗已结束则不再触发/积热。
        if (!CombatManager.Instance.IsInProgress)
        {
            return;
        }

        // 满槽才触发：不满槽既不触发模块、也不积热。
        if (!AuroraModuleController.IsFull(Owner))
        {
            return;
        }

        Flash();

        // 每层自律兵装：全模块额外触发 1 轮 → 积 1 热。战斗结束则停手、跳过剩余积热。
        for (var i = 0; i < (int)Amount; i++)
        {
            if (!CombatManager.Instance.IsInProgress)
            {
                break;
            }

            await AuroraModuleController.TriggerAsync(choiceContext, Owner);

            if (!CombatManager.Instance.IsInProgress)
            {
                break;
            }

            await HeatPower.AddHeatAsync(choiceContext, Owner, HeatPerRound, null);
        }
    }
}
