using System.Threading.Tasks;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace AuroraMod.AuroraCode.Relics;

/// <summary>预载·连携（E-03 载入连携记录）—— 下一场战斗首回合额外抽 2 张牌、随后积 2 热后自移除。抽牌不算手动打牌、不推连锁。</summary>
public class AuroraPreloadChainDrawRelic : AuroraCombatPreloadRelic
{
    protected override string ArtName => "preload_chain_draw";
    protected override int PreloadBit => AuroraPreloadConsumedPower.BitChainDraw;

    private const int DrawCount = 2;
    private const int Heat = 2;

    protected override async Task ApplyPreloadAsync(PlayerChoiceContext ctx, Creature creature)
    {
        var player = creature.Player;
        if (player != null)
        {
            await CardPileCmd.Draw(ctx, DrawCount, player);
        }

        if (CombatManager.Instance.IsInProgress)
        {
            await HeatPower.AddHeatAsync(ctx, creature, Heat, null);
        }
    }
}
