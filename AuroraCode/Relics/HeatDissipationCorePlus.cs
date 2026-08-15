using MegaCrit.Sts2.Core.Entities.Relics;

namespace AuroraMod.AuroraCode.Relics;

/// <summary>
/// 强化散热核心 / Reinforced Heat Core —— 「先古之民」把起始遗物替换为升级版时获得（架构 §4.5）。
/// 继承相同触发时机 / 选敌 / 内部初始化，只增强三档数值：冷 6 格挡 / 温 3 格挡+3 伤 / 过载 6 伤。
/// </summary>
public sealed class HeatDissipationCorePlus : HeatDissipationCore
{
    public override RelicRarity Rarity => RelicRarity.Ancient;
    protected override string ArtName => "heat_core_plus";

    protected override int ColdBlock => 6;
    protected override int WarmBlock => 3;
    protected override int WarmDamage => 3;
    protected override int OverloadDamage => 6;
}
