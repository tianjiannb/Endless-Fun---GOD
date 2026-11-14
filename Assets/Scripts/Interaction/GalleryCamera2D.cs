using UnityEngine;

public class GalleryCamera2D : MonoBehaviour
{
    public Transform target;
    public bool follow = false;
    public float minX = -8.5f;
    public float maxX = 8.5f;
    public float fixedY = 2.2f;
    public float smooth = 8f;

    void LateUpdate()
    {
        if (!follow || target == null) return;
        float x = Mathf.Clamp(target.position.x, minX, maxX);
        Vector3 p = transform.position;
        p.x = Mathf.Lerp(p.x, x, smooth * Time.deltaTime);
        p.y = fixedY;
        transform.position = p;
    }

    public void SnapToTarget()
    {
        if (target == null) return;
        Vector3 p = transform.position;
        p.x = Mathf.Clamp(target.position.x, minX, maxX);
        p.y = fixedY;
        transform.position = p;
    }
}
