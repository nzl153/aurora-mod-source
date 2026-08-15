using System.Threading.Tasks;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace AuroraMod.AuroraCode.Relics;

/// <summary>预载·攻击模块（E-01 接入火控）—— 下一场战斗首回合部署 1 枚攻击模块后自移除。</summary>
public class AuroraPreloadAttackModuleRelic : AuroraCombatPreloadRelic
{
    protected override string ArtName => "preload_attack_module";
    protected override int PreloadBit => AuroraPreloadConsumedPower.BitAttackModule;

    protected override async Task ApplyPreloadAsync(PlayerChoiceContext ctx, Creature creature)
    {
        await AuroraModuleController.DeployAsync(ctx, creature, ModuleKind.Attack, null);
    }
}
