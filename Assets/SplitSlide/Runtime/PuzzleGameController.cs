using System;
using System.Collections;
using SplitSlide.Core;
using UnityEngine;

namespace SplitSlide
{
    public sealed class PuzzleGameController : MonoBehaviour
    {
        [SerializeField] private PuzzleLevelLibrary levelLibrary;
        [SerializeField, Min(1)] private float cellsPerSecond = 9;
        private PuzzleSession session;
        private PuzzleView view;
        private Coroutine slideRoutine;
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
                Debug.LogError("Assign a non-empty PuzzleLevelLibrary to PuzzleGameController.", this);
                enabled = false;
                return;
            }
            view = gameObject.AddComponent<PuzzleView>();
            view.Build(levelLibrary, Undo, ResetLevel, NextLevel, LoadLevel, Move);
            LoadLevel(0);
        }

        private void Update()
        {
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
            if (levelLibrary == null || index < 0 || index >= levelLibrary.levels.Length) return;
            CancelAnimation();
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
                view.ShowError($"Invalid level {index + 1}. Check the Console.");
            }
        }

        public void Move(Direction direction)
        {
            if (session == null || IsAnimating || session.State.Won) return;
            SlideResult result = session.Slide(direction);
            if (result == null) { view.ShowBlocked(); return; }
            slideRoutine = StartCoroutine(Animate(result));
        }

        public void Undo()
        {
            if (session == null) return;
            CancelAnimation();
            session.Undo();
            Refresh();
        }

        public void ResetLevel()
        {
            if (session == null) return;
            CancelAnimation();
            session.Reset();
            Refresh();
        }

        public void NextLevel()
        {
            if (session != null && session.State.Won && !IsAnimating && HasNext) LoadLevel(levelIndex + 1);
        }

        private IEnumerator Animate(SlideResult result)
        {
            PuzzleSnapshot previous = result.Before;
            view.Apply(previous, session.CanUndo, true, HasNext);
            foreach (PuzzleSnapshot frame in result.Frames)
            {
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
                        view.Interpolate(previous, frame, progress);
                        yield return null;
                    }
                }
                view.Apply(frame, session.CanUndo, true, HasNext);
                previous = frame;
            }
            slideRoutine = null;
            Refresh();
        }

        private void CancelAnimation()
        {
            if (slideRoutine == null) return;
            StopCoroutine(slideRoutine);
            slideRoutine = null;
        }

        private void Refresh() => view.Apply(session.State, session.CanUndo, false, HasNext);
        private void OnDisable() => CancelAnimation();
    }
}
