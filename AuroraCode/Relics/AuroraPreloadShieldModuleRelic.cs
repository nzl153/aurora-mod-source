using System.Threading.Tasks;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace AuroraMod.AuroraCode.Relics;

/// <summary>预载·护盾模块（E-01 接入壁垒）—— 下一场战斗首回合部署 1 枚护盾模块后自移除。</summary>
public class AuroraPreloadShieldModuleRelic : AuroraCombatPreloadRelic
{
    protected override string ArtName => "preload_shield_module";
    protected override int PreloadBit => AuroraPreloadConsumedPower.BitShieldModule;

    protected override async Task ApplyPreloadAsync(PlayerChoiceContext ctx, Creature creature)
    {
        await AuroraModuleController.DeployAsync(ctx, creature, ModuleKind.Shield, null);
    }
}
