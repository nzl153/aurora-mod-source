using System.Threading.Tasks;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace AuroraMod.AuroraCode.Relics;

/// <summary>预载·积11热（E-02 解除安全阀）—— 下一场战斗首回合走标准 AddHeatAsync 积 11 热（0→11 一次越线登记 Pending+换区）后自移除。</summary>
public class AuroraPreloadHeat11Relic : AuroraCombatPreloadRelic
{
    protected override string ArtName => "preload_heat11";
    protected override int PreloadBit => AuroraPreloadConsumedPower.BitHeat11;

    private const int Heat = 11;

    protected override async Task ApplyPreloadAsync(PlayerChoiceContext ctx, Creature creature)
    {
        await HeatPower.AddHeatAsync(ctx, creature, Heat, null);
    }
}
