using System;
using System.Collections;
using SplitSlide.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SplitSlide
{
    public sealed class PuzzleGameController : MonoBehaviour
    {
        [SerializeField] private PuzzleLevelLibrary levelLibrary;
        [SerializeField, Min(1)] private float cellsPerSecond = 9;
        private PuzzleSession session;
        private PuzzleView view;
        private Coroutine slideRoutine;
        private bool finishing;
        private int levelIndex;
        public PuzzleSession Session => session;
        public bool IsAnimating => slideRoutine != null;
        public int LevelIndex => levelIndex;
        private bool HasNext => levelLibrary != null && levelIndex + 1 < levelLibrary.levels.Length;

        private void Awake()
        {
            if (levelLibrary == null) levelLibrary = Resources.Load<PuzzleLevelLibrary>("PuzzleLevels");
            if (levelLibrary == null || levelLibrary.levels == null || levelLibrary.levels.Length == 0)
            {
                Debug.LogError("请为玩法控制器配置非空的关卡库。", this);
                enabled = false;
                return;
            }
            view = gameObject.AddComponent<PuzzleView>();
            view.Build(levelLibrary, Undo, ResetLevel, LoadLevel, Move);
            LoadLevel(0);
            PuzzleAudio.Cue("music_puzzle");
        }

        private void Update()
        {
            if (finishing) return;
            if (Input.GetKeyDown(KeyCode.H)) { view.ToggleHint(); return; }
            for (int i = 0; i < levelLibrary.levels.Length && i < 9; i++)
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i))) { LoadLevel(i); return; }
            if (Input.GetKeyDown(KeyCode.R)) { ResetLevel(); return; }
            if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Backspace)) { Undo(); return; }
            if (Input.GetKeyDown(KeyCode.N)) { NextLevel(); return; }
            if (IsAnimating || session == null || session.State.Won) return;
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) Move(Direction.Up);
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) Move(Direction.Down);
            else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) Move(Direction.Left);
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) Move(Direction.Right);
        }

        public void LoadLevel(int index)
        {
            // 过关流程拥有更高优先级：转场期间忽略一切切关请求，避免黑屏卡死
            if (finishing) return;
            LoadLevelInternal(index);
        }

        private void LoadLevelInternal(int index)
        {
            if (levelLibrary == null || index < 0 || index >= levelLibrary.levels.Length) return;
            CancelAnimation();
            finishing = false;
            try
            {
                PuzzleLevelEntry entry = levelLibrary.levels[index];
                if (entry == null) throw new ArgumentException("Missing level entry.");
                var nextSession = new PuzzleSession(entry.CreateDefinition());
                levelIndex = index;
                session = nextSession;
                view.ShowLevel(session.Level, entry, index);
                Refresh();
            }
            catch (ArgumentException exception)
            {
                Debug.LogError($"Cannot load level {index + 1}: {exception.Message}", this);
                session = null;
                view.ShowError($"第 {index + 1} 关暂时无法加载，请尝试切换其他关卡。");
            }
        }

        public void Move(Direction direction)
        {
            if (finishing || session == null || IsAnimating || session.State.Won) return;
            SlideResult result = session.Slide(direction);
            if (result == null)
            {
                view.ShowBlocked();
                view.BumpWalls(session.State, direction);
                PuzzleAudio.Cue("blocked");
                return;
            }
            PuzzleAudio.Cue("slide");
            slideRoutine = StartCoroutine(Animate(result, direction));
        }

        public void Undo()
        {
            if (finishing || session == null) return;
            CancelAnimation();
            session.Undo();
            Refresh();
        }

        public void ResetLevel()
        {
            if (finishing || session == null) return;
            CancelAnimation();
            session.Reset();
            Refresh();
        }

        public void NextLevel()
        {
            if (finishing) return;
            if (session != null && session.State.Won && !IsAnimating && HasNext) LoadLevel(levelIndex + 1);
        }

        private IEnumerator Animate(SlideResult result, Direction direction)
        {
            PuzzleSnapshot previous = result.Before;
            view.Apply(previous, session.CanUndo, true, HasNext);
            for (int f = 0; f < result.Frames.Count; f++)
            {
                PuzzleSnapshot frame = result.Frames[f];
                bool settling = f == result.Frames.Count - 1;
                bool displaced = false;
                for (int i = 0; i < previous.Bodies.Count; i++)
                    if (previous.Bodies[i] != frame.Bodies[i]) displaced = true;
                if (displaced)
                {
                    float progress = 0;
                    while (progress < 1)
                    {
                        progress = Mathf.Min(1, progress + Time.unscaledDeltaTime * Mathf.Max(1, cellsPerSecond));
                        // Displacement, not Rigidbody/forces/collision callbacks.
                        view.Interpolate(previous, frame, progress, settling);
                        yield return null;
                    }
                }
                view.Apply(frame, session.CanUndo, true, HasNext);
                previous = frame;
            }
            slideRoutine = null;
            PuzzleAudio.Cue("slide_stop");
            bool hitWall = view.BumpWalls(session.State, direction);
            if (hitWall) PuzzleAudio.Cue("blocked");
            Refresh();
            if (session.State.Won) StartCoroutine(WinSequence());
        }

        /// <summary>Celebrates the droplets, then advances or returns to the gallery.</summary>
        private IEnumerator WinSequence()
        {
            if (finishing) yield break;
            finishing = true;
            view.PlayWin();
            yield return new WaitForSecondsRealtime(.45f);
            yield return view.FadeToBlack(1.1f);
            yield return new WaitForSecondsRealtime(.45f);
            if (session == null || !session.State.Won) yield break;
            if (HasNext)
            {
                LoadLevelInternal(levelIndex + 1);
                yield return view.FadeFromBlack(.5f);
                yield break;
            }
            PaintingSceneRootExit();
        }

        /// <summary>
        /// Leaves the painting scene. PaintingSceneRoot lives in the global assembly, which this
        /// assembly cannot reference, so it is called by type name and falls back to the gallery.
        /// </summary>
        private void PaintingSceneRootExit()
        {
            foreach (MonoBehaviour behaviour in FindObjectsOfType<MonoBehaviour>())
            {
                if (behaviour.GetType().Name != "PaintingSceneRoot") continue;
                behaviour.SendMessage("ExitWith", "solved", SendMessageOptions.DontRequireReceiver);
                return;
            }
            SceneManager.LoadScene("Gallery");
        }

        private void CancelAnimation()
        {
            if (slideRoutine == null) return;
            StopCoroutine(slideRoutine);
            slideRoutine = null;
            PuzzleAudio.Cue("slide_stop");
        }

        private void Refresh() => view.Apply(session.State, session.CanUndo, false, HasNext);

        private void OnDisable()
        {
            CancelAnimation();
            PuzzleAudio.Cue("music_stop");
        }
    }

    /// <summary>
    /// SplitSlide.Runtime 无法直接引用全局程序集里的 AudioManager，
    /// 这里按类型名找到它并用 SendMessage 转发音效 id。
    /// </summary>
    internal static class PuzzleAudio
    {
        private static MonoBehaviour manager;

        public static void Cue(string id)
        {
            if (manager == null)
            {
                foreach (MonoBehaviour behaviour in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>())
                {
                    if (behaviour.GetType().Name != "AudioManager") continue;
                    manager = behaviour;
                    break;
                }
                if (manager == null) return;
            }
            manager.SendMessage("PlayCue", id, SendMessageOptions.DontRequireReceiver);
        }
    }
}
