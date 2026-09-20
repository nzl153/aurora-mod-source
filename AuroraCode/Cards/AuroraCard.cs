using System;
using System.Collections.Generic;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using AuroraMod.AuroraCode.Characters;
using AuroraMod.AuroraCode.Helpers;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace AuroraMod.AuroraCode.Cards;

/// <summary>Aurora 卡牌基类：绑定卡池、统一卡图回退与机制提示。</summary>
[Pool(typeof(AuroraCardPool))]
public abstract class AuroraCard(int cost, CardType type, CardRarity rarity, TargetType target)
    : CustomCardModel(cost, type, rarity, target)
{
    private const string CardDir = "res://Aurora/Images/Cards/";

    protected virtual string ArtName => null;

    private string Fallback => Type switch
    {
        CardType.Attack => ImageHelper.GetImagePath("packed/card_portraits/ironclad/strike_ironclad.png"),
        _ => ImageHelper.GetImagePath("packed/card_portraits/ironclad/defend_ironclad.png")
    };

    public override string PortraitPath =>
        ArtName != null && ResourceLoader.Exists($"{CardDir}{ArtName}.png")
            ? $"{CardDir}{ArtName}.png"
            : Fallback;

    public override string CustomPortraitPath => PortraitPath;
    public override string BetaPortraitPath => PortraitPath;

    /// <summary>本卡涉及的自定义机制，用于悬停说明和事件筛选。</summary>
    protected virtual IEnumerable<AuroraMechanic> MechanicTips => Array.Empty<AuroraMechanic>();

    public IEnumerable<AuroraMechanic> DeclaredMechanics => MechanicTips;

    /// <summary>攻击命中特效档位。</summary>
    public virtual AuroraStrikeVfxKind StrikeVfx => AuroraStrikeVfxKind.Normal;

    /// <summary>附加的原生悬停提示，例如易伤。</summary>
    protected virtual IEnumerable<IHoverTip> AdditionalHoverTips => Array.Empty<IHoverTip>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        AuroraMechanicTips.Build(MechanicTips, AdditionalHoverTips);
}
