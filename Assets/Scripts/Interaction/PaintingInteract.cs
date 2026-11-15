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

    static Material _outlineMaterial;
    static Material OutlineMaterial
    {
        get
        {
            if (_outlineMaterial == null)
                _outlineMaterial = Resources.Load<Material>("Materials/PaintingOutline");
            return _outlineMaterial;
        }
    }

    public PaintingData Data { get { return data; } }
    public bool PlayerInside { get { return _inside; } }

    void Start()
    {
        ApplyData();
        FitHighlight();
        if (highlight != null) highlight.gameObject.SetActive(false);
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
        if (Application.isPlaying) FitHighlight();
    }

    void FitHighlight()
    {
        if (highlight == null) return;

        Transform visual = transform.Find("Canvas");
        SpriteRenderer target = visual != null ? visual.GetComponent<SpriteRenderer>() : null;
        if (target == null) target = canvasRenderer;

        SpriteRenderer glow = highlight.GetComponent<SpriteRenderer>();
        if (target == null || target.sprite == null || glow == null) return;

        highlight.SetParent(target.transform, false);
        highlight.localPosition = Vector3.zero;
        highlight.localRotation = Quaternion.identity;
        highlight.localScale = Vector3.one;

        glow.sprite = target.sprite;
        glow.color = Color.white;
        glow.sortingLayerID = target.sortingLayerID;
        glow.sortingOrder = target.sortingOrder - 1;

        Material outline = OutlineMaterial;
        if (outline != null) glow.sharedMaterial = outline;
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
        SpriteRenderer glow = highlight.GetComponent<SpriteRenderer>();
        if (glow == null) return;

        float k = 0.72f + 0.28f * (0.5f + 0.5f * Mathf.Sin(Time.time * 3f));
        glow.color = OutlineMaterial != null
            ? new Color(1f, 1f, 1f, k)
            : new Color(1f, 0.95f, 0.68f, 0.4f * k);
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
