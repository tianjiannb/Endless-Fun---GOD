using UnityEngine;

public class PaintingInteract : MonoBehaviour
{
    [Header("数据")]
    public PaintingData data;

    [Header("判定")]
    public float radius = 1.2f;
    public KeyCode interactKey = KeyCode.E;

    [Header("表现")]
    public SpriteRenderer canvasRenderer;
    public Transform highlight;

    bool _inside;
    bool _entered;
    Vector3 _highlightBaseScale = Vector3.one;

    public PaintingData Data { get { return data; } }
    public bool PlayerInside { get { return _inside; } }

    void Start()
    {
        if (highlight != null)
        {
            _highlightBaseScale = highlight.localScale;
            highlight.gameObject.SetActive(false);
        }
        ApplyData();
    }

    public void ApplyData()
    {
        if (data == null) return;
        if (canvasRenderer == null) canvasRenderer = GetComponent<SpriteRenderer>();
        if (canvasRenderer != null)
        {
            Sprite sp = data.DisplaySprite;
            if (sp != null) canvasRenderer.sprite = sp;
        }
    }

    void Update()
    {
        bool inside = IsPlayerNear();

        if (inside != _inside)
        {
            _inside = inside;
            if (highlight != null) highlight.gameObject.SetActive(inside);
            if (inside) HUD.ShowPaintingPrompt(data);
            else HUD.HidePaintingPrompt(data);
        }

        if (_inside && !_entered && Input.GetKeyDown(interactKey))
        {
            _entered = true;
            TryEnter();
        }
        else if (_entered && !_inside)
        {
            _entered = false;
        }

        PulseHighlight();
    }

    bool IsPlayerNear()
    {
        PlayerMove2D player = PlayerMove2D.Instance;
        if (player == null) return false;
        return Mathf.Abs(player.transform.position.x - transform.position.x) <= radius;
    }

    void PulseHighlight()
    {
        if (highlight == null || !highlight.gameObject.activeSelf) return;
        float k = 1f + Mathf.Sin(Time.time * 3f) * 0.012f;
        highlight.localScale = _highlightBaseScale * k;
    }

    public void TryEnter()
    {
        PaintingEntry.Enter(data);
    }

    void OnValidate()
    {
        if (Application.isPlaying) ApplyData();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
