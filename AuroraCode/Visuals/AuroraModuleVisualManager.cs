using System.Collections.Generic;
using System.Linq;
using AuroraMod.AuroraCode.Characters;
using AuroraMod.AuroraCode.Powers;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Rooms;

namespace AuroraMod.AuroraCode.Visuals;

/// <summary>
/// 奥萝拉模块纯视觉管理器：只读 <see cref="AuroraModulePower"/>，零玩法状态。
/// 重建入口幂等；Tween 不进入结算主链（由 <see cref="AuroraModuleVisualBridge"/> 火发）。
/// 挂 NCreature、不抬 ZIndex；战斗结束清理（对齐 OrbManager.ClearOrbs 纪律）。
/// </summary>
public partial class AuroraModuleVisualManager : Node2D
{
    private Creature _creature;
    private readonly Dictionary<AuroraModulePower, AuroraModuleVisual> _visuals = new();
    private bool _combatEndedHooked;

    public Creature BoundCreature => _creature;

    public void Bind(Creature creature)
    {
        _creature = creature;
        Name = AuroraModuleVisualConfig.ManagerNodeName;
        HookCombatEnded();
        RebuildFromPowers();
    }

    /// <summary>战斗初始化 / 重连 / 状态变化统一入口：先对照 Power 再增删改，不重复生成。</summary>
    public void RebuildFromPowers()
    {
        if (_creature == null || !GodotObject.IsInstanceValid(this))
        {
            return;
        }

        var modules = AuroraModulePower.All(_creature);
        var alive = new HashSet<AuroraModulePower>(modules);

        foreach (var stale in _visuals.Keys.Where(p => !alive.Contains(p)).ToList())
        {
            if (_visuals.Remove(stale, out var node) && GodotObject.IsInstanceValid(node))
            {
                node.PlayRemoveThenFree();
            }
        }

        // 每次重建按角色当前包围盒 + 当前模块总数重算头顶弧位（新增/移除后整排左右重新均分）。
        var boundsValid = TryGetLocalBounds(out var bounds);

        for (var i = 0; i < modules.Count; i++)
        {
            var module = modules[i];
            // 按 All() 全局部署顺序左→右排布（不分攻防）。
            var anchor = AuroraModuleVisualConfig.AnchorFor(bounds, boundsValid, i, modules.Count);

            if (!_visuals.TryGetValue(module, out var visual) || !GodotObject.IsInstanceValid(visual))
            {
                visual = new AuroraModuleVisual();
                _visuals[module] = visual;
                AddChild(visual);
                visual.PlaceAt(anchor);
                visual.Setup(module.Kind, module.Value, idlePhase: i * 0.55f);
            }
            else
            {
                visual.MoveToAnchorIfNeeded(anchor);

                if (visual.BoundValue != module.Value)
                {
                    visual.SetValue(module.Value);
                }
            }
        }
    }

    /// <summary>
    /// 从父 NCreature 的 Hitbox 取角色包围盒（NCreature-local 空间；管理器挂在原点、scale=1，故等同本节点局部空间）。
    /// Hitbox 未就绪/尺寸无效时返回 false，调用方退回兜底锚点。
    /// </summary>
    private bool TryGetLocalBounds(out Rect2 bounds)
    {
        bounds = default;
        if (GetParent() is not NCreature nc || nc.Hitbox == null)
        {
            return false;
        }

        var size = nc.Hitbox.Size;
        if (size.X <= 1f || size.Y <= 1f)
        {
            return false;
        }

        // Hitbox 的全局左上角换算到 NCreature 局部空间。
        var topLeftLocal = nc.Hitbox.GlobalPosition - nc.GlobalPosition;
        bounds = new Rect2(topLeftLocal, size);
        return true;
    }

    public void NotifyEnhanced(AuroraModulePower module)
    {
        if (module != null && _visuals.TryGetValue(module, out var visual) && GodotObject.IsInstanceValid(visual))
        {
            visual.SetValue(module.Value);
            visual.PlayEnhance();
        }
        else
        {
            RebuildFromPowers();
        }
    }

    /// <summary>
    /// 模块触发：抖动脉冲；攻击模块另附一条射向 <paramref name="targetGlobal"/> 的紫色光束（护盾模块传 null）。
    /// 光束起止点从全局换算到本管理器局部空间（管理器挂 NCreature 原点、scale=1）。
    /// </summary>
    public void NotifyTriggered(AuroraModulePower module, Vector2? targetGlobal)
    {
        if (module != null && _visuals.TryGetValue(module, out var visual) && GodotObject.IsInstanceValid(visual))
        {
            visual.PlayTrigger();

            if (targetGlobal.HasValue)
            {
                // 枪口朝向本次目标：以角色原点(本管理器)为基准判左右,整排模块朝向一致。
                visual.FaceTarget(targetGlobal.Value.X < GlobalPosition.X);

                var from = ToLocal(visual.GlobalPosition);
                var to = ToLocal(targetGlobal.Value);
                AuroraModuleBeam.Spawn(this, from, to);
            }
        }
    }

    /// <summary>战斗结束清视觉，避免压「搜刮!」奖励窗 / 地图残留（对齐 Orb ClearOrbs）。</summary>
    private void OnCombatEnded(CombatRoom _)
    {
        ClearVisuals();
    }

    private void ClearVisuals()
    {
        foreach (var visual in _visuals.Values)
        {
            if (GodotObject.IsInstanceValid(visual))
            {
                visual.QueueFree();
            }
        }

        _visuals.Clear();
    }

    private void HookCombatEnded()
    {
        if (_combatEndedHooked || CombatManager.Instance == null)
        {
            return;
        }

        CombatManager.Instance.CombatEnded += OnCombatEnded;
        _combatEndedHooked = true;
    }

    private void UnhookCombatEnded()
    {
        if (!_combatEndedHooked)
        {
            return;
        }

        try
        {
            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.CombatEnded -= OnCombatEnded;
            }
        }
        catch
        {
            // 退出/热重载时 Instance 可能已不可用。
        }

        _combatEndedHooked = false;
    }

    public override void _ExitTree()
    {
        UnhookCombatEnded();
        ClearVisuals();
        base._ExitTree();
    }

    public static AuroraModuleVisualManager EnsureOn(NCreature creatureNode)
    {
        if (creatureNode?.Entity == null)
        {
            return null;
        }

        if (creatureNode.Entity.Player?.Character is not Aurora)
        {
            return null;
        }

        var existing = creatureNode.GetNodeOrNull<AuroraModuleVisualManager>(AuroraModuleVisualConfig.ManagerNodeName);
        if (existing != null)
        {
            // 已绑定同一 Creature 时不要每次通知都 Rebuild（触发/强化会高频进来）。
            if (existing.BoundCreature != creatureNode.Entity)
            {
                existing.Bind(creatureNode.Entity);
            }

            return existing;
        }

        var manager = new AuroraModuleVisualManager();
        creatureNode.AddChild(manager);
        manager.Bind(creatureNode.Entity);
        return manager;
    }
}
