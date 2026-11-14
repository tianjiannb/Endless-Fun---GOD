using System.Collections.Generic;
using UnityEngine;

public static class GalleryRegistry
{
    public const string ResourceFolder = "Paintings";

    static List<PaintingData> _cache;

    public static IReadOnlyList<PaintingData> All
    {
        get
        {
            if (_cache == null) Load();
            return _cache;
        }
    }

    public static int Count { get { return All.Count; } }

    public static void Load()
    {
        PaintingData[] all = Resources.LoadAll<PaintingData>(ResourceFolder);
        _cache = new List<PaintingData>(all);
        _cache.Sort(Compare);
    }

    public static void Reload()
    {
        _cache = null;
        Load();
    }

    static int Compare(PaintingData a, PaintingData b)
    {
        int byOrder = a.sortOrder.CompareTo(b.sortOrder);
        if (byOrder != 0) return byOrder;
        return string.CompareOrdinal(a.paintingId, b.paintingId);
    }

    public static PaintingData Find(string paintingId)
    {
        IReadOnlyList<PaintingData> list = All;
        for (int i = 0; i < list.Count; i++)
            if (list[i].paintingId == paintingId) return list[i];
        return null;
    }
}
