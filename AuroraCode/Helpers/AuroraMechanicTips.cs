using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;

namespace AuroraMod.AuroraCode.Helpers;

/// <summary>
/// 奥萝拉自定义机制的卡面悬停提示 ID（架构：文本可读性）。
/// 每个 ID 对应 powers.json 中的一组 <c>AURORAMOD-TOOLTIP-&lt;KEY&gt;.title/.description</c>。
/// enum 的声明顺序即卡面悬停的稳定展示顺序（Heat → SystemCrash → ModuleCapacity →
/// AttackModule → ShieldModule → ModuleEnhancement → Lock → ChallengeProtocol → Momentum → Chain →
/// Scan → ZoneChange），原生提示排在其后。
/// </summary>
public enum AuroraMechanic
{
    Heat,
    SystemCrash,
    ModuleCapacity,
    AttackModule,
    ShieldModule,
    ModuleEnhancement,
    Lock,
    ChallengeProtocol,
    Momentum,
    Chain,
    Scan,
    ZoneChange,
}

/// <summary>
/// 卡面自定义机制悬停注册表（纯表现层）。卡牌只声明需要的 <see cref="AuroraMechanic"/> ID，本类按需
/// 用现有 powers.json 的 TOOLTIP loc 构造 <see cref="IHoverTip"/>。
///
/// 关键：<b>每次调用重新构造</b>——<see cref="HoverTip"/> 在构造时即读取当前语言并格式化，故绝不静态缓存
/// 已格式化文本（否则 PCK 未加载时得空文本、切换语言后仍显旧语言）。提示 Id 由稳定 loc key 决定，供去重。
///
/// 本流程为确定性文本展示：不读战斗随机数、不改 Power/Card 状态、不消费锁定、不生成卡牌、不写联机同步、
/// 不依赖预览伤害、不创建任何临时玩法状态。
/// </summary>
internal static class AuroraMechanicTips
{
    private const string Table = "powers";

    // 声明顺序 = 稳定展示顺序。
    private static readonly AuroraMechanic[] Order = (AuroraMechanic[])Enum.GetValues(typeof(AuroraMechanic));

    private static string Key(AuroraMechanic mechanic) => mechanic switch
    {
        AuroraMechanic.Heat => "HEAT",
        AuroraMechanic.SystemCrash => "SYSTEM_CRASH",
        AuroraMechanic.ModuleCapacity => "MODULE_CAPACITY",
        AuroraMechanic.AttackModule => "ATTACK_MODULE",
        AuroraMechanic.ShieldModule => "SHIELD_MODULE",
        AuroraMechanic.ModuleEnhancement => "MODULE_ENHANCEMENT",
        AuroraMechanic.Lock => "LOCK",
        AuroraMechanic.ChallengeProtocol => "CHALLENGE_PROTOCOL",
        AuroraMechanic.Momentum => "MOMENTUM",
        AuroraMechanic.Chain => "CHAIN",
        AuroraMechanic.Scan => "SCAN",
        AuroraMechanic.ZoneChange => "ZONE_CHANGE",
        _ => null,
    };

    // 机制 → 图标资源路径（表现层）。可复用现有 Power 图；三个概念项（模块强化/扫描/换区）
    // 无对应 Power，用 Images/Mechanics/ 下独立图标，避免同卡出现两个相同图标造成误读。
    private static string IconPath(AuroraMechanic mechanic) => mechanic switch
    {
        AuroraMechanic.Heat => "res://Aurora/Images/Powers/heat.png",
        AuroraMechanic.SystemCrash => "res://Aurora/Images/Powers/system_crash.png",
        AuroraMechanic.ModuleCapacity => "res://Aurora/Images/Powers/auxiliary_hardpoint.png",
        AuroraMechanic.AttackModule => "res://Aurora/Images/Powers/attack_module.png",
        AuroraMechanic.ShieldModule => "res://Aurora/Images/Powers/shield_module.png",
        AuroraMechanic.Lock => "res://Aurora/Images/Powers/lock.png",
        AuroraMechanic.ChallengeProtocol => "res://Aurora/Images/Powers/challenge_protocol.png",
        AuroraMechanic.Momentum => "res://Aurora/Images/Powers/momentum.png",
        AuroraMechanic.Chain => "res://Aurora/Images/Powers/chain.png",
        AuroraMechanic.ModuleEnhancement => "res://Aurora/Images/Mechanics/module_enhancement.png",
        AuroraMechanic.Scan => "res://Aurora/Images/Mechanics/scan.png",
        AuroraMechanic.ZoneChange => "res://Aurora/Images/Mechanics/zone_change.png",
        _ => null,
    };

    // 按 path 静态缓存纹理（与语言无关，故可长驻；钉死引用同一性、免去悬停高频 Exists/Load 抖动）。
    // 仅缓存 Texture2D，绝不缓存带 LocString 文本的 HoverTip（那会导致语言切换后显旧文）。
    private static readonly Dictionary<string, Texture2D> IconCache = new();

    /// <summary>按需加载图标纹理（带静态缓存）；不存在或加载失败返回 null（HoverTip 退化为无图标，绝不报错）。</summary>
    private static Texture2D LoadIcon(AuroraMechanic mechanic)
    {
        try
        {
            var path = IconPath(mechanic);
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            if (IconCache.TryGetValue(path, out var cached))
            {
                return cached;
            }

            var tex = ResourceLoader.Exists(path) ? ResourceLoader.Load<Texture2D>(path) : null;
            IconCache[path] = tex;   // null 也缓存，避免反复对缺失路径 Exists。
            return tex;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>按需为单个机制构造悬停提示（读当前语言 + 加载图标；均不静态缓存）。</summary>
    private static IHoverTip BuildTip(AuroraMechanic mechanic)
    {
        var key = Key(mechanic);
        var title = new LocString(Table, $"AURORAMOD-TOOLTIP-{key}.title");
        var description = new LocString(Table, $"AURORAMOD-TOOLTIP-{key}.description");
        return new HoverTip(title, description, LoadIcon(mechanic));
    }

    /// <summary>
    /// 合并机制提示与附加原生提示，按稳定顺序去重后返回（机制在前、原生在后）。
    /// 供 <c>ExtraHoverTips</c> 直接返回；每次访问重建，语言切换即时生效。
    /// </summary>
    public static IEnumerable<IHoverTip> Build(
        IEnumerable<AuroraMechanic> mechanics,
        IEnumerable<IHoverTip> additional = null)
    {
        var wanted = mechanics != null
            ? new HashSet<AuroraMechanic>(mechanics)
            : new HashSet<AuroraMechanic>();

        var list = new List<IHoverTip>();
        foreach (var mechanic in Order)
        {
            if (wanted.Contains(mechanic))
            {
                list.Add(BuildTip(mechanic));
            }
        }

        if (additional != null)
        {
            list.AddRange(additional);
        }

        return list;
    }
}
