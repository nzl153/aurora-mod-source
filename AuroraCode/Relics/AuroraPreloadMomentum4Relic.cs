using System.Threading.Tasks;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace AuroraMod.AuroraCode.Relics;

/// <summary>预载·4剑势（E-03 载入独剑记录）—— 下一场战斗首回合获得 4 剑势后自移除。</summary>
public class AuroraPreloadMomentum4Relic : AuroraCombatPreloadRelic
{
    protected override string ArtName => "preload_momentum4";
    protected override int PreloadBit => AuroraPreloadConsumedPower.BitMomentum4;

    private const int Momentum = 4;

    protected override async Task ApplyPreloadAsync(PlayerChoiceContext ctx, Creature creature)
    {
        await AuroraMomentumService.GainAsync(ctx, creature, Momentum, null);
    }
}
