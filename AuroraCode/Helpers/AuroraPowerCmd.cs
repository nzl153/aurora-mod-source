using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace AuroraMod.AuroraCode.Helpers;

/// <summary>
/// 对 PowerCmd 的薄封装（照 Kakarot 的 KakarotPowerCmd）。无玩家选择上下文时用 ThrowingPlayerChoiceContext。
/// </summary>
internal static class AuroraPowerCmd
{
    private static PlayerChoiceContext NoChoiceContext() => new ThrowingPlayerChoiceContext();

    public static Task Apply<T>(Creature target, decimal amount, Creature applier, CardModel cardSource, bool silent = false)
        where T : PowerModel, new()
        => PowerCmd.Apply<T>(NoChoiceContext(), target, amount, applier, cardSource, silent);

    public static Task Apply<T>(PlayerChoiceContext ctx, Creature target, decimal amount, Creature applier, CardModel cardSource, bool silent = false)
        where T : PowerModel, new()
        => PowerCmd.Apply<T>(ctx, target, amount, applier, cardSource, silent);

    public static Task Apply(PlayerChoiceContext ctx, PowerModel power, Creature target, decimal amount, Creature applier, CardModel cardSource, bool silent = false)
        => PowerCmd.Apply(ctx, power, target, amount, applier, cardSource, silent);
}
