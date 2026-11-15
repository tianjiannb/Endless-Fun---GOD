using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// 打开相机的后处理，并注入一个只含 Bloom 的全局 Volume。
/// 相机的 HDR 已开启，粒子材质亮度超过 1 的自发光部分会泛光。
/// 运行时可在 Hierarchy 里选中 BloomVolume 实时调参。
/// </summary>
public static class BloomPostFX
{
    static VolumeProfile _profile;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initialize()
    {
        Apply();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Apply();
    }

    static void Apply()
    {
        Camera cam = Camera.main;
        if (cam == null) cam = Object.FindObjectOfType<Camera>();
        if (cam == null) return;

        // 没有粒子的场景不开泛光，避免普通画面被 Bloom 洗掉
        if (Object.FindObjectOfType<ParticleSystemRenderer>() == null) return;

        UniversalAdditionalCameraData data = cam.GetUniversalAdditionalCameraData();
        data.renderPostProcessing = true;

        // 场景里已经有 Volume 时不再重复创建
        if (Object.FindObjectOfType<Volume>() != null) return;

        GameObject go = new GameObject("BloomVolume");
        Volume volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 1f;
        volume.profile = Profile;
    }

    static VolumeProfile Profile
    {
        get
        {
            if (_profile == null)
            {
                _profile = ScriptableObject.CreateInstance<VolumeProfile>();
                _profile.hideFlags = HideFlags.DontSave;

                Bloom bloom = _profile.Add<Bloom>(true);
                bloom.threshold.Override(1f);
                bloom.intensity.Override(0.9f);
                bloom.scatter.Override(0.7f);
                bloom.highQualityFiltering.Override(true);
                bloom.tint.Override(new Color(1f, 0.97f, 0.92f));
            }
            return _profile;
        }
    }
}
