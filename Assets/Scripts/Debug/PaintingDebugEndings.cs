using UnityEngine;

/// <summary>
/// 可选调试件：挂在画作场景的任意物体上，按数字键直接产出结局。
/// 默认不加入场景，保持画作流程只有「进入接口 / 退出接口」两件事。
/// 要测结局墙时手动 Add Component 即可。
/// </summary>
public class PaintingDebugEndings : MonoBehaviour
{
    public KeyCode endingAKey = KeyCode.Alpha1;
    public KeyCode endingBKey = KeyCode.Alpha2;

    void Update()
    {
        PaintingSceneRoot root = FindObjectOfType<PaintingSceneRoot>();
        if (root == null) return;

        if (Input.GetKeyDown(endingAKey)) root.ExitWith("ending_a");
        else if (Input.GetKeyDown(endingBKey)) root.ExitWith("ending_b");
    }
}
