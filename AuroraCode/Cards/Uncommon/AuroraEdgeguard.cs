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

namespace AuroraMod.AuroraCode.Cards.Uncommon;

/// <summary>
/// B3 剑势护体 / Edgeguard（罕见，B 剑势）。获得 7 格挡，再获得等同于当前剑势的格挡，<b>最多额外 8 格挡</b>。升级：基础 7→9，剑势读取上限 8→10。
/// 为非消耗剑势防御加读取上限——保留「持势防御」，但不让无上限剑势同时成为永久无上限格挡（与无月伤害并存）。
/// 结算：读当前剑势快照 → 基础格挡 → min(剑势, 上限) 格挡（均走 Move，同 cardPlay）。不清空剑势。Echo 每次按当时剑势（受上限）再给一份。
/// 1 费罕见位数值上调（对照原版 680 张卡解包统计：奥萝拉 1 费罕见攻击均值 7.2 / 中位 7，
/// 原版 9.4 / 8；格挡 6.1 / 6 对原版 8.4 / 7——该档是全卡池唯一明显洼地，而罕见奖励是玩家整局看得最多的三选一）。
/// </summary>
public class AuroraEdgeguard() : AuroraCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override string ArtName => "edgeguard";

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Momentum];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(7, ValueProp.Move),
        new DynamicVar("MomentumBlockCap", 8m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        var momentum = AuroraMomentumService.Get(creature);   // 当前剑势快照
        await CreatureCmd.GainBlock(creature, (int)DynamicVars.Block.BaseValue, ValueProp.Move, cardPlay);

        var bonus = System.Math.Min(momentum, (int)DynamicVars["MomentumBlockCap"].BaseValue);   // 剑势格挡封顶
        if (bonus > 0)
        {
            await CreatureCmd.GainBlock(creature, bonus, ValueProp.Move, cardPlay);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);                 // 7 → 9
        DynamicVars["MomentumBlockCap"].UpgradeValueBy(2m);   // 8 → 10
    }
}
