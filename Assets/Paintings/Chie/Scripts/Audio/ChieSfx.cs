using System.Collections;
using UnityEngine;

namespace Numen
{
    /// <summary>
    /// 一把音频总线：背景底噪 + 三种音效。资源缺失时整体静默降级，不影响流程。
    /// Resources 路径见 ChieSfx.ClipRoot。
    /// </summary>
    public class ChieSfx : MonoBehaviour
    {
        private const string ClipRoot = "Chie/Audio/";

        private AudioSource _bgm;
        private AudioSource _sfx;

        private AudioClip _cBrush;
        private AudioClip _cRestore;
        private AudioClip _cBreak;

        private bool _muted;
        private bool _bgmPlaying;
        private bool _fading;
        private bool _warnedBrush;
        private bool _warnedRestore;
        private bool _warnedBreak;
        private bool _warnedBgm;
        private float _lastBrush = -1f;

        /// <summary>节流：连续笔迹事件的最小间隔（秒）。</summary>
        private const float BrushGap = 0.055f;

        public static ChieSfx Build(Transform parent)
        {
            var go = new GameObject("ChieSfx");
            go.transform.SetParent(parent, false);
            var sfx = go.AddComponent<ChieSfx>();
            sfx.Load();
            return sfx;
        }

        private void Load()
        {
            _cBrush = Resources.Load<AudioClip>(ClipRoot + "sfx_brush");
            _cRestore = Resources.Load<AudioClip>(ClipRoot + "sfx_restore");
            _cBreak = Resources.Load<AudioClip>(ClipRoot + "sfx_break");
            AudioClip bgm = Resources.Load<AudioClip>(ClipRoot + "bgm_room");

            _bgm = gameObject.AddComponent<AudioSource>();
            _bgm.playOnAwake = false;
            _bgm.loop = true;
            _bgm.spatialBlend = 0f;
            _bgm.volume = 0f;
            _bgm.clip = bgm;

            _sfx = gameObject.AddComponent<AudioSource>();
            _sfx.playOnAwake = false;
            _sfx.loop = false;
            _sfx.spatialBlend = 0f;

            // 四个资源全缺 = 这一版没有声音，静默跑
            _muted = _cBrush == null && _cRestore == null && _cBreak == null && bgm == null;
            if (_muted) Debug.LogWarning("[Chie] 音频资源缺失，静音继续跑");
        }

        /// <summary>起底噪（0.4 秒缓入到 BgmVolume）。</summary>
        public void PlayBgm()
        {
            if (_muted || _bgm == null) return;
            if (_bgm.clip == null)
            {
                Warn(ref _warnedBgm, "bgm_room");
                return;
            }
            if (_bgmPlaying) return;
            _bgmPlaying = true;
            _fading = false;
            _bgm.Play();
            StartCoroutine(CoFade(0f, BgmVolume, 0.4f));
        }

        /// <summary>淡出并停底噪。</summary>
        public void StopBgm(float fadeSeconds = 0.6f)
        {
            if (_bgm == null || !_bgmPlaying) return;
            _bgmPlaying = false;
            StopCoroutine(nameof(CoFade));
            StartCoroutine(CoFade(_bgm.volume, 0f, Mathf.Max(0.05f, fadeSeconds), true));
        }

        /// <summary>一笔落下时的摩擦声（内部节流）。</summary>
        public void Brush()
        {
            if (_muted || _sfx == null) return;
            if (_cBrush == null)
            {
                Warn(ref _warnedBrush, "sfx_brush");
                return;
            }
            float now = Time.unscaledTime;
            if (_lastBrush >= 0f && now - _lastBrush < BrushGap) return;
            _lastBrush = now;
            _sfx.pitch = Random.Range(0.92f, 1.08f);
            _sfx.PlayOneShot(_cBrush, Random.Range(0.5f, 0.75f));
        }

        /// <summary>一处破绽长好。</summary>
        public void Restore()
        {
            if (_muted || _sfx == null) return;
            if (_cRestore == null)
            {
                Warn(ref _warnedRestore, "sfx_restore");
                return;
            }
            _sfx.pitch = 1f;
            _sfx.PlayOneShot(_cRestore, 0.55f);
        }

        /// <summary>结局碎裂。</summary>
        public void Break()
        {
            if (_muted || _sfx == null) return;
            if (_cBreak == null)
            {
                Warn(ref _warnedBreak, "sfx_break");
                return;
            }
            _sfx.pitch = 1f;
            _sfx.PlayOneShot(_cBreak, 0.7f);
        }

        private const float BgmVolume = 0.18f;

        private IEnumerator CoFade(float from, float to, float seconds, bool stopAtEnd = false)
        {
            if (_fading) yield break;
            _fading = true;
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                _bgm.volume = Mathf.Lerp(from, to, Mathf.Clamp01(t / seconds));
                yield return null;
            }
            _bgm.volume = to;
            if (stopAtEnd && _bgm != null)
            {
                _bgm.Stop();
                _bgm.volume = 0f;
            }
            _fading = false;
        }

        private static void Warn(ref bool flag, string name)
        {
            if (flag) return;
            flag = true;
            Debug.LogWarning("[Chie] 缺少音频：" + name);
        }
    }
}
