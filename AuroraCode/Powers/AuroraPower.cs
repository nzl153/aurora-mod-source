using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Helpers;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// Aurora 全部自定义 Power 的基类。
/// 子类用 <see cref="IconName"/> 指定 <c>Aurora/Images/Powers/&lt;name&gt;.png</c> 下的紫黑机甲 buff 图；
/// 缺图时回退游戏内置 flame_barrier(火/热主题)占位，保证永不崩。
/// </summary>
public abstract class AuroraPower : CustomPowerModel
{
    private const string PowerDir = "res://Aurora/Images/Powers/";

    /// <summary>子类返回图标文件名(不含扩展名)；返回 null 表示暂无专属图，用占位。</summary>
    protected virtual string IconName => null;

    public override string CustomPackedIconPath =>
        IconName != null && ResourceLoader.Exists($"{PowerDir}{IconName}.png")
            ? $"{PowerDir}{IconName}.png"
            : ImageHelper.GetImagePath("atlases/power_atlas.sprites/flame_barrier_power.tres");

    public override string CustomBigIconPath =>
        IconName != null && ResourceLoader.Exists($"{PowerDir}Big/{IconName}.png")
            ? $"{PowerDir}Big/{IconName}.png"
            : ImageHelper.GetImagePath("powers/flame_barrier_power.png");
}
