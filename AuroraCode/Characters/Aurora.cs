using System.Collections.Generic;
using BaseLib.Abstracts;
using Godot;
using AuroraMod.AuroraCode.Cards.Basic;
using AuroraMod.AuroraCode.Relics;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace AuroraMod.AuroraCode.Characters;

/// <summary>
/// 奥萝拉 / Aurora —— 被退役封存、仍为一场早已结束的战争挥剑的战争兵器少女。
/// 核心机制：热量 / 过载（Heat）。四流派：过热暴走 / 蓄力一斩 / 悬浮部件 / 指令连锁。
///
/// 当前为骨架：先用占位角色（silent）的视觉与动画跑通选人/战斗；
/// 待 Spine（Aurora/Spine/aurora.*）在 Godot 里导入并生成 SpineSkeletonDataResource(.tres) 后，
/// 再覆写 CustomVisualPath / CustomCharacterSelectBg 等指向自定义场景。
/// </summary>
public class Aurora : PlaceholderCharacterModel
{
    public const string CharacterId = "Aurora";

    // 用 silent 作占位：女性、敏捷持剑，比例动画最接近奥萝拉。
    public override string PlaceholderID => "silent";

    // 幽紫（机甲主色）。
    public static readonly Color Color = new("8e44ad");

    public override Color NameColor => Color;
    public override CharacterGender Gender => CharacterGender.Feminine;
    public override int StartingHp => 72;

    // 初始卡组 11 张（88 总稿 / 定盘通知）：打击×3 防御×4 + 四单例。
    // 开机=反应炉唤醒；不再放旧点火斩。模块/扫描/剑势/协议/锁定不进起手。
    public override IEnumerable<CardModel> StartingDeck =>
    [
        // 初始 11 张：打击×4、防御×3、四张功能牌各×1。
        // 第 4 张打击是"换挡牌"而非失控加速：冷/温 +1 助进档，过载 -1 自带刹车，永不由基础打击触发过热。
        ModelDb.Card<AuroraStrike>(),
        ModelDb.Card<AuroraStrike>(),
        ModelDb.Card<AuroraStrike>(),
        ModelDb.Card<AuroraStrike>(),
        ModelDb.Card<AuroraDefend>(),
        ModelDb.Card<AuroraDefend>(),
        ModelDb.Card<AuroraDefend>(),
        ModelDb.Card<AuroraReactorWake>(),
        ModelDb.Card<AuroraTacticalConvergence>(),
        ModelDb.Card<AuroraBreachingThrust>(),
        ModelDb.Card<AuroraSidestep>(),
    ];

    public override IReadOnlyList<RelicModel> StartingRelics => [ModelDb.Relic<HeatDissipationCore>()];

    public override CardPoolModel CardPool => ModelDb.CardPool<AuroraCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<AuroraRelicPool>();
    // 专属药水池 = 共享药水 + 奥萝拉专属 2 张（AuroraPotionPool.GenerateAllPotions 返回共享池全部）。
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<AuroraPotionPool>();

    // 阶段2a：战斗视觉 = Spine（AuroraVisual.tscn 内 NCreatureVisuals 根 + SpineSprite，
    // 动画名 idle_loop/attack/hurt/die 与本体一致，由游戏战斗系统自动驱动；skill 供卡牌特效手动触发）。
    public override string CustomVisualPath => "res://Aurora/Scenes/AuroraVisual.tscn";

    // 选人页：动态呼吸立绘背景场景 + 选人小图标 + 锁定图（美术已就位，全部启用）。
    public override string CustomCharacterSelectBg => "res://Aurora/Scenes/CharSelect/AuroraCharSelectBg.tscn";
    public override string CustomCharacterSelectIconPath => "res://Aurora/Images/Charui/char_select_aurora_icon.png";
    public override string CustomCharacterSelectLockedIconPath => "res://Aurora/Images/Charui/char_select_aurora_locked.png";

    // 顶栏角色图标 + 地图标记。
    public override string CustomIconTexturePath => "res://Aurora/Images/Charui/character_icon_aurora.png";
    public override string CustomMapMarkerPath => "res://Aurora/Images/Map/aurora_marker.png";

    // 选人→战斗的入场过场：紫黑机甲阈值展开材质（由 BaseLib Harmony 补丁接进原生 CharacterSelectTransitionPath；
    // 非空即用本材质，否则会落到缺失的 aurora_transition_mat.tres 兜底成女猎手撕裂）。
    public override string CustomCharacterSelectTransitionPath => "res://Aurora/Materials/Transitions/aurora_transition_mat.tres";

    // 过场擦除音效：强制成可识别的 wipe_aurora（默认会经 silent 占位解析成女猎手擦除音）。
    // AuroraAudioPatches 命中此字符串时改播 transition.wav 并跳过原生调用（不再响女猎手音）。
    public override string CharacterTransitionSfx => "event:/sfx/ui/wipe_aurora";

    /// <summary>
    /// 基类把 Cast/PowerUp 映到 AnimState("cast")，但奥萝拉 Spine 只有 skill。
    /// 其余触发与基类一致；不改 Spine 文件。
    /// </summary>
    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        var idle = new AnimState("idle_loop", isLooping: true);
        var skill = new AnimState("skill");
        var attack = new AnimState("attack");
        var hurt = new AnimState("hurt");
        var die = new AnimState("die");
        // Spine 只有 idle_loop/attack/hurt/die/skill，没有独立的 relaxed_loop。
        // 之前点名播不存在的 relaxed_loop → 商店/地图落到静态 setup 姿势（不呼吸、站位偏高）。
        // 复用 idle_loop（带眨眼+呼吸），商店即有呼吸，且站姿高度与战斗一致。
        var relaxed = new AnimState("idle_loop", isLooping: true);

        skill.NextState = idle;
        attack.NextState = idle;
        hurt.NextState = idle;
        relaxed.AddBranch("Idle", idle);

        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Idle", idle);
        animator.AddAnyState("Dead", die);
        animator.AddAnyState("Hit", hurt);
        animator.AddAnyState("Attack", attack);
        animator.AddAnyState("Cast", skill);
        animator.AddAnyState("PowerUp", skill);
        animator.AddAnyState("Relaxed", relaxed);
        return animator;
    }

    public override List<string> GetArchitectAttackVfx()
    {
        return
        [
            "vfx/vfx_attack_slash",
            "vfx/vfx_bloody_impact",
            "vfx/vfx_heavy_blunt"
        ];
    }
}
