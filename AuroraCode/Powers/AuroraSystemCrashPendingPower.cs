using System.Threading.Tasks;
using AuroraMod.AuroraCode.Cards.Token;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// 隐藏计数：过热后在下个玩家回合开始，把 N 张「宕机」塞进初始手牌，然后移除自己。
/// （照 Kakarot 的 KakarotDrawNextTurnPower「下回合」模式。）
/// </summary>
public sealed class AuroraSystemCrashPendingPower : AuroraPower
{
    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => false;
    protected override string IconName => "system_crash";

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
        {
            return;
        }

        Flash();
        var creature = Owner;
        var count = (int)Amount;
        for (var i = 0; i < count; i++)
        {
            var card = creature.CombatState.CreateCard<AuroraSystemCrash>(player);
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player);
        }

        // 挂上「宕机攻击减伤」隐藏 Power（全场常驻、动态查手牌），已存在则不重复挂。
        if (creature.GetPowerAmount<AuroraSystemCrashPenaltyPower>() <= 0)
        {
            await AuroraMod.AuroraCode.Helpers.AuroraPowerCmd.Apply<AuroraSystemCrashPenaltyPower>(
                choiceContext, creature, 1, creature, null, silent: true);
        }

        await PowerCmd.Remove(this);
    }
}
