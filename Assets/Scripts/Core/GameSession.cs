using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }

    public const string GallerySceneName = "Gallery";

    readonly List<ChoiceResult> _results = new List<ChoiceResult>();

    public IReadOnlyList<ChoiceResult> Results { get { return _results; } }

    string _returnScene = GallerySceneName;

    public string ReturnScene
    {
        get { return string.IsNullOrEmpty(_returnScene) ? GallerySceneName : _returnScene; }
    }

    string _currentPaintingId;

    public string CurrentPaintingId { get { return _currentPaintingId; } }

    public int CompletedCount { get { return _results.Count; } }

    public static GameSession Ensure()
    {
        if (Instance != null) return Instance;
        GameObject go = new GameObject("GameSession");
        return go.AddComponent<GameSession>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public bool IsCompleted(string paintingId)
    {
        for (int i = 0; i < _results.Count; i++)
            if (_results[i].paintingId == paintingId) return true;
        return false;
    }

    public bool TryGetLastResult(string paintingId, out ChoiceResult result)
    {
        for (int i = _results.Count - 1; i >= 0; i--)
        {
            if (_results[i].paintingId == paintingId)
            {
                result = _results[i];
                return true;
            }
        }
        result = default(ChoiceResult);
        return false;
    }

    public void RecordResult(ChoiceResult r)
    {
        for (int i = 0; i < _results.Count; i++)
        {
            if (_results[i].paintingId == r.paintingId) { _results[i] = r; return; }
        }
        _results.Add(r);
    }

    public void BeginEnter(string paintingId)
    {
        _currentPaintingId = paintingId;
        _returnScene = SceneManager.GetActiveScene().name;
    }

    public void Enter(string paintingId, string sceneName)
    {
        _currentPaintingId = paintingId;
        _returnScene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(sceneName);
    }

    public void Exit(ChoiceResult r)
    {
        RecordResult(r);
        _currentPaintingId = null;
        SceneManager.LoadScene(ReturnScene);
    }

    public void Clear()
    {
        _results.Clear();
        _currentPaintingId = null;
    }
}
