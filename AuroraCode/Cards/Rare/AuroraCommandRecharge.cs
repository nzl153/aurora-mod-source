using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Cards.Rare;

/// <summary>
/// D-R03 指令回充 / Command Recharge（稀有，D 指令连锁；消耗）。获得 4 格挡；打出前 ≥3 张抽 1+得 1 能量；≥6 张改为抽 2+得 2 能量。消耗。升级格挡 4→7。
/// 结算：读打出前手动出牌数一次 → 先获得格挡 → 6 档「替代」3 档（禁叠成 3 能量/3 抽）。Echo 重复资源收益但不推进连锁。
/// 0 费 + 消耗 + 连锁前置共同限制、不能自成稳定循环。D 从「已连锁」冲「高连锁」的一次性燃料，补足湮灭指令所需手牌与能量。
/// </summary>
public class AuroraCommandRecharge() : AuroraCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override string ArtName => "command_recharge";

    /// <summary>金框：本回合手动出牌数已达中档阈值（再高一档收益更好）时额外效果可触发（工坊反馈 #1，沿用原版 Dismantle/Spite 的金框语义）。</summary>
    protected override bool ShouldGlowGoldInternal => AuroraGlow.ChainAtLeast(this, MidThreshold);

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Chain];

    private const int MidThreshold = 3;
    private const int HighThreshold = 6;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(4, ValueProp.Move),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        var player = Owner;
        if (creature == null || player == null)
        {
            return;
        }

        var n = ChainPower.GetCount(creature);   // 打出前手动出牌数，只读一次
        await CreatureCmd.GainBlock(creature, (int)DynamicVars.Block.BaseValue, ValueProp.Move, cardPlay);

        // 6 档完全替代 3 档，不叠加。
        if (n >= HighThreshold)
        {
            await CardPileCmd.Draw(choiceContext, 2m, player);
            await PlayerCmd.GainEnergy(2m, player);
        }
        else if (n >= MidThreshold)
        {
            await CardPileCmd.Draw(choiceContext, 1m, player);
            await PlayerCmd.GainEnergy(1m, player);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);   // 4 → 7
    }
}
