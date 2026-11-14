using System.Collections;
using NUnit.Framework;
using SplitSlide.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SplitSlide.Tests
{
    public sealed class PuzzleSceneTests
    {
        private PuzzleGameController game;

        [UnitySetUp]
        public IEnumerator OpenGameScene()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            game = Object.FindObjectOfType<PuzzleGameController>();
            Assert.That(game, Is.Not.Null);
            Assert.That(game.Session, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator SceneBuildsPlayableBoardWithoutPhysicsComponents()
        {
            Assert.That(Object.FindObjectsOfType<Button>().Length, Is.EqualTo(12));
            Assert.That(Object.FindObjectsOfType<Rigidbody>().Length, Is.Zero);
            Assert.That(Object.FindObjectsOfType<Rigidbody2D>().Length, Is.Zero);
            Assert.That(Object.FindObjectsOfType<Collider>().Length, Is.Zero);
            Assert.That(Object.FindObjectsOfType<Collider2D>().Length, Is.Zero);
            Assert.That(GameObject.Find("Body 1"), Is.Not.Null);
            yield return null;
        }

        [UnityTest]
        public IEnumerator UndoAndResetCanInterruptDisplacementAnimation()
        {
            game.LoadLevel(1);
            game.Move(Direction.Right);
            Assert.That(game.IsAnimating, Is.True);
            game.Move(Direction.Up); // Inputs during a slide are ignored.
            Assert.That(game.Session.State.MoveCount, Is.EqualTo(1));
            game.Undo();
            yield return null;
            Assert.That(game.IsAnimating, Is.False);
            Assert.That(game.Session.State.HasSplit, Is.False);
            Assert.That(game.Session.State.MoveCount, Is.Zero);
            Assert.That(GameObject.Find("Body 2"), Is.Null);
            game.Move(Direction.Right);
            game.ResetLevel();
            yield return null;
            Assert.That(game.IsAnimating, Is.False);
            Assert.That(game.Session.CanUndo, Is.False);
            Assert.That(game.Session.State.Bodies.Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator TwoBodiesAnimateToGoalAndNextLevelResetsState()
        {
            game.LoadLevel(1);
            game.Move(Direction.Right);
            yield return WaitForSlide();
            Assert.That(GameObject.Find("Body 2"), Is.Not.Null);
            game.Move(Direction.Up);
            yield return WaitForSlide();
            Assert.That(game.Session.State.Won, Is.True);
            Assert.That(game.IsAnimating, Is.False);
            game.NextLevel();
            yield return null;
            Assert.That(game.LevelIndex, Is.EqualTo(2));
            Assert.That(game.Session.State.MoveCount, Is.Zero);
            Assert.That(game.Session.State.HasSplit, Is.False);
        }

        [UnityTest]
        public IEnumerator EveryShippedLevelCanBePlayedThroughController()
        {
            string[] solutions = { "UR", "RU", "R", "DLULD", "DUDLDR" };
            for (int index = 0; index < solutions.Length; index++)
            {
                game.LoadLevel(index);
                foreach (char key in solutions[index])
                {
                    game.Move(key == 'U' ? Direction.Up : key == 'D' ? Direction.Down
                        : key == 'L' ? Direction.Left : Direction.Right);
                    yield return WaitForSlide();
                }
                Assert.That(game.Session.State.Won, Is.True, $"Level {index + 1} failed.");
            }
        }

        private IEnumerator WaitForSlide()
        {
            float deadline = Time.realtimeSinceStartup + 5;
            while (game.IsAnimating && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(game.IsAnimating, Is.False, "Slide animation timed out.");
        }
    }
}
