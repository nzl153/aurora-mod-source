using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;
using AuroraMod.AuroraCode.Characters;
using MegaCrit.Sts2.Core.Helpers;

namespace AuroraMod.AuroraCode.Relics;

/// <summary>
/// Aurora 遗物基类，绑定 AuroraRelicPool。子类用 <see cref="ArtName"/> 指定
/// <c>Aurora/Images/Relics/&lt;name&gt;.png</c>（大图放 Big/ 子目录）；缺图回退游戏内置 burning_blood。
/// </summary>
[Pool(typeof(AuroraRelicPool))]
public abstract class AuroraRelic : CustomRelicModel
{
    private const string RelicDir = "res://Aurora/Images/Relics/";

    /// <summary>子类返回图标文件名(不含扩展名)。</summary>
    protected virtual string ArtName => null;

    private static readonly string Fallback = ImageHelper.GetImagePath("relics/burning_blood.png");

    public override string PackedIconPath =>
        ArtName != null && ResourceLoader.Exists($"{RelicDir}{ArtName}.png")
            ? $"{RelicDir}{ArtName}.png"
            : Fallback;

    protected override string PackedIconOutlinePath => PackedIconPath;

    protected override string BigIconPath =>
        ArtName != null && ResourceLoader.Exists($"{RelicDir}Big/{ArtName}.png")
            ? $"{RelicDir}Big/{ArtName}.png"
            : Fallback;
}
