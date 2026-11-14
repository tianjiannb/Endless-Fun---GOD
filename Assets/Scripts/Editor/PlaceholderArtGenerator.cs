using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 白模素材生成器：纯色带描边方块，不依赖外部图片。
/// 菜单：Tools / 游趣无限 / 1. 重建白模素材
/// </summary>
public static class PlaceholderArtGenerator
{
    public const string ArtRoot = "Assets/Art/Placeholder";
    public const int PPU = 100;

    static readonly Color White = new Color(1f, 1f, 1f, 1f);
    static readonly Color Grey = new Color(0.62f, 0.62f, 0.66f, 1f);
    static readonly Color Dark = new Color(0.18f, 0.18f, 0.20f, 1f);
    static readonly Color Slate = new Color(0.30f, 0.32f, 0.36f, 1f);
    static readonly Color Paper = new Color(0.94f, 0.94f, 0.92f, 1f);
    static readonly Color Sand = new Color(0.86f, 0.84f, 0.78f, 1f);
    static readonly Color Clay = new Color(0.72f, 0.66f, 0.60f, 1f);

    static void EnsureFolder(string assetPath)
    {
        string full = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
        if (!Directory.Exists(full)) Directory.CreateDirectory(full);
    }

    static Texture2D MakeTexture(int w, int h, Color fill, Color border, int borderPx, TextureWrapMode wrap)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = wrap;
        Color[] px = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool edge = borderPx > 0 && (x < borderPx || y < borderPx || x >= w - borderPx || y >= h - borderPx);
                px[y * w + x] = edge ? border : fill;
            }
        }
        tex.SetPixels(px);
        tex.Apply(false, false);
        return tex;
    }

    static Sprite WriteSprite(string relPath, int w, int h, Color fill, Color border, int borderPx, TextureWrapMode wrap)
    {
        string assetPath = ArtRoot + "/" + relPath + ".png";
        EnsureFolder(Path.GetDirectoryName(assetPath).Replace('\\', '/'));

        Texture2D tex = MakeTexture(w, h, fill, border, borderPx, wrap);
        File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), assetPath), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        TextureImporter imp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (imp != null)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = PPU;
            imp.filterMode = FilterMode.Point;
            imp.wrapMode = wrap;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            TextureImporterSettings settings = new TextureImporterSettings();
            imp.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteExtrude = 0;
            imp.SetTextureSettings(settings);
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    [MenuItem("Tools/游趣无限/1. 重建白模素材")]
    public static void GenerateAll()
    {
        EnsureFolder(ArtRoot);

        WriteSprite("Frames/frame_p1", 240, 180, Paper, Dark, 8, TextureWrapMode.Clamp);
        WriteSprite("Frames/frame_p2", 240, 180, Sand, Dark, 8, TextureWrapMode.Clamp);
        WriteSprite("Frames/frame_ending", 240, 180, Slate, Dark, 8, TextureWrapMode.Clamp);
        WriteSprite("Frames/frame_done", 240, 180, Clay, Dark, 8, TextureWrapMode.Clamp);
        WriteSprite("Frames/frame_highlight", 240, 180, new Color(1f, 1f, 1f, 0.25f), new Color(1f, 0.95f, 0.55f, 0.95f), 6, TextureWrapMode.Clamp);
        WriteSprite("Player/player_idle", 80, 180, White, Grey, 4, TextureWrapMode.Clamp);
        WriteSprite("Environment/floor_tile", 100, 100, new Color(0.72f, 0.70f, 0.66f), new Color(0.60f, 0.58f, 0.55f), 3, TextureWrapMode.Repeat);
        WriteSprite("Environment/wall_panel", 400, 600, new Color(0.90f, 0.90f, 0.88f), new Color(0.80f, 0.80f, 0.78f), 2, TextureWrapMode.Clamp);
        WriteSprite("Environment/backdrop", 16, 16, new Color(0.83f, 0.83f, 0.82f), new Color(0.83f, 0.83f, 0.82f), 0, TextureWrapMode.Repeat);

        AssetDatabase.Refresh();
        Debug.Log("[白模] 素材已重建于 " + ArtRoot);
    }

    public static void GenerateAllBatch()
    {
        GenerateAll();
    }
}
