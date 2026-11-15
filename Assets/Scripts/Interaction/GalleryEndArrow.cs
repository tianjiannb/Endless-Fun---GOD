using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 画廊尽头的向右箭头：画廊里的画作全部完成后出现，
/// 玩家继续走到最右侧即进入结算场景。
/// 箭头贴图在运行时生成，不依赖美术资源。
/// </summary>
public class GalleryEndArrow : MonoBehaviour
{
    [Header("触发")]
    [Tooltip("玩家 x 达到该值即进入结算")]
    public float triggerX = 8.6f;
    [Tooltip("走到该位置开始提示继续向右")]
    public float hintX = 6.6f;
    public string endingScene = "Ending";

    [Header("外观")]
    public Color color = new Color(1f, 0.93f, 0.76f, 0.9f);
    public float scale = 0.6f;
    public float bobAmplitude = 0.05f;
    public float bobPeriod = 2.4f;

    SpriteRenderer _renderer;
    Vector3 _basePosition;
    float _time;
    bool _active;
    bool _leaving;

    void Start()
    {
        _basePosition = transform.position;
        EnsureRenderer();
        _active = AllPaintingsDone();
        _renderer.enabled = _active;
        transform.localScale = Vector3.one * scale;
    }

    void Update()
    {
        if (!_active || _leaving) return;

        _time += Time.deltaTime;
        transform.position = _basePosition + Vector3.up * (Mathf.Sin(_time * Mathf.PI * 2f / bobPeriod) * bobAmplitude);
        float pulse = 0.72f + Mathf.Sin(_time * 2.4f) * 0.22f;
        _renderer.color = new Color(color.r, color.g, color.b, color.a * pulse);

        PlayerMove2D player = PlayerMove2D.Instance;
        if (player == null) return;

        float x = player.transform.position.x;
        if (x >= triggerX)
        {
            StartCoroutine(GoToEnding());
            return;
        }

        if (x >= hintX) HUD.ShowRawPrompt("继续向右", "gallery_end");
        else HUD.HideRawPrompt("gallery_end");
    }

    IEnumerator GoToEnding()
    {
        _leaving = true;
        HUD.HideRawPrompt("gallery_end");
        if (AudioManager.Instance != null) AudioManager.Instance.StopMusic();
        if (ScreenFader.Instance != null)
        {
            ScreenFader.Instance.StartCoroutine(ScreenFader.Instance.FadeAndLoad(endingScene));
            yield break;
        }
        SceneManager.LoadScene(endingScene);
    }

    static bool AllPaintingsDone()
    {
        GameSession session = GameSession.Instance;
        if (session == null) return false;

        PaintingInteract[] paintings = FindObjectsOfType<PaintingInteract>();
        int considered = 0;
        for (int i = 0; i < paintings.Length; i++)
        {
            PaintingInteract painting = paintings[i];
            if (painting == null || painting.Data == null || !painting.Data.interactable) continue;
            considered++;
            if (!session.IsCompleted(painting.Data.paintingId)) return false;
        }
        return considered > 0;
    }

    void EnsureRenderer()
    {
        _renderer = GetComponent<SpriteRenderer>();
        if (_renderer == null) _renderer = gameObject.AddComponent<SpriteRenderer>();
        if (_renderer.sprite == null) _renderer.sprite = MakeArrowSprite();
        _renderer.color = color;
        _renderer.sortingOrder = 30;
    }

    static Sprite MakeArrowSprite()
    {
        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float cover = 0f;
                for (int sy = 0; sy < 4; sy++)
                {
                    for (int sx = 0; sx < 4; sx++)
                    {
                        float px = x + (sx + 0.5f) / 4f;
                        float py = y + (sy + 0.5f) / 4f;
                        if (InsideArrow(px, py, size)) cover += 1f;
                    }
                }
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, cover / 16f));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    static bool InsideArrow(float x, float y, int size)
    {
        float center = size * 0.5f;
        float dx = x - center;
        float dy = Mathf.Abs(y - center);
        if (dx >= -46f && dx <= 6f && dy <= 9f) return true;
        if (dx >= -6f && dx <= 46f && dy <= (46f - dx) * 0.95f) return true;
        return false;
    }
}
