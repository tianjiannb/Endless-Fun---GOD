using System;
using UnityEngine;

[Serializable]
public sealed class AudioEntry
{
    [Tooltip("代码里用的 id，例如 slide / blocked / door / split / win / gallery / puzzle")]
    public string id;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
}

/// <summary>代码里使用的音频 id，与 AudioLibrary 里填写的一致。</summary>
public static class AudioId
{
    public const string Slide = "slide";
    public const string Blocked = "blocked";
    public const string Door = "door";
    public const string Split = "split";
    public const string Win = "win";

    public const string MusicGallery = "gallery";
    public const string MusicPuzzle = "puzzle";
    public const string MusicSnow = "snow";
}

/// <summary>
/// 所有音频数据：多首音乐、音效、音量。放在 Resources/AudioLibrary.asset，
/// 加音乐或音效只需在资源里新增一条 id + clip，不用改代码。
/// </summary>
[CreateAssetMenu(menuName = "Numen/Audio Library", fileName = "AudioLibrary")]
public sealed class AudioLibrary : ScriptableObject
{
    [Header("Music")]
    [Tooltip("可以放多首；AudioManager.PlayMusic(id) 指定播放哪一首")]
    public AudioEntry[] music = Array.Empty<AudioEntry>();
    [Range(0f, 1f)] public float musicVolume = 0.6f;

    [Header("SFX")]
    [Range(0f, 1f)] public float sfxVolume = 1f;
    public AudioEntry[] sfx = Array.Empty<AudioEntry>();

    public AudioEntry FindMusic(string id) => FindIn(music, id);
    public AudioEntry FindSfx(string id) => FindIn(sfx, id);

    static AudioEntry FindIn(AudioEntry[] list, string id)
    {
        if (list == null || string.IsNullOrEmpty(id)) return null;
        for (int i = 0; i < list.Length; i++)
            if (list[i] != null && list[i].id == id) return list[i];
        return null;
    }
}
