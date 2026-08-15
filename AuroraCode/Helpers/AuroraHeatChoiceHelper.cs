using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using AuroraMod.AuroraCode.Cards.Token;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace AuroraMod.AuroraCode.Helpers;

/// <summary>
/// 热量二选一原语（#25 微调专用）：让本人同步在「积 1 热 / 散 1 热」中选一侧并执行。
/// 候选按稳定索引（[积热, 散热]）两端一致构建 → <see cref="CardSelectCmd.FromSimpleGrid"/> 的按索引同步
/// 在单人 / 远端 / 重连下解析到同一侧。取消选择 = 什么都不做（不本地默认某侧）。
/// 返回本次操作是否<b>跨越了热区阈值</b>（供调用方决定是否给额外收益）；取消 / 无实际变动均返回 false。
/// </summary>
internal static class AuroraHeatChoiceHelper
{
    private static readonly LocString ChoosePrompt = new("combat_messages", "AURORAMOD_HEAT_CHOOSE");

    /// <summary>
    /// 弹出二选一并执行选中的热量调整。<paramref name="delta"/> 固定为 1（积 1 / 散 1）。
    /// </summary>
    public static async Task<bool> ChooseAdjustAsync(PlayerChoiceContext ctx, Creature creature, CardModel source)
    {
        var player = creature?.Player;
        var combat = creature?.CombatState;
        if (player == null || combat == null)
        {
            return false;
        }

        // 稳定顺序：索引 0 = 积热，索引 1 = 散热。两端各自据此构建 → 按索引同步解析到同一侧。
        var gainToken = combat.CreateCard<AuroraGainHeatToken>(player);
        var ventToken = combat.CreateCard<AuroraVentHeatToken>(player);
        var options = new List<CardModel> { gainToken, ventToken };

        var prefs = new CardSelectorPrefs(ChoosePrompt, 1, 1);
        var chosen = (await CardSelectCmd.FromSimpleGrid(ctx, options, player, prefs)).ToList();
        if (chosen.Count == 0)
        {
            // 取消：不默认、不调热。
            return false;
        }

        var index = options.IndexOf(chosen[0]);
        if (index < 0)
        {
            GD.PushError("[Aurora][Heat] 微调二选一索引无效，跳过。");
            return false;
        }

        // 选择期间战斗可能推进：调热前重读区段快照。
        var zoneBefore = HeatPower.GetZone(creature);
        if (index == 0)
        {
            await HeatPower.AddHeatAsync(ctx, creature, 1, source);
        }
        else
        {
            await HeatPower.VentUpToAsync(ctx, creature, 1, source);
        }

        var zoneAfter = HeatPower.GetZone(creature);
        return zoneAfter != zoneBefore;
    }
}
