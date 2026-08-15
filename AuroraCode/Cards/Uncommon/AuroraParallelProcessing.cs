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
/// 52 并行处理 / Parallel Processing（罕见，D 扫描+连锁）。获得 4 格挡，扫描 4；每实际移走 1 张牌再获得 1 格挡；
/// 若打出前已连锁，随后抽 2。升级：基础格挡 4→6，每张额外格挡 1→2（扫描、抽牌不变）。
/// （扫描实为「查抽牌堆顶」非看全手牌、没那么强，回调连锁抽 1→2 恢复原设计。）
/// 结算（连锁快照→基础格挡→同步扫描→按实际移动数一次性得额外格挡→若打出前已连锁则抽 2）：
/// 连锁只读一次打出前快照（<see cref="ChainPower.GetIsChained"/>），本牌自身若是第 3 张手动牌则 wasChained=false 不抽；
/// 额外格挡严格用 <see cref="AuroraScanHelper.ScanAsync"/> 返回列表的 Count（UI 选中但换堆失败的不算）。
/// </summary>
public class AuroraParallelProcessing() : AuroraCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override string ArtName => "parallel_processing";

    /// <summary>金框：已连锁时额外效果可触发（工坊反馈 #1，沿用原版 Dismantle/Spite 的金框语义）。</summary>
    protected override bool ShouldGlowGoldInternal => AuroraGlow.Chained(this);

    protected override IEnumerable<AuroraMechanic> MechanicTips =>
        [AuroraMechanic.Scan, AuroraMechanic.Chain];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(4, ValueProp.Move),
        new DynamicVar("ScanCount", 4m),
        new DynamicVar("BlockPerMoved", 1m),
        new DynamicVar("DrawCount", 2m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        var player = Owner;
        if (creature == null || player == null)
        {
            return;
        }

        // 1. 打出前只读一次连锁状态（本牌自身若为第 3 张手动牌，此时仍未连锁）。
        var wasChained = ChainPower.GetIsChained(creature);

        // 2. 基础格挡 4/6。
        await CreatureCmd.GainBlock(creature, (int)DynamicVars.Block.BaseValue, ValueProp.Move, cardPlay);

        // 3. 同步扫描，取实际移入弃牌堆的数量。
        var moved = await AuroraScanHelper.ScanAsync(choiceContext, player, (int)DynamicVars["ScanCount"].BaseValue, this);

        // 4. 按实际移动数一次性获得额外格挡。
        if (moved.Count > 0)
        {
            var extra = moved.Count * (int)DynamicVars["BlockPerMoved"].BaseValue;
            await CreatureCmd.GainBlock(creature, extra, ValueProp.Move, cardPlay);
        }

        // 5. 仅当「打出前已连锁」时抽 2（本牌完成后才激活的连锁不算）。
        //    额外叠 IsFirstInSeries 守卫：playCount>1 / Echo 复制的第 2+ 次额外结算不重复吃连锁抽牌
        //    （与 ChainPower 同一守卫；正常单次手动打出 IsFirstInSeries=true，不影响「第4张抽2」）。
        if (wasChained && cardPlay.IsFirstInSeries)
        {
            await CardPileCmd.Draw(choiceContext, DynamicVars["DrawCount"].BaseValue, player);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);               // 4 → 6
        DynamicVars["BlockPerMoved"].UpgradeValueBy(1m);    // 1 → 2
    }
}
