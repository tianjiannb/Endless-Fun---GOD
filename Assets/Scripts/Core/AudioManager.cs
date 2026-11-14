using System.Collections;
using UnityEngine;

/// <summary>
/// 全局音频播放：音乐与音效内容全部来自 AudioLibrary 数据资源，
/// 这里只负责 AudioSource 和播放。游戏启动时自动创建，无需摆进场景。
/// 音乐可以有多个 id，切换时做交叉淡入淡出。
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Tooltip("留空时自动加载 Resources/AudioLibrary")]
    [SerializeField] AudioLibrary library;
    [Tooltip("切换音乐时的交叉淡入淡出时长（秒）")]
    [SerializeField] float musicFade = 0.6f;

    AudioSource _sfx;
    AudioSource _music;
    AudioSource _musicAlt;
    Coroutine _musicFade;
    string _musicId;
    float _sfxScale = 1f;
    float _musicScale = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap() => Ensure();

    public static AudioManager Ensure()
    {
        if (Instance != null) return Instance;
        return new GameObject("AudioManager").AddComponent<AudioManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (library == null) library = Resources.Load<AudioLibrary>("AudioLibrary");

        _sfx = gameObject.AddComponent<AudioSource>();
        _sfx.playOnAwake = false;
        _sfx.spatialBlend = 0f;

        _music = gameObject.AddComponent<AudioSource>();
        _music.playOnAwake = false;
        _music.loop = true;
        _music.spatialBlend = 0f;

        _musicAlt = gameObject.AddComponent<AudioSource>();
        _musicAlt.playOnAwake = false;
        _musicAlt.loop = true;
        _musicAlt.spatialBlend = 0f;

        // 默认播放列表里的第一首，作为开场音乐。
        if (library != null && library.music.Length > 0) PlayMusic(library.music[0].id);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void PlaySfx(string id, float volumeScale = 1f)
    {
        AudioEntry entry = library != null ? library.FindSfx(id) : null;
        if (entry == null || entry.clip == null || _sfx == null) return;
        _sfx.PlayOneShot(entry.clip, Mathf.Clamp01(entry.volume * volumeScale) * library.sfxVolume * _sfxScale);
    }

    /// <summary>播放指定 id 的音乐；同一首正在播放时不会重头开始。</summary>
    public void PlayMusic(string id, bool restart = false)
    {
        if (library == null || _music == null) return;
        AudioEntry entry = library.FindMusic(id);
        if (entry == null || entry.clip == null) return;
        if (_musicId == id && _music.isPlaying && !restart) return;

        if (_musicFade != null) StopCoroutine(_musicFade);

        if (!_music.isPlaying || restart || _music.clip == null)
        {
            _music.clip = entry.clip;
            _music.volume = MusicVolume(entry);
            _music.Play();
            _musicId = id;
            return;
        }

        _musicFade = StartCoroutine(Crossfade(entry));
    }

    IEnumerator Crossfade(AudioEntry entry)
    {
        AudioSource from = _music;
        AudioSource to = _musicAlt;
        _music = to;
        _musicAlt = from;

        to.clip = entry.clip;
        to.volume = 0f;
        to.Play();
        _musicId = entry.id;

        float startVolume = from.volume;
        float target = MusicVolume(entry);
        float t = 0f;
        while (t < musicFade)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / musicFade);
            to.volume = target * k;
            from.volume = startVolume * (1f - k);
            yield return null;
        }

        to.volume = target;
        from.Stop();
        from.clip = null;
        from.volume = 0f;
        _musicFade = null;
    }

    public void StopMusic()
    {
        if (_musicFade != null) { StopCoroutine(_musicFade); _musicFade = null; }
        _musicId = null;
        Stop(_music);
        Stop(_musicAlt);
    }

    static void Stop(AudioSource source)
    {
        if (source == null) return;
        source.Stop();
        source.clip = null;
        source.volume = 0f;
    }

    public void SetSfxVolume(float value) => _sfxScale = Mathf.Clamp01(value);

    public void SetMusicVolume(float value)
    {
        _musicScale = Mathf.Clamp01(value);
        if (_musicFade != null || library == null) return;
        AudioEntry entry = library.FindMusic(_musicId);
        if (entry != null) _music.volume = MusicVolume(entry);
    }

    float MusicVolume(AudioEntry entry) =>
        library == null ? 0f : library.musicVolume * _musicScale * Mathf.Clamp01(entry.volume);
}
