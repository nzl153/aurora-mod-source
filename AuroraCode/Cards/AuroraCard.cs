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

/// <summary>
/// Aurora 卡牌基类。绑定 AuroraCardPool。
/// 子类用 <see cref="ArtName"/> 指定 <c>Aurora/Images/Cards/&lt;name&gt;.png</c> 下的紫黑机甲卡图；
/// 缺图时按卡型回退 ironclad 占位。
/// </summary>
[Pool(typeof(AuroraCardPool))]
public abstract class AuroraCard(int cost, CardType type, CardRarity rarity, TargetType target)
    : CustomCardModel(cost, type, rarity, target)
{
    private const string CardDir = "res://Aurora/Images/Cards/";

    /// <summary>子类返回卡图文件名(不含扩展名)；返回 null 用占位。</summary>
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

    /// <summary>子类声明本卡涉及的奥萝拉自定义机制，卡面据此展示悬停说明（稳定顺序去重）。</summary>
    protected virtual IEnumerable<AuroraMechanic> MechanicTips => Array.Empty<AuroraMechanic>();

    /// <summary>公开读取本卡声明的机制（供事件按流派筛选卡池，如 E-03 终战指令库）。</summary>
    public IEnumerable<AuroraMechanic> DeclaredMechanics => MechanicTips;

    /// <summary>
    /// 攻击命中演出档位（纯表现，见 <see cref="AuroraStrikeVfx"/>）。默认 <see cref="AuroraStrikeVfxKind.Normal"/> 素净小刀光；
    /// 少数招牌终结技用 <see cref="AuroraStrikeVfxKind.Ultimate"/>，群体横扫用 <see cref="AuroraStrikeVfxKind.Flurry"/> 紫刃齐射，
    /// 单发重击用 <see cref="AuroraStrikeVfxKind.Heavy"/> 重拳顿感。由 AuroraStrikeVfxPatch 读取。过载灼热火花独立叠加、不受此档位影响。
    /// </summary>
    public virtual AuroraStrikeVfxKind StrikeVfx => AuroraStrikeVfxKind.Normal;

    /// <summary>子类声明附加的原生提示（如易伤），排在机制提示之后。</summary>
    protected virtual IEnumerable<IHoverTip> AdditionalHoverTips => Array.Empty<IHoverTip>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        AuroraMechanicTips.Build(MechanicTips, AdditionalHoverTips);
}
