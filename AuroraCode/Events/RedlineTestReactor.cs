using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using AuroraMod.AuroraCode.Characters;
using AuroraMod.AuroraCode.Relics;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace AuroraMod.AuroraCode.Events;

/// <summary>
/// E-02 红线试验炉 / The Redline Test Reactor（奥萝拉专属，第 2–3 幕）。
/// 解除安全阀：升级最多 2 张可升级牌（无可升级则禁用）→ 授予「预载·积11热」（下一场战斗首回合走标准 AddHeatAsync 积 11 热）。
/// 抽干冷却液：支付 60 金币、回复 25% 最大生命（金币不足或已满血则禁用）。封死炉门：无事发生。仅奥萝拉可遇。
/// </summary>
public class RedlineTestReactor : CustomEventModel
{
    private const int GoldCost = 60;
    private const double HealFraction = 0.25;
    private const int UpgradeMax = 2;

    public override string CustomInitialPortraitPath =>
        ResourceLoader.Exists(PortraitPath) ? PortraitPath : null;

    private const string PortraitPath = "res://Aurora/Images/Events/redline_test_reactor.png";

    public override bool IsAllowed(IRunState runState) =>
        runState.CurrentActIndex is >= 1 and <= 2
        && runState.Players.Any(p => p.Character is Aurora);

    private LocString Loc(string rel) => new(LocTable, Id.Entry + "." + rel);

    private bool HasUpgradable() => Owner?.Deck.Cards.Any(c => c?.IsUpgradable ?? false) ?? false;

    private bool CanDrain()
    {
        var creature = Owner?.Creature;
        return Owner != null && creature != null && Owner.Gold >= GoldCost && creature.CurrentHp < creature.MaxHp;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        // 混队守卫：非奥萝拉拥有者只能离开。
        if (Owner?.Character is not Aurora)
        {
            return new List<EventOption> { Option(Leave, Loc("pages.INITIAL.options.LEAVE.title"), Loc("pages.INITIAL.options.LEAVE.description")) };
        }

        // 选项悬停预览：本选项附赠「预载·积11热」，不给 HoverTips 则悬停一片空白、玩家不知道会拿到什么
        // （NEventOptionButton.OnFocus → NHoverTipSet.CreateAndShow(Option.HoverTips)）。禁用态也照给。
        // 纯展示：不改判定、不动随机数、不影响联机。另两个选项（金币换血 / 离开）无遗物卡牌奖励，文案已说清，不加。
        var valveTips = HoverTipFactory.FromRelic<AuroraPreloadHeat11Relic>().ToArray();
        return new List<EventOption>
        {
            (HasUpgradable()
                ? Option(ReleaseValve, Loc("pages.INITIAL.options.RELEASE_VALVE.title"), Loc("pages.INITIAL.options.RELEASE_VALVE.description"), valveTips)
                : LockedOption("RELEASE_VALVE", "INITIAL", valveTips))
                .WithRelic<AuroraPreloadHeat11Relic>(Owner),
            CanDrain()
                ? Option(DrainCoolant, Loc("pages.INITIAL.options.DRAIN_COOLANT.title"), Loc("pages.INITIAL.options.DRAIN_COOLANT.description"))
                : LockedOption("DRAIN_COOLANT"),
            Option(SealHatch, Loc("pages.INITIAL.options.SEAL_HATCH.title"), Loc("pages.INITIAL.options.SEAL_HATCH.description")),
        };
    }

    private Task Leave()
    {
        SetEventFinished(Loc("pages.LEAVE.description"));
        return Task.CompletedTask;
    }

    private async Task ReleaseValve()
    {
        var prefs = new CardSelectorPrefs(Loc("upgradePrompt"), 0, UpgradeMax);
        var chosen = (await CardSelectCmd.FromDeckForUpgrade(Owner, prefs)).ToList();
        foreach (var card in chosen)
        {
            CardCmd.Upgrade(card);
        }

        await RelicCmd.Obtain<AuroraPreloadHeat11Relic>(Owner);
        SetEventFinished(Loc("pages.RELEASE_VALVE.description"));
    }

    private async Task DrainCoolant()
    {
        await PlayerCmd.LoseGold(GoldCost, Owner);
        var heal = (int)Math.Ceiling((Owner?.Creature?.MaxHp ?? 0) * HealFraction);
        if (heal > 0 && Owner?.Creature != null)
        {
            await CreatureCmd.Heal(Owner.Creature, heal);
        }

        SetEventFinished(Loc("pages.DRAIN_COOLANT.description"));
    }

    private Task SealHatch()
    {
        SetEventFinished(Loc("pages.SEAL_HATCH.description"));
        return Task.CompletedTask;
    }
}
