using BaseLib.Abstracts;
using Godot;

namespace AuroraMod.AuroraCode.Characters;

public class AuroraRelicPool : CustomRelicPoolModel
{
    public override string EnergyColorName => "aurora";
    public override Color LabOutlineColor => Aurora.Color;
}
