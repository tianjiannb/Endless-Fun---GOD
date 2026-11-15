using System;
using System.Collections;
using UnityEngine;

/// <summary>在现有分层画廊上运行透视镜头，结束或中断后原样归还游戏摄像机。</summary>
[DisallowMultipleComponent]
public sealed class GalleryOpeningTour : MonoBehaviour
{
    [Serializable]
    public class Shot
    {
        public string label;
        [Tooltip("画廊整张画布的归一化坐标，左下为 (0,0)。")]
        public Vector2 focus;
        [Min(0.3f)] public float halfHeight = 2f;
        [Min(0.1f)] public float travel = 2f;
        [Min(0f)] public float linger = 0.7f;
        public Vector2 angle;
        public bool ghost;
    }

    public Camera galleryCamera;
    public SpriteRenderer backdrop;
    public Transform ghost;
    [Range(25f, 65f)] public float fieldOfView = 42f;
    [Min(0.1f)] public float returnDuration = 2.4f;
    public Shot[] shots =
    {
        new Shot { label = "左上 · 莲花特写", focus = new Vector2(.127f,.677f), halfHeight = 2.85f, travel = 2f, linger = 1.6f, angle = new Vector2(-4f,1.5f) },
        new Shot { label = "左侧 · 浮空城堡", focus = new Vector2(.273f,.503f), halfHeight = 1.85f, travel = 1.8f, angle = new Vector2(-2f,1f) },
        new Shot { label = "左下 · 暗夜城堡", focus = new Vector2(.175f,.2f), halfHeight = 1.65f, travel = 1.8f, angle = new Vector2(-3f,-1.5f) },
        new Shot { label = "中央 · 雪夜特写", focus = new Vector2(.501f,.478f), halfHeight = 4f, travel = 2.8f, linger = 2f, angle = new Vector2(2.5f,1f) },
        new Shot { label = "右上 · 双幅城堡", focus = new Vector2(.854f,.746f), halfHeight = 2.45f, travel = 2.5f, angle = new Vector2(4f,1.5f) },
        new Shot { label = "右侧 · 星空城堡", focus = new Vector2(.707f,.418f), halfHeight = 2f, travel = 1.8f, angle = new Vector2(2f,-1f) },
        new Shot { label = "右下 · 海边城堡", focus = new Vector2(.84f,.201f), halfHeight = 1.7f, travel = 1.8f, angle = new Vector2(3f,-1.5f) },
        new Shot { label = "归来 · 幽灵主角", halfHeight = 1.6f, travel = 2.7f, linger = 1.3f, angle = new Vector2(-2f,-1f), ghost = true }
    };

    public bool IsPlaying { get; private set; }
    public int CurrentShot { get; private set; } = -1;
    public bool SkipRequested { get; private set; }
    bool _captured;
    bool _orthographic;
    float _size, _fov, _near, _far;
    Vector3 _position;
    Quaternion _rotation;
    Bounds _bounds;
    GalleryCamera2D _follow;
    bool _followEnabled;

    public void Prepare()
    {
        if (!isActiveAndEnabled || _captured || galleryCamera == null || backdrop == null) return;
        _captured = true;
        _orthographic = galleryCamera.orthographic;
        _size = galleryCamera.orthographicSize;
        _fov = galleryCamera.fieldOfView;
        _near = galleryCamera.nearClipPlane;
        _far = galleryCamera.farClipPlane;
        _position = galleryCamera.transform.position;
        _rotation = galleryCamera.transform.rotation;
        _bounds = backdrop.bounds;
        _follow = galleryCamera.GetComponent<GalleryCamera2D>();
        if (_follow != null) { _followEnabled = _follow.enabled; _follow.enabled = false; }
        galleryCamera.orthographic = false;
        galleryCamera.fieldOfView = fieldOfView;
        galleryCamera.nearClipPlane = 0.05f;
        galleryCamera.farClipPlane = Mathf.Max(100f, _far);
        SkipRequested = false;
        if (shots != null && shots.Length > 0 && shots[0] != null)
            ApplyPose(Focus(shots[0]) + Vector3.right * .35f, shots[0].halfHeight * 1.12f, shots[0].angle);
    }

    public void RequestSkip() { if (IsPlaying) SkipRequested = true; }

    public IEnumerator Play()
    {
        Prepare();
        if (!_captured) yield break;
        IsPlaying = true;
        // 从左上揭幕。每个停留段仍有缓慢推进和侧移，避免变成静态幻灯片。
        for (int i = 0; _captured && isActiveAndEnabled && shots != null && i < shots.Length && !SkipRequested; i++)
        {
            Shot shot = shots[i];
            if (shot == null) continue;
            CurrentShot = i;
            Vector3 from = PlaneCenter();
            float height = VisibleHalfHeight();
            Vector2 fromAngle = CameraAngle();
            yield return Move(from, Focus(shot), height, shot.halfHeight, fromAngle, shot.angle, shot.travel);
            if (!SkipRequested)
                yield return Move(Focus(shot), Focus(shot) + Vector3.right * .07f,
                    shot.halfHeight, shot.halfHeight * .98f, shot.angle, shot.angle + new Vector2(.6f,0f), shot.linger);
        }

        // 从幽灵缓缓拉回可玩全景，再切回完全相同构图的原正交投影。
        if (!_captured) yield break;
        CurrentShot = shots != null ? shots.Length : 0;
        Vector3 center = _position + _rotation * Vector3.forward * ((_bounds.center.z - _position.z) / (_rotation * Vector3.forward).z);
        float overview = _orthographic ? _size : Mathf.Abs(_position.z - _bounds.center.z) * Mathf.Tan(_fov * Mathf.Deg2Rad * .5f);
        bool skipped = SkipRequested;
        SkipRequested = false;
        yield return Move(PlaneCenter(), center, VisibleHalfHeight(), overview, CameraAngle(), Vector2.zero,
            skipped ? .65f : returnDuration);
        Restore();
    }

