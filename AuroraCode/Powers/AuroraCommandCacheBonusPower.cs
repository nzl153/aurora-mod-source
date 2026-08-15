using System.Threading.Tasks;
using AuroraMod.AuroraCode.Helpers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// 指令缓存「升级触发格挡 + 积热」累计 —— 隐藏 Power，<b>权威=<see cref="AuroraPower.Amount"/> 位打包</b>（低 8 位=总格挡，次 8 位=总积热）。
/// 原先存 DynamicVar["BlockOnTrigger"]/["HeatOnTrigger"]，重连后丢失。用自定义累加(SetAmount)而非 Apply 叠加，故位打包安全。
/// 主抽牌数仍在可见 Power 的 Amount；可见 Power 的 {BlockOnTrigger}/{HeatOnTrigger} DV 仅作展示镜像、从本 Power 派生。
/// </summary>
public sealed class AuroraCommandCacheBonusPower : AuroraPower
{
    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => false;

    private const int HeatShift = 8;
    private const int Mask = 0xFF;

    public static int Block(Creature creature) =>
        (int)(creature?.GetPowerAmount<AuroraCommandCacheBonusPower>() ?? 0) & Mask;

    public static int Heat(Creature creature) =>
        ((int)(creature?.GetPowerAmount<AuroraCommandCacheBonusPower>() ?? 0) >> HeatShift) & Mask;

    public static async Task AddAsync(PlayerChoiceContext ctx, Creature creature, int block, int heat)
    {
        if (creature == null || (block <= 0 && heat <= 0))
        {
            return;
        }

        // 累加后 clamp 到 0..255，避免超 255 时 & 0xFF 回绕把高值绕成小值（虽实战难触及，防御性硬化）。
        int addBlock = System.Math.Max(0, block);
        int addHeat = System.Math.Max(0, heat);

        var power = creature.GetPower<AuroraCommandCacheBonusPower>();
        if (power == null)
        {
            int packed = System.Math.Min(Mask, addBlock) | (System.Math.Min(Mask, addHeat) << HeatShift);
            await AuroraPowerCmd.Apply<AuroraCommandCacheBonusPower>(ctx, creature, packed, creature, null, silent: true);
            return;
        }

        power.AssertMutable();
        int cur = (int)power.Amount;
        int newBlock = System.Math.Min(Mask, (cur & Mask) + addBlock);
        int newHeat = System.Math.Min(Mask, ((cur >> HeatShift) & Mask) + addHeat);
        power.SetAmount(newBlock | (newHeat << HeatShift));
    }
}
