using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class GallerySceneBootstrap
{
    public const string ArtRoot = PlaceholderArtGenerator.ArtRoot;
    public const string DataRoot = "Assets/Resources/Paintings";
    public const string SceneDir = "Assets/Scenes/Gallery";
    public const string GalleryPath = SceneDir + "/Gallery.unity";

    const float CameraSize = 5.4f;
    const float CameraY = 2.2f;
    const float FrameY = 2.2f;
    const float FrameW = 2.4f;
    const float FrameH = 1.8f;
    const float SlotSpacing = 6f;
    const float InteractRadius = 1.2f;
    const float SidePadding = 3.2f;

    static Sprite Sp(string rel)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + "/" + rel + ".png");
    }

    static void FitToSize(Transform t, Sprite sprite, Vector2 size)
    {
        if (sprite == null) return;
        Vector2 native = sprite.rect.size / sprite.pixelsPerUnit;
        if (native.x <= 0f || native.y <= 0f) return;
        t.localScale = new Vector3(size.x / native.x, size.y / native.y, 1f);
    }

    static GameObject Quad(Transform parent, string name, Sprite sprite, Vector3 pos, Vector2 size, Color tint, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = tint;
        sr.sortingOrder = order;
        FitToSize(go.transform, sprite, size);
        return go;
    }

    static GameObject Tiled(Transform parent, string name, Vector2 center, Vector2 size, Color color, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(center.x, center.y, 0f);
        Sprite sprite = Sp("Environment/backdrop");
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = order;
        FitToSize(go.transform, sprite, size);
        return go;
    }
    public static List<string> LoadPaintingPaths()
    {
        List<string> paths = new List<string>();
        if (!AssetDatabase.IsValidFolder(DataRoot)) return paths;
        string[] guids = AssetDatabase.FindAssets("t:PaintingData", new[] { DataRoot });
        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            PaintingData d = AssetDatabase.LoadAssetAtPath<PaintingData>(assetPath);
            if (d != null) paths.Add(assetPath);
        }
        paths.Sort(delegate(string a, string b)
        {
            PaintingData da = AssetDatabase.LoadAssetAtPath<PaintingData>(a);
            PaintingData db = AssetDatabase.LoadAssetAtPath<PaintingData>(b);
            if (da == null || db == null) return string.CompareOrdinal(a, b);
            return SortPainting(da, db);
        });
        return paths;
    }

    public static List<PaintingData> LoadPaintings()
    {
        List<PaintingData> list = new List<PaintingData>();
        if (!AssetDatabase.IsValidFolder(DataRoot)) return list;
        string[] guids = AssetDatabase.FindAssets("t:PaintingData", new[] { DataRoot });
        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            PaintingData d = AssetDatabase.LoadAssetAtPath<PaintingData>(assetPath);
            if (d != null) list.Add(d);
        }
        list.Sort(SortPainting);
        return list;
    }

    static int SortPainting(PaintingData a, PaintingData b)
    {
        int byOrder = a.sortOrder.CompareTo(b.sortOrder);
        if (byOrder != 0) return byOrder;
        return string.CompareOrdinal(a.paintingId, b.paintingId);
    }

    static void EnsureFolder(string parent, string leaf)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + leaf))
            AssetDatabase.CreateFolder(parent, leaf);
    }

    static void BuildCamera(float centerX, bool follow, float minX, float maxX)
    {
        GameObject camGo = new GameObject("Main Camera");
        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = CameraSize;
        cam.transform.position = new Vector3(centerX, CameraY, -10f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.86f, 0.86f, 0.85f);
        cam.nearClipPlane = -20f;
        cam.farClipPlane = 40f;
        camGo.AddComponent<AudioListener>();

        GalleryCamera2D c2d = camGo.AddComponent<GalleryCamera2D>();
        c2d.follow = follow;
        c2d.minX = minX;
        c2d.maxX = maxX;
        c2d.fixedY = CameraY;
    }

    static void BuildBackdrop(float minX, float maxX)
    {
        float width = (maxX - minX) + 2f * SidePadding;
        float cx = (minX + maxX) * 0.5f;

        GameObject root = new GameObject("=== Environment ===");
        Tiled(root.transform, "Backdrop", new Vector2(cx, 4f), new Vector2(width, 20f), new Color(0.89f, 0.89f, 0.88f), -50);
        Tiled(root.transform, "WallBase", new Vector2(cx, 5.2f), new Vector2(width, 8f), new Color(0.93f, 0.93f, 0.92f), -40);
        Tiled(root.transform, "Floor", new Vector2(cx, -0.6f), new Vector2(width, 3.2f), new Color(0.70f, 0.68f, 0.64f), -20);

        int lines = Mathf.CeilToInt(width / 2f);
        for (int i = 0; i <= lines; i++)
        {
            float x = cx - width * 0.5f + i * 2f;
            Tiled(root.transform, "FloorLine_" + i, new Vector2(x, -0.62f), new Vector2(0.04f, 3.2f), new Color(0.55f, 0.53f, 0.50f, 0.55f), -15);
        }
    }
    static void BuildFrames(List<PaintingData> paintings)
    {
        GameObject root = new GameObject("=== Frames ===");
        Vector2 frameSize = new Vector2(FrameW, FrameH);

        for (int i = 0; i < paintings.Count; i++)
        {
            PaintingData d = paintings[i];
            float x = i * SlotSpacing;
            GameObject frameRoot = new GameObject("Frame_" + d.paintingId);
            frameRoot.transform.SetParent(root.transform, false);
            frameRoot.transform.localPosition = new Vector3(x, FrameY, 0f);

            Sprite sp = d.DisplaySprite != null ? d.DisplaySprite : Sp("Frames/frame_p1");
            GameObject canvasGo = Quad(frameRoot.transform, "Canvas", sp, Vector3.zero, frameSize, Color.white, 0);
            GameObject hi = Quad(frameRoot.transform, "Highlight", Sp("Frames/frame_highlight"), Vector3.zero,
                frameSize + new Vector2(0.16f, 0.16f), new Color(1f, 1f, 1f, 0.35f), 5);
            hi.SetActive(false);

            PaintingInteract pi = frameRoot.AddComponent<PaintingInteract>();
            pi.data = d;
            pi.radius = InteractRadius;
            pi.canvasRenderer = canvasGo.GetComponent<SpriteRenderer>();
            pi.highlight = hi.transform;

            BoxCollider2D zone = frameRoot.AddComponent<BoxCollider2D>();
            zone.isTrigger = true;
            zone.size = new Vector2(FrameW, FrameH);
        }

        float endX = paintings.Count * SlotSpacing;
        GameObject endRoot = new GameObject("Frame_ending");
        endRoot.transform.SetParent(root.transform, false);
        endRoot.transform.localPosition = new Vector3(endX, FrameY, 0f);
        Quad(endRoot.transform, "Canvas", Sp("Frames/frame_ending"), Vector3.zero, frameSize, Color.white, 0);
        GameObject endHi = Quad(endRoot.transform, "Highlight", Sp("Frames/frame_highlight"), Vector3.zero,
            frameSize + new Vector2(0.16f, 0.16f), new Color(1f, 1f, 1f, 0.35f), 5);
        endHi.SetActive(false);

        EndingWall ew = endRoot.AddComponent<EndingWall>();
        ew.highlight = endHi.transform;

        BoxCollider2D endZone = endRoot.AddComponent<BoxCollider2D>();
        endZone.isTrigger = true;
        endZone.size = new Vector2(FrameW, FrameH);
    }

    static void BuildPlayer(float minX, float maxX)
    {
        GameObject root = new GameObject("=== Player ===");
        GameObject player = new GameObject("Player");
        player.transform.SetParent(root.transform, false);
        player.transform.localPosition = new Vector3(minX + 1f, 0f, 0f);

        SpriteRenderer sr = player.AddComponent<SpriteRenderer>();
        sr.sprite = Sp("Player/player_idle");
        sr.color = Color.white;
        sr.sortingOrder = 10;

        BoxCollider2D bc = player.AddComponent<BoxCollider2D>();
        bc.size = new Vector2(0.8f, 1.8f);
        bc.isTrigger = true;

        PlayerMove2D move = player.AddComponent<PlayerMove2D>();
        move.minX = minX;
        move.maxX = maxX;
    }
    static void BuildUI()
    {
        GameObject root = new GameObject("=== UI ===");
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        HUD hud = root.AddComponent<HUD>();
        hud.promptFontSize = 34;

        GameObject fader = new GameObject("FaderCanvas", typeof(Canvas));
        fader.transform.SetParent(root.transform, false);
        Canvas fc = fader.GetComponent<Canvas>();
        fc.renderMode = RenderMode.ScreenSpaceOverlay;
        fc.sortingOrder = 500;

        GameObject img = new GameObject("Black", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        RectTransform rt = img.GetComponent<RectTransform>();
        rt.SetParent(fader.transform, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        img.GetComponent<Image>().color = Color.black;
        CanvasGroup cg = img.GetComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.blocksRaycasts = false;

        ScreenFader sf = img.AddComponent<ScreenFader>();
        sf.fadeOutDuration = 0.35f;
        sf.fadeInDuration = 0.45f;
    }

    public static void BuildGallery()
    {
        EnsureFolder("Assets/Scenes", "Gallery");

        List<PaintingData> paintings = LoadPaintings();
        float lastX = paintings.Count * SlotSpacing;
        float minX = -SidePadding;
        float maxX = lastX + SidePadding;
        float centerX = (minX + maxX) * 0.5f;

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BuildCamera(centerX, false, minX, maxX);
        BuildBackdrop(minX, maxX);
        BuildFrames(paintings);
        BuildPlayer(minX, maxX);
        BuildUI();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, GalleryPath);
        Debug.Log("[画廊] 场景已生成，画作数 = " + paintings.Count + " → " + GalleryPath);
    }
    public static void BuildPaintingScene(PaintingData data)
    {
        try { BuildPaintingSceneInternal(data); }
        catch (System.Exception e) { Debug.LogError("[画廊] 画作场景生成失败: " + e); }
    }

    static void BuildPaintingSceneInternal(PaintingData data)
    {
        if (data == null) return;
        string sceneName = data.sceneName;
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[画廊] 画作 " + data.paintingId + " 没有填 sceneName，跳过场景生成");
            return;
        }
        string path = SceneDir + "/" + sceneName + ".unity";

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject camGo = new GameObject("Main Camera");
        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = CameraSize;
        cam.transform.position = new Vector3(0f, CameraY, -10f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.94f, 0.93f, 0.90f);
        cam.nearClipPlane = -20f;
        cam.farClipPlane = 40f;
        camGo.tag = "MainCamera";
        camGo.AddComponent<AudioListener>();

        GameObject root = new GameObject("PaintingRoot");
        PaintingSceneRoot psr = root.AddComponent<PaintingSceneRoot>();
        psr.paintingId = data.paintingId;

        GameObject hint = new GameObject("Hint", typeof(Canvas), typeof(CanvasScaler));
        Canvas cs = hint.GetComponent<Canvas>();
        cs.renderMode = RenderMode.ScreenSpaceOverlay;
        cs.sortingOrder = 10;
        CanvasScaler sc = hint.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);

        GameObject txt = new GameObject("Txt", typeof(RectTransform), typeof(Text));
        RectTransform rt = txt.GetComponent<RectTransform>();
        rt.SetParent(hint.transform, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(80f, 0f);
        rt.offsetMax = new Vector2(-80f, -80f);
        Text t = txt.GetComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 32;
        t.color = new Color(0.1f, 0.1f, 0.12f);
        t.alignment = TextAnchor.UpperLeft;
        t.text = data.displayName + "\n空场景（待接入内容）\nEsc 返回画廊";

        EditorSceneManager.MarkSceneDirty(scene);
        bool saved = EditorSceneManager.SaveScene(scene, path);
        Debug.Log("[画廊] 画作场景已生成(saved=" + saved + "): " + path);
    }

    static void BindCameraToPlayer()
    {
        GalleryCamera2D c2d = Object.FindObjectOfType<GalleryCamera2D>();
        PlayerMove2D pm = Object.FindObjectOfType<PlayerMove2D>();
        if (c2d != null && pm != null) c2d.target = pm.transform;
    }

    public static void SetupBuildSettings()
    {
        List<EditorBuildSettingsScene> list = new List<EditorBuildSettingsScene>();
        list.Add(new EditorBuildSettingsScene(GalleryPath, true));

        List<PaintingData> paintings = LoadPaintings();
        for (int i = 0; i < paintings.Count; i++)
        {
            string sceneName = paintings[i].sceneName;
            if (string.IsNullOrEmpty(sceneName)) continue;
            string path = SceneDir + "/" + sceneName + ".unity";
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) == null) continue;
            list.Add(new EditorBuildSettingsScene(path, true));
        }
        EditorBuildSettings.scenes = list.ToArray();
    }

    public static void BuildAll()
    {
        PlaceholderArtGenerator.GenerateAll();
        BuildGallery();

        List<string> paths = LoadPaintingPaths();
        Debug.Log("[画廊] 待生成画作场景数 = " + paths.Count);
        for (int i = 0; i < paths.Count; i++)
        {
            PaintingData d = AssetDatabase.LoadAssetAtPath<PaintingData>(paths[i]);
            if (d == null)
            {
                Debug.LogWarning("[画廊] 画作数据无法加载，跳过: " + paths[i]);
                continue;
            }
            Debug.Log("[画廊] 生成第 " + i + " 个: " + d.paintingId + " / " + d.sceneName);
            BuildPaintingScene(d);
        }

        SetupBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!Application.isBatchMode) EditorSceneManager.OpenScene(GalleryPath, OpenSceneMode.Single);
        Debug.Log("[画廊] 全部完成");
    }

    public static void BuildAllBatch()
    {
        BuildAll();
        Debug.Log("[画廊] 批处理完成");
    }

    [MenuItem("Tools/游趣无限/0. 一键生成全部（素材 + 场景）")]
    static void MenuBuildAll() { BuildAll(); }

    [MenuItem("Tools/游趣无限/1. 只重建画廊场景")]
    static void MenuBuildGallery() { BuildGallery(); SetupBuildSettings(); AssetDatabase.SaveAssets(); }

    [MenuItem("Tools/游趣无限/2. 只重建全部画作场景")]
    static void MenuBuildPaintings()
    {
        List<string> paths = LoadPaintingPaths();
        for (int i = 0; i < paths.Count; i++)
        {
            PaintingData d = AssetDatabase.LoadAssetAtPath<PaintingData>(paths[i]);
            if (d != null) BuildPaintingScene(d);
        }
        SetupBuildSettings();
        AssetDatabase.SaveAssets();
    }
}
