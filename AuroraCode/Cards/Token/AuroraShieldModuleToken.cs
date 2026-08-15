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
/// 护盾模块令牌 / Shield Module Token —— 满槽替换选择界面的<b>只读候选卡</b>（架构 §6.1 / §12）。
/// 与 <see cref="AuroraAttackModuleToken"/> 同：不进牌堆/图鉴、不可打出、不可被生成；<c>Value</c> 反映对应模块当前生效值。
/// 必须挂 <c>[Pool]</c>（BaseLib 硬要求），挂原生 <see cref="TokenCardPool"/>。
/// 卡框由 <c>AuroraCardFramePatch</c> 按类型识别。
/// </summary>
[Pool(typeof(TokenCardPool))]
public class AuroraShieldModuleToken()
    : CustomCardModel(0, CardType.Skill, CardRarity.Token, TargetType.Self, showInCardLibrary: false)
{
    private const string ArtPath = "res://Aurora/Images/Cards/shield_module_token.png";
    private static readonly string Fallback =
        ImageHelper.GetImagePath("packed/card_portraits/ironclad/defend_ironclad.png");

    protected override bool IsPlayable => false;
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;

    private static string ResolvePortrait() =>
        ResourceLoader.Exists(ArtPath) ? ArtPath : Fallback;

    public override string PortraitPath => ResolvePortrait();
    public override string CustomPortraitPath => ResolvePortrait();
    public override string BetaPortraitPath => ResolvePortrait();

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Value", 5m)];

    // Token 未继承 AuroraCard，直接接入同一悬停注册表（护盾模块）。
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        AuroraMechanicTips.Build([AuroraMechanic.ShieldModule]);

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Task.CompletedTask;
}
