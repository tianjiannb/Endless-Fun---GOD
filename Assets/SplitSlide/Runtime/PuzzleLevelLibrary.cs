using System;
using SplitSlide.Core;
using UnityEngine;

namespace SplitSlide
{
    [Serializable]
    public sealed class PuzzleLevelEntry
    {
        public string title;
        [TextArea(2, 4)] public string hint;
        public bool doorsInitiallyOpen;
        public string[] rows;

        public LevelDefinition CreateDefinition() => new LevelDefinition(rows, doorsInitiallyOpen);
    }

    [CreateAssetMenu(menuName = "Split Slide/Level Library", fileName = "PuzzleLevels")]
    public sealed class PuzzleLevelLibrary : ScriptableObject
    {
        public PuzzleLevelEntry[] levels;

        private void OnValidate()
        {
            if (levels == null) return;
            for (int i = 0; i < levels.Length; i++)
            {
                try
                {
                    if (levels[i] == null) throw new ArgumentException("Missing level entry.");
                    levels[i].CreateDefinition();
                }
                catch (ArgumentException exception)
                {
                    Debug.LogWarning($"Level {i + 1}: {exception.Message}", this);
                }
            }
        }
    }
}
