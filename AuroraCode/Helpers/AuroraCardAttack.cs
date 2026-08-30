using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Helpers;

/// <summary>统一正式版与 Beta 的 BaseLib CardAttack 签名。</summary>
internal static class AuroraCardAttack
{
    public static AttackCommand Create(
        CardModel card,
        CardPlay cardPlay,
        Creature target,
        decimal damage,
        ValueProp valueProp)
    {
#if STS2_BETA
        return CommonActions.CardAttack(card, cardPlay, target, damage, valueProp);
#else
        return CommonActions.CardAttack(card, target, damage, valueProp);
#endif
    }
}
