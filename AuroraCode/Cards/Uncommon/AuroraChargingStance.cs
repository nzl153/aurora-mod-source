using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace AuroraMod.AuroraCode.Cards.Uncommon;

/// <summary>
/// 过载引擎 / Overload Engine（罕见，1 费，能力）。本场战斗中，你打出的每张攻击牌额外积 1 点热量。
/// 升级：费用 1 → 0。
///
/// <b>⚠ 类名与文件名保持 ChargingStance 不变，这是刻意的</b>：卡牌 ID 与本地化 key 由类名派生
/// （<c>AURORAMOD-AURORA_CHARGING_STANCE</c>），Aurora 已发布，改名会让进行中的存档找不到这张牌。
///
/// 由旧「蓄能架式」原地重做。旧效果是「回合结束时若本回合未打出攻击牌则给剑势」——
/// 94 张牌里 35 张是攻击牌，那个条件与角色主业直接对立，实战几乎不触发，是全套最鸡肋的一张。
/// 新效果把同一个「攻击 ↔ 热量」母题翻到有用的一侧，并成为过载流派缺失的启动器。
///
/// 设计约束（刻意为之，别加条件）：
/// ① 一句话零条件。工坊反馈奥萝拉晦涩，流派核心必须是全套最好读的牌之一。
/// ② 升级只降费不加字，保住可读性，同时让引擎更早铺下去。
/// ③ 代价天然存在，不必额外写：过热伤害递增 10/12/14/16 且每次生成宕机，
///    烧得快＝挨得多＝牌库变脏，玩家得自行凑余热装甲/超频/灰烬复燃。
///
/// 对剑势流派不是抽血而是供血：炉心淬锋（过热→剑势）与热喂专注（热量→剑势）都吃它的产出。
/// </summary>
public class AuroraChargingStance() : AuroraCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override string ArtName => "charging_stance";

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Heat];

    private const int Stacks = 1;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("HeatPerAttack", AuroraChargingStancePower.HeatPerAttack),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        await AuroraChargingStancePower.ApplyAsync(choiceContext, creature, Stacks, this);
    }

    protected override void OnUpgrade()
    {
        // 升级只降费：1 → 0。不加任何文本，保住这张流派核心的可读性。
        // 必须用 EnergyCost.UpgradeBy，不是 UpgradeStarCostBy——后者改的是没人读的星费（工坊反馈 #1）。
        EnergyCost.UpgradeBy(-1);
    }
}
