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

    AudioSource[] _sfxSources;
    int _sfxCursor;
    AudioSource _slideSource;
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

        // 音效走小播放池，避免同时触发时互相改音高/截断
        _sfxSources = new AudioSource[6];
        for (int i = 0; i < _sfxSources.Length; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            _sfxSources[i] = source;
        }

        // 滑行音效单独一路：循环播放，滑行结束时立刻停
        _slideSource = gameObject.AddComponent<AudioSource>();
        _slideSource.playOnAwake = false;
        _slideSource.loop = true;
        _slideSource.spatialBlend = 0f;

        _music = gameObject.AddComponent<AudioSource>();
        _music.playOnAwake = false;
        _music.loop = true;
        _music.spatialBlend = 0f;

        _musicAlt = gameObject.AddComponent<AudioSource>();
        _musicAlt.playOnAwake = false;
        _musicAlt.loop = true;
        _musicAlt.spatialBlend = 0f;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void PlaySfx(string id, float volumeScale = 1f, float pitch = 1f)
    {
        AudioClip clip = ResolveSfx(id, out float entryVolume);
        if (clip == null || _sfxSources == null || _sfxSources.Length == 0) return;

        AudioSource source = _sfxSources[_sfxCursor];
        _sfxCursor = (_sfxCursor + 1) % _sfxSources.Length;
        source.pitch = pitch * (1f + Random.Range(-0.04f, 0.04f));

        float libraryVolume = library != null ? library.sfxVolume : 1f;
        source.PlayOneShot(clip, Mathf.Clamp01(entryVolume * volumeScale) * libraryVolume * _sfxScale);
    }

    /// <summary>滑行开始：循环播放滑动音效，直到 <see cref="StopSlide"/>。</summary>
    public void PlaySlide()
    {
        AudioClip clip = ResolveSfx(AudioId.Slide, out float entryVolume);
        if (clip == null || _slideSource == null) return;

        float libraryVolume = library != null ? library.sfxVolume : 1f;
        _slideSource.clip = clip;
        _slideSource.volume = Mathf.Clamp01(entryVolume) * libraryVolume * _sfxScale;
        _slideSource.pitch = 1f + Random.Range(-0.04f, 0.04f);
        if (!_slideSource.isPlaying) _slideSource.Play();
    }

    /// <summary>滑行停止：立刻停掉滑动音效。</summary>
    public void StopSlide()
    {
        if (_slideSource != null && _slideSource.isPlaying) _slideSource.Stop();
    }

    /// <summary>先查 AudioLibrary，缺失时回退到 Resources/SFX/&lt;id&gt;。</summary>
    AudioClip ResolveSfx(string id, out float volume)
    {
        AudioEntry entry = library != null ? library.FindSfx(id) : null;
        volume = entry != null ? entry.volume : 1f;
        AudioClip clip = entry != null ? entry.clip : null;
        return clip != null ? clip : Resources.Load<AudioClip>("SFX/" + id);
    }

    /// <summary>
    /// 给无法直接引用本程序集的模块用的入口（如 SplitSlide 通过 SendMessage 调用）。
    /// 门开关沿用同一个素材，用音高区分。
    /// </summary>
    public void PlayCue(string id)
    {
        switch (id)
        {
            case "slide": PlaySlide(); break;
            case "slide_stop": StopSlide(); break;
            case "blocked": PlaySfx(AudioId.Blocked, 0.9f); break;
            case "door_open": PlaySfx(AudioId.Door, 1f, 1.15f); break;
            case "door_close": PlaySfx(AudioId.Door, 0.8f, 0.85f); break;
            case "music_puzzle": PlayMusic(AudioId.MusicPuzzle); break;
            case "music_gallery": PlayMusic(AudioId.MusicGallery); break;
            case "music_stop": StopMusic(); break;
            default: PlaySfx(id); break;
        }
    }

    /// <summary>播放指定 id 的音乐；同一首正在播放时不会重头开始。</summary>
    public void PlayMusic(string id, bool restart = false)
    {
        if (_music == null) return;
        AudioEntry entry = library != null ? library.FindMusic(id) : null;
        AudioClip clip = entry != null ? entry.clip : null;
        // 数据资源里没配时，回退到 Resources/Music/<id>.wav
        if (clip == null) clip = Resources.Load<AudioClip>("Music/" + id);
        if (clip == null) return;
        if (_musicId == id && _music.isPlaying && !restart) return;

        if (_musicFade != null) StopCoroutine(_musicFade);

        float target = MusicVolume(entry);
        if (!_music.isPlaying || restart || _music.clip == null)
        {
            _music.clip = clip;
            _musicId = id;
            _music.volume = 0f;
            _music.Play();
            _musicFade = StartCoroutine(FadeTo(_music, target));
            return;
        }

        _musicId = id;
        _musicFade = StartCoroutine(Crossfade(clip, target));
    }

    IEnumerator FadeTo(AudioSource source, float target)
    {
        float from = source.volume;
        float t = 0f;
        while (t < musicFade)
        {
            t += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(from, target, Mathf.Clamp01(t / musicFade));
            yield return null;
        }
        source.volume = target;
        _musicFade = null;
    }

    IEnumerator Crossfade(AudioClip clip, float target)
    {
        AudioSource from = _music;
        AudioSource to = _musicAlt;
        _music = to;
        _musicAlt = from;

        to.clip = clip;
        to.volume = 0f;
        to.Play();

        float startVolume = from.volume;
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
        bool aPlaying = _music != null && _music.isPlaying;
        bool bPlaying = _musicAlt != null && _musicAlt.isPlaying;
        if (!aPlaying && !bPlaying)
        {
            Stop(_music);
            Stop(_musicAlt);
            return;
        }
        _musicFade = StartCoroutine(FadeOut());
    }

    IEnumerator FadeOut()
    {
        float a = _music != null ? _music.volume : 0f;
        float b = _musicAlt != null ? _musicAlt.volume : 0f;
        float t = 0f;
        while (t < musicFade)
        {
            t += Time.unscaledDeltaTime;
            float k = 1f - Mathf.Clamp01(t / musicFade);
            if (_music != null) _music.volume = a * k;
            if (_musicAlt != null) _musicAlt.volume = b * k;
            yield return null;
        }
        Stop(_music);
        Stop(_musicAlt);
        _musicFade = null;
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
        if (_musicFade != null) return;
        AudioEntry entry = library != null ? library.FindMusic(_musicId) : null;
        if (_music != null && _music.clip != null) _music.volume = MusicVolume(entry);
    }

    float MusicVolume(AudioEntry entry)
    {
        float libraryVolume = library != null ? library.musicVolume : 0.6f;
        float entryVolume = entry != null ? Mathf.Clamp01(entry.volume) : 1f;
        return libraryVolume * _musicScale * entryVolume;
    }
}
