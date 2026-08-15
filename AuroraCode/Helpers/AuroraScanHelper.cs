using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace AuroraMod.AuroraCode.Helpers;

/// <summary>
/// 扫描统一工具（架构 §10 / §14.1）—— 唯一负责扫描的同步选择与静默换堆。
///
/// 扫描 N：查看抽牌堆顶 N 张（<c>DrawPile.Cards[0]</c> 为顶），可选任意张放入弃牌堆底部；
/// 未选牌保持原相对顺序（我们只移走被选中的，其余不动）。不足 N 只看现有、空堆返回空、不洗牌、不耗 RNG。
///
/// <b>扫描不是弃牌事件</b>：用 <see cref="CardPileCmd.Add"/> 换堆（会正常触发 AfterCardChangedPiles/牌堆表现），
/// 但<b>不</b>走 <c>CardCmd.Discard</c>，因此不写弃牌历史、不触发 AfterCardDiscarded / 灵动(Sly) 等弃牌专属效果。
/// <see cref="CardPileCmd.Add"/> 第 4 参数（本游戏版本签名名为 <c>source</c>）会原样传入 AfterCardChangedPiles
/// 的最后一参（即 clonedBy 语义），故此处固定传 <c>null</c>，避免污染复制递归保护（S13）；扫描 source 只用于日志。
///
/// 卡牌只调 <see cref="ScanAsync"/>，不自己操牌堆（§14 分层）。多人同步只依赖传入的
/// <see cref="PlayerChoiceContext"/> + <see cref="CardSelectCmd.FromSimpleGrid"/> + 游戏原生同步器。
/// </summary>
internal static class AuroraScanHelper
{
    private static readonly LocString ScanPrompt = new("combat_messages", "AURORAMOD_SCAN_SELECT");

    /// <summary>
    /// 扫描 <paramref name="n"/>。返回<b>实际成功移入弃牌堆</b>的牌，按扫描前候选（抽牌堆顶→底）顺序排列。
    /// 调用方读 <c>result.Count</c> 取实际移动数、或读被移动牌的 Type/Cost/标签兑现收益。
    /// </summary>
    public static async Task<IReadOnlyList<CardModel>> ScanAsync(PlayerChoiceContext ctx, Player player, int n, AbstractModel source)
    {
        // n==0：不开 UI、不记错、返回空。
        if (n == 0)
        {
            return Array.Empty<CardModel>();
        }

        // n<0：记错（含 source/player/n），返回空，绝不解释为反向操作。
        if (n < 0)
        {
            GD.PushError($"[Aurora][Scan] 拒绝负数 N={n}（player={player?.NetId}, source={source?.GetType().Name}）。");
            return Array.Empty<CardModel>();
        }

        // 开 UI 前校验战斗有效性。
        if (!IsCombatValid(ctx, player, source))
        {
            return Array.Empty<CardModel>();
        }

        var drawPile = player.PlayerCombatState.DrawPile;

        // 稳定快照：抽牌堆顶 min(n, 堆量) 张。空堆正常返回、不记错。
        var candidates = drawPile.Cards.Take(Math.Min(n, drawPile.Cards.Count)).ToList();
        if (candidates.Count == 0)
        {
            return Array.Empty<CardModel>();
        }

        // 0..N 选择；不设 Comparison（不排序）；不覆盖自动生成的 RequireManualConfirmation。
        var prefs = new CardSelectorPrefs(ScanPrompt, 0, candidates.Count);
        var chosen = await CardSelectCmd.FromSimpleGrid(ctx, candidates, player, prefs);

        // 选择返回后再次校验战斗有效性（选择期间可能战斗结束/玩家死亡）。
        if (!IsCombatValid(ctx, player, source))
        {
            return Array.Empty<CardModel>();
        }

        // 规范化：UI 返回顺序不作数，按候选原顶→底顺序筛出被选牌（按实例判定，不按名称重查）。
        var chosenSet = new HashSet<CardModel>(chosen);
        var normalizedSelected = candidates.Where(c => chosenSet.Contains(c)).ToList();

        var discardPile = player.PlayerCombatState.DiscardPile;
        var moved = new List<CardModel>();

        foreach (var card in normalizedSelected)
        {
            // 移动前逐张校验：仍属本 player、仍在原抽牌堆、属于候选快照、未被移出状态。无效则忽略并记错，不找同名替代。
            if (card == null || card.Owner != player || card.Pile != drawPile
                || !candidates.Contains(card) || card.HasBeenRemovedFromState)
            {
                GD.PushError($"[Aurora][Scan] 跳过无效候选牌（card={card?.Id.Entry}, source={source?.GetType().Name}）。");
                continue;
            }

            // 静默换堆到弃牌堆底：第 4 参数固定 null（=AfterCardChangedPiles 的 clonedBy），source 不入此参。
            await CardPileCmd.Add(card, discardPile, CardPilePosition.Bottom, null, skipVisuals: false);

            // 只把「确实已在弃牌堆」的牌计入收益；移动失败的不计。
            if (card.Pile == discardPile)
            {
                moved.Add(card);
            }
            else
            {
                GD.PushError($"[Aurora][Scan] 换堆后牌未在弃牌堆，未计入收益（card={card.Id.Entry}）。");
            }
        }

        return moved;
    }

    private static bool IsCombatValid(PlayerChoiceContext ctx, Player player, AbstractModel source)
    {
        if (ctx == null || player == null || source == null)
        {
            return false;
        }

        var creature = player.Creature;
        if (creature == null || creature.IsDead)
        {
            return false;
        }

        if (player.PlayerCombatState == null || creature.CombatState == null)
        {
            return false;
        }

        return CombatManager.Instance.IsInProgress && !CombatManager.Instance.IsEnding;
    }
}
