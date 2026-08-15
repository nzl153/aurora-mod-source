using HarmonyLib;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;

namespace AuroraMod.AuroraCode.Patches;

/// <summary>
/// 奥萝拉 skill（部署/技能）动画播放太慢，这里在它被设为当前动画那一刻把 track 时间缩放提到 1.25 倍。
/// 只匹配 <c>state.Id == "skill"</c>——原版角色用 "cast"，故不影响任何其他角色/动画；
/// 纯表现层，失败静默不打断动画状态机。skill 之后排队的 idle 不受影响（各自的 track，本 patch 只碰当前）。
/// </summary>
[HarmonyPatch(typeof(CreatureAnimator), "SetNextState")]
public static class AuroraSkillSpeedPatch
{
    /// <summary>skill 动画目标倍速（实机嫌快/慢改这里）。</summary>
    public const float SkillTimeScale = 1.5f;

    public static void Postfix(CreatureAnimator __instance, AnimState state)
    {
        try
        {
            if (state == null || state.Id != "skill")
            {
                return;
            }

            var controller = Traverse.Create(__instance).Field("_spineController").GetValue();
            if (controller == null)
            {
                return;
            }

            var animState = Traverse.Create(controller).Method("GetAnimationState").GetValue<MegaAnimationState>();
            var track = animState?.GetCurrent(0);
            track?.SetTimeScale(SkillTimeScale);
        }
        catch
        {
            // 表现层：设速失败不得影响动画播放本身。
        }
    }
}
