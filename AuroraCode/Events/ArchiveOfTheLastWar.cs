using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.Characters;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Relics;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Events;

/// <summary>
/// E-03 终战指令库 / Archive of the Last War（奥萝拉专属，第 1–2 幕）。
/// 载入独剑记录：失去当前生命 → 从 3 张随机 B·剑势 普通/罕见牌中选 1 张加入牌组 → 授予「预载·4剑势」。
/// 载入连携记录：失去当前生命 → 从 3 张随机 D·连锁 普通/罕见牌中选 1 张加入牌组 → 授予「预载·连携」（下一场首回合抽2+积2热）。
/// 执行遗忘：失去 5 点最大生命 → 从牌组移除 1 张牌（最大生命不足则禁用）。候选卡按 <see cref="EventModel.Rng"/> 抽取（联机一致）。仅奥萝拉可遇。
/// </summary>
public class ArchiveOfTheLastWar : CustomEventModel
{
    private const double HpCostFraction = 0.08;
    private const int MaxHpCost = 5;
    private const int CandidateCount = 3;

    public override string CustomInitialPortraitPath =>
        ResourceLoader.Exists(PortraitPath) ? PortraitPath : null;

    private const string PortraitPath = "res://Aurora/Images/Events/archive_of_the_last_war.png";

    public override bool IsAllowed(IRunState runState) =>
        runState.CurrentActIndex <= 1
        && runState.Players.Any(p => p.Character is Aurora);

    private LocString Loc(string rel) => new(LocTable, Id.Entry + "." + rel);

    private int HpCost() => (int)Math.Ceiling((Owner?.Creature?.MaxHp ?? 0) * HpCostFraction);

    private bool CanAffordHp()
    {
        var creature = Owner?.Creature;
        return creature != null && creature.CurrentHp > HpCost();
    }

    private bool CanForget() => (Owner?.Creature?.MaxHp ?? 0) > MaxHpCost;

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        // 混队守卫：非奥萝拉拥有者只能离开。
        if (Owner?.Character is not Aurora)
        {
            return new List<EventOption> { Option(Leave, Loc("pages.INITIAL.options.LEAVE.title"), Loc("pages.INITIAL.options.LEAVE.description")) };
        }

