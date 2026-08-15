using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Helpers;

namespace AuroraMod.AuroraCode.Characters;

public class AuroraCardPool : CustomCardPoolModel
{
    public override string Title => Aurora.CharacterId;

    public override string EnergyColorName => "aurora";

    // 紫水晶能量图标(PIL 生成的切面宝石,扣水晶剑主题)。
    public override string BigEnergyIconPath => "res://Aurora/Images/sprite_fonts/aurora_energy_icon.png";
    public override string TextEnergyIconPath => "res://Aurora/Images/sprite_fonts/aurora_energy_icon.png";

    // 紫色卡框色调（HSV）。
    public override float H => 0.77f;
    public override float S => 0.55f;
    public override float V => 0.85f;

    public override Color DeckEntryCardColor => new("8e44ad");
    public override Color EnergyOutlineColor => new("4A235A");
    public override bool IsColorless => false;
}
