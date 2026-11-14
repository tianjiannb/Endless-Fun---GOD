using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerMove2D : MonoBehaviour
{
    public static PlayerMove2D Instance { get; private set; }

    [Header("速度")]
    public float maxSpeed = 4.2f;
    public float acceleration = 40f;
    public float deceleration = 55f;

    [Header("边界")]
    public float minX = -8.5f;
    public float maxX = 8.5f;

    [Header("按键")]
    public KeyCode leftKey = KeyCode.A;
    public KeyCode rightKey = KeyCode.D;

    [Header("表现")]
    public bool flipSprite = true;

    float _vx;
    SpriteRenderer _sr;

    public float CurrentSpeed { get { return _vx; } }

    void Awake()
    {
        Instance = this;
        _sr = GetComponent<SpriteRenderer>();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        float input = 0f;
        if (Input.GetKey(leftKey)) input -= 1f;
        if (Input.GetKey(rightKey)) input += 1f;

        float target = input * maxSpeed;
        float rate = Mathf.Abs(target) > 0.001f ? acceleration : deceleration;
        _vx = Mathf.MoveTowards(_vx, target, rate * Time.deltaTime);

        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x + _vx * Time.deltaTime, minX, maxX);
        transform.position = pos;

        if (flipSprite && _sr != null && Mathf.Abs(_vx) > 0.05f) _sr.flipX = _vx < 0f;
    }
}
