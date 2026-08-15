using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// 连锁激活监听接口。<see cref="ChainPower"/> 在本回合出牌数<b>恰好越过</b>
/// <see cref="ChainPower.ChainThreshold"/>（2→3）那一刻、于同步 <see cref="PlayerChoiceContext"/> action 链内
/// await 派发一次。因连锁数每回合只递增、越线只发生一次，故本事件天然「每回合至多一次」。
/// 用于 #54 指令缓存（首次连锁抽牌），后续零延迟 / 脉冲编译器遗物复用同一事件。
/// 约定：快照派发；不消耗 RNG；绝不 fire-and-forget；复制/自动打出不推进连锁（<see cref="ChainPower"/> 已处理）。
/// </summary>
public interface IAuroraChainListener
{
    Task OnChainActivatedAsync(PlayerChoiceContext ctx, Creature owner);
}
