using System.Collections.Generic;
using Godot;

namespace AuroraMod.AuroraCode.Helpers;

/// <summary>
/// 奥萝拉自定义音效播放（纯表现层）。用 Godot 原生 AudioStreamPlayer 播放打包进 pck 的普通音频文件，
/// 绕开 FMOD 事件那套（同 Merchant2CuteII 等 mod 的做法）。
///
/// 每次一次性播放：新建 AudioStreamPlayer 挂到场景树根，Finished 后自 QueueFree；流按 path 静态缓存。
/// 绝不影响玩法/伤害/RNG/联机——纯声音。异常一律吞掉，不中断游戏。
/// </summary>
public static class AuroraAudio
{
    private const string Dir = "res://Aurora/Audio/";

    private static readonly Dictionary<string, AudioStream> Cache = new();

    // 语音独占：正在播的语音实例，避免来回选人叠音。
    private static AudioStreamPlayer _voicePlayer;

    private static AudioStream Load(string name)
    {
        try
        {
            var path = Dir + name;
            if (Cache.TryGetValue(path, out var cached))
            {
                return cached;
            }

            var stream = ResourceLoader.Exists(path) ? GD.Load<AudioStream>(path) : null;
            Cache[path] = stream;   // null 也缓存，避免反复 Exists。
            return stream;
        }
        catch
        {
            return null;
        }
    }

    private static SceneTree Tree => Engine.GetMainLoop() as SceneTree;

    private static AudioStreamPlayer Spawn(AudioStream stream, float volumeDb)
    {
        var root = Tree?.Root;
        if (root == null)
        {
            return null;
        }

        var player = new AudioStreamPlayer
        {
            Stream = stream,
            VolumeDb = volumeDb,
        };
        root.AddChild(player);
        player.Finished += () =>
        {
            if (GodotObject.IsInstanceValid(player))
            {
                player.QueueFree();
            }
        };
        player.Play();
        return player;
    }

    /// <summary>一次性播放一个音效文件（如 "attack_hit.wav"）；缺失/失败静默跳过。</summary>
    public static void PlaySfx(string name, float volumeDb = 0f)
    {
        try
        {
            var stream = Load(name);
            if (stream != null)
            {
                Spawn(stream, volumeDb);
            }
        }
        catch
        {
            // 纯表现：绝不因它中断游戏。
        }
    }

    /// <summary>播放语音，若上一句仍在播则跳过（避免叠音）。</summary>
    public static void PlayVoice(string name, float volumeDb = 0f)
    {
        try
        {
            if (_voicePlayer != null && GodotObject.IsInstanceValid(_voicePlayer) && _voicePlayer.Playing)
            {
                return;
            }

            var stream = Load(name);
            if (stream != null)
            {
                _voicePlayer = Spawn(stream, volumeDb);
            }
        }
        catch
        {
            // 纯表现。
        }
    }
}
