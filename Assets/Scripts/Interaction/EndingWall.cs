using UnityEngine;

public class EndingWall : MonoBehaviour
{
    [Header("表现")]
    public Transform highlight;

    void Start()
    {
        if (highlight != null) highlight.gameObject.SetActive(false);
    }
}
