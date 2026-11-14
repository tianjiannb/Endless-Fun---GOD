using System.Collections;
using NUnit.Framework;
using SplitSlide.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SplitSlide.Tests
{
    public sealed class PuzzleArtworkTests
    {
        private PuzzleGameController game;
        private PuzzleView view;

        [UnitySetUp]
        public IEnumerator OpenScene()
        {
#if UNITY_EDITOR
            // Gallery is the build entry point; the puzzle can also be reviewed directly in the editor.
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/SampleScene.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
#else
            yield return SceneManager.LoadSceneAsync("SampleScene");
#endif
            game = Object.FindObjectOfType<PuzzleGameController>();
            view = game.GetComponent<PuzzleView>();
        }

        [UnityTest]
        public IEnumerator EveryCurrentLevelUsesSuppliedTileAndPlayerArtwork()
        {
            var library = Resources.Load<PuzzleLevelLibrary>("PuzzleLevels");
            for (int index = 0; index < library.levels.Length; index++)
            {
                game.LoadLevel(index);
                yield return null;
                Assert.That(GameObject.Find("Body 1").GetComponent<Image>().sprite.name, Is.EqualTo("09_玩家细胞"));
                foreach (Image image in GameObject.Find("Board Grid").GetComponentsInChildren<Image>())
                {
                    if (image.name.StartsWith("Floor ")) Assert.That(image.sprite.name, Is.EqualTo("10_莲池油画地板"));
                    if (image.name.StartsWith("Wall ")) Assert.That(image.sprite.name, Is.EqualTo("02_墙壁"));
                    if (image.name == "Splitter Artwork") Assert.That(image.sprite.name, Is.EqualTo("03_分裂格"));
                    if (image.name == "Goal Artwork") Assert.That(image.sprite.name, Is.EqualTo("04_目标格"));
                }
            }
            Assert.That(GameObject.Find("Leaf Backdrop").GetComponent<RawImage>().texture, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator DoorAndSwitchPicturesFollowTheirOwnPairAndUndo()
        {
            var entry = new PuzzleLevelEntry
            {
                title = "Artwork check", hint = "",
                rows = new[] { "##########", "#T.aA.P..#", "#bB......#", "##########" }
            };
            var session = new PuzzleSession(entry.CreateDefinition());
            view.ShowLevel(session.Level, entry, 0);
            view.Apply(session.State, false, false, true);
            AssertFixture("Switch (3, 2)", "06_开关格_关闭");
            AssertFixture("Door (4, 2)", "07_门_关闭");
            session.Slide(Direction.Right);
            view.Apply(session.State, true, false, true);
            AssertFixture("Switch (3, 2)", "05_开关格_开启");
            AssertFixture("Door (4, 2)", "08_门_开启");
            AssertFixture("Switch (1, 1)", "06_开关格_关闭");
            AssertFixture("Door (2, 1)", "07_门_关闭");
            session.Undo();
            view.Apply(session.State, false, false, true);
            AssertFixture("Switch (3, 2)", "06_开关格_关闭");
            AssertFixture("Door (4, 2)", "07_门_关闭");
            yield return null;
        }

        [UnityTest]
        public IEnumerator InitiallyOpenDoorsCanCloseIndependentlyAndResetTheirPictures()
        {
            var entry = new PuzzleLevelEntry
            {
                title = "Artwork check", hint = "", doorsInitiallyOpen = true,
                rows = new[] { "##########", "#T.aA.P..#", "#bB......#", "##########" }
            };
            var session = new PuzzleSession(entry.CreateDefinition());
            view.ShowLevel(session.Level, entry, 0);
            view.Apply(session.State, false, false, true);
            AssertFixture("Door (4, 2)", "08_门_开启");
            AssertFixture("Door (2, 1)", "08_门_开启");
            session.Slide(Direction.Right);
            view.Apply(session.State, true, false, true);
            AssertFixture("Door (4, 2)", "07_门_关闭");
            AssertFixture("Switch (3, 2)", "06_开关格_关闭");
            AssertFixture("Door (2, 1)", "08_门_开启");
            AssertFixture("Switch (1, 1)", "05_开关格_开启");
            session.Reset();
            view.Apply(session.State, false, false, true);
            AssertFixture("Door (4, 2)", "08_门_开启");
            yield return null;
        }

        [UnityTest]
        public IEnumerator SplitCreatesTwoTexturedBodiesAndUndoRemovesSecondBody()
        {
            var entry = new PuzzleLevelEntry
            {
                title = "Artwork check", hint = "",
                rows = new[] { "########", "#P.....#", "#T....S#", "########" }
            };
            var session = new PuzzleSession(entry.CreateDefinition());
            view.ShowLevel(session.Level, entry, 0);
            view.Apply(session.State, false, false, true);
            session.Slide(Direction.Right);
            view.Apply(session.State, true, false, true);
            Assert.That(GameObject.Find("Body 2").GetComponent<Image>().sprite.name, Is.EqualTo("09_玩家细胞"));
            session.Undo();
            view.Apply(session.State, false, false, true);
            yield return null;
            Assert.That(GameObject.Find("Body 2"), Is.Null);
        }

        private static void AssertFixture(string cellName, string expectedSprite)
        {
            Transform cell = GameObject.Find(cellName).transform;
            string name = cellName.StartsWith("Door") ? "Door Artwork" : "Switch Artwork";
            Assert.That(cell.Find(name).GetComponent<Image>().sprite.name, Is.EqualTo(expectedSprite));
        }
    }
}
