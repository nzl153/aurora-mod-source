using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using AuroraMod.AuroraCode.Cards.Common;
using AuroraMod.AuroraCode.Characters;
using AuroraMod.AuroraCode.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Events;

/// <summary>
/// E-01 沉默装配线 / The Silent Assembly Line（奥萝拉专属，第 1–2 幕）。停摆的装配线认出奥萝拉，供她接入火控或壁垒，或切断总线离开。
/// 接入火控/壁垒：失去当前生命（≈最大生命 8%，向上取整；不足以支付且会降至 0 则该选项 LockedOption 禁用）→ 获得一张已升级的《部署：利刃/壁垒》
/// → 授予对应「预载模块」遗物（下一场战斗首回合部署一枚模块，见 <see cref="AuroraCombatPreloadRelic"/>）。切断总线：无事发生。
/// 仅奥萝拉可遇（IsAllowed 查角色）；随机性不涉及（选项确定），联机一致。
/// </summary>
public class SilentAssemblyLine : CustomEventModel
{
    private const double HpCostFraction = 0.08;

    private const string PortraitPath = "res://Aurora/Images/Events/silent_assembly_line.png";

    public override string CustomInitialPortraitPath =>
        Godot.ResourceLoader.Exists(PortraitPath) ? PortraitPath : null;

    public override bool IsAllowed(IRunState runState) =>
        runState.CurrentActIndex <= 1
        && runState.Players.Any(p => p.Character is Aurora);

    private LocString Loc(string rel) => new(LocTable, Id.Entry + "." + rel);

    private int HpCost()
    {
        var maxHp = Owner?.Creature?.MaxHp ?? 0;
        return (int)Math.Ceiling(maxHp * HpCostFraction);
    }

    private bool CanAffordHp()
    {
        var creature = Owner?.Creature;
        return creature != null && creature.CurrentHp > HpCost();
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        // 混队守卫：非奥萝拉拥有者只能离开、不获得奥萝拉专属奖励/预载。
        if (Owner?.Character is not Aurora)
        {
            return new List<EventOption> { Option(Leave, Loc("pages.INITIAL.options.LEAVE.title"), Loc("pages.INITIAL.options.LEAVE.description")) };
        }

        var affordable = CanAffordHp();
        var fireTips = RewardTips<AuroraDeployBlade, AuroraPreloadAttackModuleRelic>();
        var bulwarkTips = RewardTips<AuroraDeployBulwark, AuroraPreloadShieldModuleRelic>();
        return new List<EventOption>
        {
            (affordable
                ? Option(FireControl, Loc("pages.INITIAL.options.FIRE_CONTROL.title"), Loc("pages.INITIAL.options.FIRE_CONTROL.description"), fireTips)
                : LockedOption("FIRE_CONTROL", "INITIAL", fireTips))
                .WithRelic<AuroraPreloadAttackModuleRelic>(Owner),
            (affordable
                ? Option(Bulwark, Loc("pages.INITIAL.options.BULWARK.title"), Loc("pages.INITIAL.options.BULWARK.description"), bulwarkTips)
                : LockedOption("BULWARK", "INITIAL", bulwarkTips))
                .WithRelic<AuroraPreloadShieldModuleRelic>(Owner),
            Option(CutBus, Loc("pages.INITIAL.options.CUT_BUS.title"), Loc("pages.INITIAL.options.CUT_BUS.description")),
        };
    }

    /// <summary>
    /// 选项悬停预览：升级态卡牌大图 + 该卡自身的机制说明 + 附赠遗物说明。
    /// 事件选项按钮会读 <see cref="EventOption.HoverTips"/> 渲染悬停框（NEventOptionButton.OnFocus →
    /// NHoverTipSet.CreateAndShow）；不传即空数组 → 悬停无任何提示，玩家不知道拿的是哪张卡/哪个遗物。
    /// 禁用态选项也照样给提示，让玩家知道自己错过了什么。纯展示：不改判定、不动随机数、不影响联机。
    /// </summary>
    private static IHoverTip[] RewardTips<TCard, TRelic>()
        where TCard : CardModel
        where TRelic : RelicModel
        => HoverTipFactory.FromCardWithCardHoverTips<TCard>(upgrade: true)
            .Concat(HoverTipFactory.FromRelic<TRelic>())
            .ToArray();

    private async Task FireControl()
    {
        await PayHpAsync();
        await AddUpgradedCardAsync<AuroraDeployBlade>();
        await RelicCmd.Obtain<AuroraPreloadAttackModuleRelic>(Owner);
        SetEventFinished(Loc("pages.FIRE_CONTROL.description"));
    }

    private async Task Bulwark()
    {
        await PayHpAsync();
        await AddUpgradedCardAsync<AuroraDeployBulwark>();
        await RelicCmd.Obtain<AuroraPreloadShieldModuleRelic>(Owner);
        SetEventFinished(Loc("pages.BULWARK.description"));
    }

    private Task CutBus()
    {
        SetEventFinished(Loc("pages.CUT_BUS.description"));
        return Task.CompletedTask;
    }

    private Task Leave()
    {
        SetEventFinished(Loc("pages.LEAVE.description"));
        return Task.CompletedTask;
    }

    private async Task PayHpAsync()
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), creature, HpCost(),
            ValueProp.Unblockable | ValueProp.Unpowered, null, null);
    }

    private async Task AddUpgradedCardAsync<T>() where T : CardModel, new()
    {
        var card = Owner.RunState.CreateCard<T>(Owner);
        CardCmd.Upgrade(card);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck), 2f);
    }
}