    Vector3 Focus(Shot shot)
    {
        if (shot.ghost && ghost != null)
        {
            SpriteRenderer renderer = ghost.GetComponent<SpriteRenderer>();
            Vector3 center = renderer != null ? renderer.bounds.center : ghost.position;
            return new Vector3(center.x,center.y,_bounds.center.z);
        }
        return new Vector3(Mathf.Lerp(_bounds.min.x,_bounds.max.x,shot.focus.x),
            Mathf.Lerp(_bounds.min.y,_bounds.max.y,shot.focus.y), _bounds.center.z);
    }

    IEnumerator Move(Vector3 from, Vector3 to, float fromHeight, float toHeight, Vector2 fromAngle, Vector2 toAngle, float duration)
    {
        float elapsed = 0f;
        while (_captured && isActiveAndEnabled && elapsed < duration && !SkipRequested)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(.01f,duration));
            t = t * t * t * (t * (t * 6f - 15f) + 10f);
            ApplyPose(Vector3.Lerp(from,to,t), Mathf.Lerp(fromHeight,toHeight,t), Vector2.Lerp(fromAngle,toAngle,t));
            yield return null;
        }
    }

    void ApplyPose(Vector3 focus, float halfHeight, Vector2 angle)
    {
        Quaternion rotation = Quaternion.Euler(angle.y, angle.x, 0f);
        float distance = halfHeight / Mathf.Tan(galleryCamera.fieldOfView * Mathf.Deg2Rad * .5f);
        Transform cameraTransform = galleryCamera.transform;
        // 用屏幕四角射线检查背景覆盖，宽屏、侧转和靠近画布边缘时都不露出底色。
        for (int i = 0; i < 12; i++)
        {
            cameraTransform.SetPositionAndRotation(focus - rotation * Vector3.forward * distance,rotation);
            Bounds view = PlaneViewBounds();
            if (view.size.x > _bounds.size.x || view.size.y > _bounds.size.y)
            {
                distance *= Mathf.Min(_bounds.size.x / view.size.x, _bounds.size.y / view.size.y) * .999f;
                continue;
            }
            Vector3 correction = new Vector3(
                Mathf.Max(0f,_bounds.min.x-view.min.x) + Mathf.Min(0f,_bounds.max.x-view.max.x),
                Mathf.Max(0f,_bounds.min.y-view.min.y) + Mathf.Min(0f,_bounds.max.y-view.max.y),0f);
            cameraTransform.position += correction;
            break;
        }
    }

    Bounds PlaneViewBounds()
    {
        Bounds bounds = new Bounds(PlanePoint(new Vector2(0f,0f)),Vector3.zero);
        bounds.Encapsulate(PlanePoint(new Vector2(0f,1f)));
        bounds.Encapsulate(PlanePoint(new Vector2(1f,0f)));
        bounds.Encapsulate(PlanePoint(new Vector2(1f,1f)));
        return bounds;
    }

    Vector3 PlanePoint(Vector2 viewport)
    {
        Ray ray = galleryCamera.ViewportPointToRay(viewport);
        return ray.GetPoint((_bounds.center.z - ray.origin.z) / ray.direction.z);
    }

    Vector3 PlaneCenter() { return PlanePoint(new Vector2(.5f,.5f)); }
    float VisibleHalfHeight() { return Vector3.Distance(galleryCamera.transform.position,PlaneCenter()) * Mathf.Tan(galleryCamera.fieldOfView * Mathf.Deg2Rad * .5f); }
    Vector2 CameraAngle()
    {
        Vector3 euler = galleryCamera.transform.eulerAngles;
        return new Vector2(Mathf.DeltaAngle(0f,euler.y),Mathf.DeltaAngle(0f,euler.x));
    }

    public void Restore()
    {
        if (!_captured) return;
        if (galleryCamera != null)
        {
            galleryCamera.transform.SetPositionAndRotation(_position,_rotation);
            galleryCamera.orthographic = _orthographic;
            galleryCamera.orthographicSize = _size;
            galleryCamera.fieldOfView = _fov;
            galleryCamera.nearClipPlane = _near;
            galleryCamera.farClipPlane = _far;
        }
        if (_follow != null) _follow.enabled = _followEnabled;
        _captured = false;
        IsPlaying = false;
    }

    void OnDisable() { StopAllCoroutines(); Restore(); }
}
