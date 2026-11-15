using UnityEngine;

/// <summary>
/// 幽灵的漂浮表现：上下浮动 + 轻微呼吸缩放。
/// 只改 y 和 localScale，不影响 PlayerMove2D 的水平移动与翻转。
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class GhostFloat : MonoBehaviour
{
    [Header("漂浮")]
    public float amplitude = 0.08f;
    public float period = 3.2f;

    [Header("呼吸缩放")]
    [Range(0f, 0.1f)] public float stretch = 0.03f;

    float _baseY;
    Vector3 _baseScale;
    float _time;

    void Awake()
    {
        _baseY = transform.position.y;
        _baseScale = transform.localScale;
    }

    void LateUpdate()
    {
        _time += Time.deltaTime;
        float phase = _time * Mathf.PI * 2f / Mathf.Max(0.01f, period);

        // 主漂浮，叠一点二段频率，避免升降过于机械
        float bob = Mathf.Sin(phase) * amplitude
                  + Mathf.Sin(phase * 1.7f + 0.6f) * amplitude * 0.18f;

        // 上升时拉长、下沉时压扁，横向反向做一点体积补偿
        float breathing = Mathf.Cos(phase);
        float sy = 1f + breathing * stretch;
        float sx = 1f - breathing * stretch * 0.6f;

        Vector3 pos = transform.position;
        pos.y = _baseY + bob;
        transform.position = pos;

        transform.localScale = new Vector3(_baseScale.x * sx, _baseScale.y * sy, _baseScale.z);
    }
}
