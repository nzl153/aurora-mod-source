using System.Threading.Tasks;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Relics;

/// <summary>
/// R-01 脉冲节拍器 / Pulse Metronome（普通，D 指令连锁）—— 每回合连锁激活时获得 5 格挡，随后积 1 热。
/// 挂 <see cref="IAuroraChainListener"/>，由 <see cref="ChainPower"/> 在第 3 张手动牌越阈那刻派发（每回合天然至多一次；Echo/复制/自动结算不推进不触发）。
/// 顺序：先格挡后积热；这 1 热可换区/登记待结算过热，只影响后续牌，不反改本次激活瞬间其它效果已选的热区分支。战斗已结束跳过积热。
/// </summary>
public class PulseMetronome : AuroraRelic, IAuroraChainListener
{
    public override RelicRarity Rarity => RelicRarity.Common;
    protected override string ArtName => "pulse_metronome";

    private const int Block = 5;
    private const int Heat = 1;

    public async Task OnChainActivatedAsync(PlayerChoiceContext ctx, Creature owner)
    {
        if (owner == null || owner != Owner?.Creature || !CombatManager.Instance.IsInProgress)
        {
            return;
        }

        Flash();
        await CreatureCmd.GainBlock(owner, Block, ValueProp.Unpowered, null);

        if (CombatManager.Instance.IsInProgress)
        {
            await HeatPower.AddHeatAsync(ctx, owner, Heat, null);
        }
    }
}
