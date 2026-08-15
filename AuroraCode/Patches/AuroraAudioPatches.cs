using System;
using AuroraMod.AuroraCode.Helpers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace AuroraMod.AuroraCode.Patches;

/// <summary>
/// 奥萝拉自定义音效的两个界面接线（纯表现层）：
/// ① 入场过场擦除音：游戏在 embark 时 SfxCmd.Play(character.CharacterTransitionSfx)，奥萝拉的
///    "event:/sfx/ui/wipe_aurora" 事件不存在（FMOD 静默）；命中该字符串时补播我们的 transition.wav，
///    时机与原生过场擦除完全一致。
/// ② 选人语音：NCharacterSelectButton.Select 在（重新）选中时触发，命中奥萝拉时播 select_voice.wav
///    （PlayVoice 独占，来回选人不叠音）。
/// 均不改玩法/RNG/联机，只补声音。
/// </summary>
[HarmonyPatch]
public static class AuroraAudioPatches
{
    // ① 过场擦除音：命中奥萝拉的 wipe 事件字符串时改播 transition.wav，并跳过原生调用
    //    （否则会响 silent 占位的女猎手擦除音）。返回 false = 拦下原生 SfxCmd.Play。
    [HarmonyPatch(typeof(SfxCmd), nameof(SfxCmd.Play), new[] { typeof(string), typeof(float) })]
    [HarmonyPrefix]
    public static bool SfxPlayPrefix(string sfx)
    {
        try
        {
            if (!string.IsNullOrEmpty(sfx)
                && sfx.Contains("wipe", StringComparison.OrdinalIgnoreCase)
                && sfx.Contains("aurora", StringComparison.OrdinalIgnoreCase))
            {
                AuroraAudio.PlaySfx("transition.wav");
                return false;   // 拦下原生，避免响原版擦除音。
            }
        }
        catch
        {
            // 纯表现：异常不拦原生。
        }

        return true;
    }

    // ② 选人语音：选中奥萝拉时播一句（独占，防叠音）。
    [HarmonyPatch(typeof(NCharacterSelectButton), nameof(NCharacterSelectButton.Select))]
    [HarmonyPostfix]
    public static void SelectPostfix(NCharacterSelectButton __instance)
    {
        try
        {
            var entry = __instance?.Character?.Id.Entry;
            if (!string.IsNullOrWhiteSpace(entry)
                && entry.Contains("aurora", StringComparison.OrdinalIgnoreCase))
            {
                AuroraAudio.PlayVoice("select_voice.wav");
            }
        }
        catch
        {
            // 纯表现。
        }
    }
}
