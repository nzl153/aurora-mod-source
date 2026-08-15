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
/// 事件「下一场战斗预载」遗物抽象基类 —— 由专属事件用 <see cref="RelicCmd.Obtain{T}"/> 授予；地图可见（符合规格「预载可查看、非隐藏惩罚」）；
/// 下一场战斗<b>首个本人回合开始</b>发放预载效果后 <see cref="RelicCmd.Remove"/> 自移除 → 只消费一次、不延续第二场。
/// Rarity=Event（不进战斗/商店/精英奖励袋——GrabBag 只收 Common/Uncommon/Rare/Shop）。
/// 消费顺序固定：守卫 → 若门闩已置=半消费残留则仅清理不重发 → 先 Apply 战斗态门闩 <see cref="AuroraPreloadConsumedPower"/>（重连恢复防二次）→ Flash+效果 → 自移除。
/// 之所以在 AfterPlayerTurnStart 而非 BeforeCombatStart：后者 IsInProgress 可能未置真、AddHeatAsync 会早退吞热（同 HeatLinedScabbard 教训）。
/// 默认「一进战斗回合就消费」：即使效果内部部署被取消/失败，也已置门闩并移除，避免预载跨多场卡死。
/// </summary>
public abstract class AuroraCombatPreloadRelic : AuroraRelic
{
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <summary>本预载在 <see cref="AuroraPreloadConsumedPower"/> 门闩里的 bit（存入 Amount 位掩码）。</summary>
    protected abstract int PreloadBit { get; }

    /// <summary>发放本预载效果（走既有服务：DeployAsync / AddHeatAsync / GainAsync / Draw）。</summary>
    protected abstract Task ApplyPreloadAsync(PlayerChoiceContext ctx, Creature creature);

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        var creature = Owner?.Creature;
        if (creature == null || player != creature.Player || !CombatManager.Instance.IsInProgress)
        {
            return;
        }

        // 门闩已置但遗物仍在 = 半消费残留（重连落在钩子执行中途）→ 仅清理、不重发效果。
        if (AuroraPreloadConsumedPower.IsConsumed(creature, PreloadBit))
        {
            await RelicCmd.Remove(this);
            return;
        }

        // 先置战斗态门闩（重连恢复后 IsConsumed 为真）→ 再发效果 → 自移除。
        await AuroraPreloadConsumedPower.MarkAsync(choiceContext, creature, PreloadBit);
        Flash();
        await ApplyPreloadAsync(choiceContext, creature);
        await RelicCmd.Remove(this);
    }
}
