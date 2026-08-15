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
/// 积热令牌 / Gain Heat Token —— #25 微调二选一界面的<b>只读候选卡</b>（“积 1 热”一侧）。
/// 不进牌堆、不进图鉴、不可打出、不可被生成；仅由 <see cref="AuroraHeatChoiceHelper"/> 现建现用、按索引同步。
/// 必须挂 <c>[Pool]</c>（BaseLib CustomCardModel 构造硬要求），挂原生 <see cref="TokenCardPool"/>。
/// </summary>
[Pool(typeof(TokenCardPool))]
public class AuroraGainHeatToken()
    : CustomCardModel(0, CardType.Skill, CardRarity.Token, TargetType.Self, showInCardLibrary: false)
{
    private const string ArtPath = "res://Aurora/Images/Cards/gain_heat_token.png";
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

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        AuroraMechanicTips.Build([AuroraMechanic.Heat]);

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Task.CompletedTask;
}
