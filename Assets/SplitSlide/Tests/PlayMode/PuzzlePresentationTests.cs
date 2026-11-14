using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SplitSlide.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SplitSlide.Tests
{
    public sealed class PuzzlePresentationTests
    {
        private static IEnumerator Open(string path)
        {
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(path,
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
#else
            yield return SceneManager.LoadSceneAsync(System.IO.Path.GetFileNameWithoutExtension(path));
#endif
        }

        [UnityTest]
        public IEnumerator EveryLevelHasChineseHintsAndRenderableChineseGlyphs()
        {
            yield return Open("Assets/Scenes/SampleScene.unity");
            var game = Object.FindObjectOfType<PuzzleGameController>();
            var library = Resources.Load<PuzzleLevelLibrary>("PuzzleLevels");
            for (int i = 0; i < library.levels.Length; i++)
            {
                Assert.That(Regex.IsMatch(library.levels[i].title, "[\u4e00-\u9fff]"), Is.True);
                Assert.That(Regex.IsMatch(library.levels[i].hint, "[\u4e00-\u9fff]"), Is.True);
                game.LoadLevel(i);
                game.GetComponent<PuzzleView>().ToggleHint();
                yield return null;
                AssertChineseFontAndGlyphs();
                foreach (Text text in Object.FindObjectsOfType<Text>(true))
                    Assert.That(Regex.IsMatch(text.text, "Undo|Reset|MOVES|Goal reached|Blocked|Press|Slide|Switch"), Is.False, text.text);
            }
        }

        [UnityTest]
        public IEnumerator BlockedWinCompletionAndErrorMessagesAreChinese()
        {
            yield return Open("Assets/Scenes/SampleScene.unity");
            var game = Object.FindObjectOfType<PuzzleGameController>();
            var view = game.GetComponent<PuzzleView>();
            view.ShowBlocked();
            AssertMessageContains("方向被挡住");
            var entry = new PuzzleLevelEntry { title = "验证关卡", hint = "占住目标叶片。", rows = new[] { "TP" } };
            var session = new PuzzleSession(entry.CreateDefinition());
            session.Slide(Direction.Right);
            view.ShowLevel(session.Level, entry, 0);
            view.Apply(session.State, true, false, true);
            AssertMessageContains("进入下一关");
            view.ShowLevel(session.Level, entry, 0);
            view.Apply(session.State, true, false, false);
            AssertMessageContains("恭喜通关");
            view.ShowError("关卡暂时无法加载，请重试。");
            AssertMessageContains("关卡暂时无法加载");
            yield return null;
            AssertChineseFontAndGlyphs();
        }

        [UnityTest]
        public IEnumerator GalleryAndBothPaintingScenesUseBundledChineseFont()
        {
            foreach (string name in new[] { "Gallery", "Painting_P1", "Painting_P2" })
            {
                yield return Open($"Assets/Scenes/Gallery/{name}.unity");
                Assert.That(Object.FindObjectsOfType<Text>(true).Length, Is.GreaterThan(0));
                AssertChineseFontAndGlyphs();
            }
        }

        private static void AssertMessageContains(string phrase)
        {
            bool found = false;
            foreach (Text text in Object.FindObjectsOfType<Text>()) found |= text.text.Contains(phrase);
            Assert.That(found, Is.True, phrase);
        }

        private static void AssertChineseFontAndGlyphs()
        {
            foreach (Text text in Object.FindObjectsOfType<Text>(true))
            {
                Assert.That(text.font, Is.Not.Null, text.name);
                Assert.That(text.font.name, Is.EqualTo("NotoSansCJKsc-Regular"), text.name);
                text.font.RequestCharactersInTexture(text.text, text.fontSize, text.fontStyle);
                foreach (char character in text.text)
                {
                    if (char.IsWhiteSpace(character)) continue;
                    Assert.That(text.font.GetCharacterInfo(character, out _, text.fontSize, text.fontStyle),
                        Is.True, $"Missing glyph: {character} in {text.text}");
                }
            }
        }
    }
}
