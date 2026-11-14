using UnityEngine;

[CreateAssetMenu(fileName = "PaintingData", menuName = "游趣无限/PaintingData")]
public class PaintingData : ScriptableObject
{
    [Header("身份")]
    public string paintingId = "p1";
    public string displayName = "未命名画作";
    [Tooltip("画廊排列顺序，小的在左")]
    public int sortOrder = 0;

    [Header("内容")]
    [TextArea(2, 6)] public string teaser = "";
    public Sprite thumbnail;
    public Sprite thumbnailLocked;

    [Header("流程")]
    [Tooltip("画作场景名，必须在 Build Settings 里")]
    public string sceneName = "";
    public bool interactable = true;

    [Header("白模占位")]
    public Sprite placeholderSprite;

    public bool IsPlayable
    {
        get { return interactable && !string.IsNullOrEmpty(sceneName); }
    }

    public Sprite DisplaySprite
    {
        get
        {
            if (!interactable && thumbnailLocked != null) return thumbnailLocked;
            if (thumbnail != null) return thumbnail;
            return placeholderSprite;
        }
    }
}
