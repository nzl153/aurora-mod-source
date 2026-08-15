using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace AuroraMod.AuroraCode.Cards.Rare;

/// <summary>
/// 76 辅助肩架 / Auxiliary Hardpoint（稀有，C；改规则）。2 费能力：本场模块容量 +1（硬上限 3），随后同步选择部署 1 枚攻击或护盾模块。升级费用 2→1。
/// 容量语义（§4.7）：只持续本场（权威 <see cref="AuroraModuleCapacityPower"/>，Amount 0/1），基础 2、硬上限 3、唯一扩容来源；
/// 多张/复制/重复打出都不到 4 槽（额外容量与 CurrentCapacity 均钳制）。顺序：先扩容 → 同步选类型 → 经控制器部署。
/// 已满 2 枚先扩到 3 再直接占第 3 槽（不弹替换）；已满 3 枚容量保持 3、选型后走既有三候选替换 UI。
/// 扩容本身不算部署（不触发「部署时」能力）；类型/替换选择失败则容量保留、跳过部署，不默认某型/最弱者。
/// </summary>
public class AuroraAuxiliaryHardpoint() : AuroraCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override string ArtName => "auxiliary_hardpoint";

    protected override IEnumerable<AuroraMechanic> MechanicTips =>
        [AuroraMechanic.ModuleCapacity, AuroraMechanic.AttackModule, AuroraMechanic.ShieldModule];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        // 1. 本场容量 +1（额外容量已达上限则不再叠，避免 Amount 无意义增长；CurrentCapacity 天然钳制到 3）。
        if (AuroraModuleCapacityPower.GetExtra(creature) < AuroraModuleCapacityPower.MaxExtra)
        {
            await AuroraPowerCmd.Apply<AuroraModuleCapacityPower>(choiceContext, creature, 1, creature, this);
        }

        // 2+3. 同步选择模块类型并经控制器部署（满槽由 DeployAsync 内部走既有替换 UI）。
        await AuroraModuleController.DeployChosenTypeAsync(choiceContext, creature, this);
    }

    protected override void OnUpgrade()
    {
        // 必须用 EnergyCost.UpgradeBy，不是 UpgradeStarCostBy（工坊反馈 #1，2026-07-31）。
        // 引擎有两套独立费用：CardModel 构造函数传的 cost → CanonicalEnergyCost（能量费，卡面显示的那个）；
        // 星费是另一套，CanonicalStarCost 默认 -1，本卡压根没有。原来调 UpgradeStarCostBy 改的是没人读的星费，
        // 导致升级前后卡面完全一致、费用也不降 —— 玩家看到的「升级没区别」正是这个。
        // 引擎 CardModel.OnUpgrade 的注释明写：「To upgrade a card's energy cost, use CardEnergyCost.UpgradeBy(int)」。
        EnergyCost.UpgradeBy(-1);   // 2 → 1
    }
}