        var affordable = CanAffordHp();
        var bladeTips = RewardTips<AuroraPreloadMomentum4Relic>(AuroraMechanic.Momentum);
        var sequenceTips = RewardTips<AuroraPreloadChainDrawRelic>(AuroraMechanic.Chain);
        return new List<EventOption>
        {
            (affordable
                ? Option(LoadLoneBlade, Loc("pages.INITIAL.options.LONE_BLADE.title"), Loc("pages.INITIAL.options.LONE_BLADE.description"), bladeTips)
                : LockedOption("LONE_BLADE", "INITIAL", bladeTips))
                .WithRelic<AuroraPreloadMomentum4Relic>(Owner),
            (affordable
                ? Option(LoadSequence, Loc("pages.INITIAL.options.SEQUENCE.title"), Loc("pages.INITIAL.options.SEQUENCE.description"), sequenceTips)
                : LockedOption("SEQUENCE", "INITIAL", sequenceTips))
                .WithRelic<AuroraPreloadChainDrawRelic>(Owner),
            CanForget()
                ? Option(Forget, Loc("pages.INITIAL.options.FORGET.title"), Loc("pages.INITIAL.options.FORGET.description"))
                : LockedOption("FORGET"),
        };
    }

    /// <summary>
    /// 选项悬停预览。本事件的卡是从卡池随机抽 3 张再让玩家挑，无法预览具体卡面，
    /// 故改为预览「该选项走哪条流派」（剑势 / 连锁机制说明）+ 附赠的预载遗物说明。
    /// 不传 HoverTips 时选项悬停一片空白，玩家完全不知道选了会得到什么
    /// （事件选项按钮走 NEventOptionButton.OnFocus → NHoverTipSet.CreateAndShow(Option.HoverTips)）。
    /// 禁用态也照给，让玩家知道错过了什么。纯展示：不改判定、不动随机数、不影响联机。
    /// </summary>
    private static IHoverTip[] RewardTips<TRelic>(AuroraMechanic mechanic)
        where TRelic : RelicModel
        => AuroraMechanicTips.Build(new[] { mechanic })
            .Concat(HoverTipFactory.FromRelic<TRelic>())
            .ToArray();

    private async Task LoadLoneBlade()
    {
        await PayHpAsync();
        await OfferCandidateAsync(AuroraMechanic.Momentum);
        await RelicCmd.Obtain<AuroraPreloadMomentum4Relic>(Owner);
        SetEventFinished(Loc("pages.LONE_BLADE.description"));
    }

    private async Task LoadSequence()
    {
        await PayHpAsync();
        await OfferCandidateAsync(AuroraMechanic.Chain);
        await RelicCmd.Obtain<AuroraPreloadChainDrawRelic>(Owner);
        SetEventFinished(Loc("pages.SEQUENCE.description"));
    }

    private async Task Forget()
    {
        if (Owner?.Creature != null)
        {
            await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), Owner.Creature, MaxHpCost, false);
        }

        var prefs = new CardSelectorPrefs(Loc("removePrompt"), 1, 1);
        var toRemove = (await CardSelectCmd.FromDeckForRemoval(Owner, prefs)).ToList();
        if (toRemove.Count > 0)
        {
            await CardPileCmd.RemoveFromDeck(toRemove);
        }

        SetEventFinished(Loc("pages.FORGET.description"));
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

    /// <summary>从奥萝拉卡池按机制+普通/罕见抽 3 张候选（Rng 联机一致），弹「选一张」加入牌组。候选为空则跳过。</summary>
    private async Task OfferCandidateAsync(AuroraMechanic mechanic)
    {
        var pool = ModelDb.CardPool<AuroraCardPool>().AllCards
            .OfType<AuroraCard>()
            .Where(c => (c.Rarity == CardRarity.Common || c.Rarity == CardRarity.Uncommon)
                        && c.DeclaredMechanics.Contains(mechanic))
            .Cast<CardModel>()
            .ToList();

        var candidates = new List<CardModel>();
        for (var i = 0; i < CandidateCount && pool.Count > 0; i++)
        {
            var pick = Rng.NextItem(pool);
            pool.Remove(pick);
            candidates.Add(Owner.RunState.CreateCard(pick, Owner));
        }

        if (candidates.Count == 0)
        {
            return;
        }

        // 【必须是 Blocking，不能用 Throwing】ThrowingPlayerChoiceContext 的语义是「确信这条调用链绝不会
        // 发起玩家选择，发起了就抛」——它的 SignalPlayerChoiceBegun 直接 throw NotImplementedException。
        // 而 FromChooseACardScreen 开的正是玩家选择界面，第一步就调 SignalPlayerChoiceBegun → 抛异常 →
        // 打断 EventOption.Chosen 的 async 链 → 事件永久卡死（选项点了没反应）。
        // 事件在战斗外，没有别的玩家队列需要解阻塞，故用 BlockingPlayerChoiceContext；
        // 联机一致性由 FromChooseACardScreen 内部的 PlayerChoiceSynchronizer 保证，与本 context 无关。
        // 同款用法见原版 HeftyTablet 遗物：CardSelectCmd.FromChooseACardScreen(new BlockingPlayerChoiceContext(), ...)。
        // 注意：本事件其余 CreatureCmd.Damage/LoseMaxHp 不发起选择，继续用 Throwing 是对的，勿一起改。
        var chosen = await CardSelectCmd.FromChooseACardScreen(new BlockingPlayerChoiceContext(), candidates, Owner);
        if (chosen != null)
        {
            CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(chosen, PileType.Deck), 2f);
        }
    }
}
