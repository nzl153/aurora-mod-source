using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;
using AuroraMod.AuroraCode.Helpers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace AuroraMod.AuroraCode.Cards.Token;

/// <summary>
/// 攻击模块令牌 / Attack Module Token —— 满槽替换选择界面的<b>只读候选卡</b>（架构 §6.1 / §12）。
/// 不进牌堆、不进图鉴、不可打出、不可被生成；<c>Value</c> 由控制器置为对应模块的当前生效值以便玩家区分。
/// 必须挂 <c>[Pool]</c>（BaseLib CustomCardModel 构造硬要求），挂原生 <see cref="TokenCardPool"/>；
/// Token 稀有度 + 禁止生成，确保永不出现在奖励里。
/// 卡框由 <c>AuroraCardFramePatch</c> 按类型识别（池是 TokenCardPool，不含 aurora）。
/// </summary>
[Pool(typeof(TokenCardPool))]
public class AuroraAttackModuleToken()
    : CustomCardModel(0, CardType.Skill, CardRarity.Token, TargetType.Self, showInCardLibrary: false)
{
    private const string ArtPath = "res://Aurora/Images/Cards/attack_module_token.png";
    private static readonly string Fallback =
        ImageHelper.GetImagePath("packed/card_portraits/ironclad/strike_ironclad.png");

    protected override bool IsPlayable => false;
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;

    private static string ResolvePortrait() =>
        ResourceLoader.Exists(ArtPath) ? ArtPath : Fallback;

    public override string PortraitPath => ResolvePortrait();
    public override string CustomPortraitPath => ResolvePortrait();
    public override string BetaPortraitPath => ResolvePortrait();

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Value", 4m)];

    // Token 未继承 AuroraCard，直接接入同一悬停注册表（攻击模块 + 锁定）。
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        AuroraMechanicTips.Build([AuroraMechanic.AttackModule, AuroraMechanic.Lock]);

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Task.CompletedTask;
}
