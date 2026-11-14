using System;
using NUnit.Framework;
using SplitSlide.Core;

namespace SplitSlide.Tests
{
    public sealed class PuzzleRulesTests
    {
        private static PuzzleSession Game(params string[] rows) => new PuzzleSession(new LevelDefinition(rows));
        private static PuzzleSession SplitGame() => Game("########", "#G.....#", "#......#", "#P....S#", "########");

        [Test]
        public void SlidesToBoundaryWithoutPhysics()
        {
            var game = Game("P...G");
            game.Slide(Direction.Right);
            Assert.That(game.State.Bodies[0], Is.EqualTo(new Cell(4, 0)));
            Assert.That(game.State.Won, Is.True);
        }

        [Test]
        public void StopsImmediatelyBeforeWall()
        {
            var game = Game("P..#G");
            game.Slide(Direction.Right);
            Assert.That(game.State.Bodies[0], Is.EqualTo(new Cell(2, 0)));
        }

        [Test]
        public void BlockedInputDoesNotConsumeMoveOrUndo()
        {
            var game = Game("P#G");
            Assert.That(game.Slide(Direction.Right), Is.Null);
            Assert.That(game.State.MoveCount, Is.Zero);
            Assert.That(game.CanUndo, Is.False);
        }

        [Test]
        public void PassingSplitterDoesNotSplit()
        {
            var game = Game("#P.S.G#");
            game.Slide(Direction.Right);
            Assert.That(game.State.HasSplit, Is.False);
            Assert.That(game.State.Bodies.Count, Is.EqualTo(1));
        }

        [Test]
        public void StoppingOnSplitterSpawnsExactlyOneCellBehind()
        {
            var game = SplitGame();
            game.Slide(Direction.Right);
            Assert.That(game.State.HasSplit, Is.True);
            Assert.That(game.State.Bodies, Is.EqualTo(new[] { new Cell(6, 1), new Cell(5, 1) }));
        }

        [TestCase(Direction.Up, "#####", "#S.G#", "#P..#", "#####")]
        [TestCase(Direction.Down, "#####", "#P.G#", "#S..#", "#####")]
        [TestCase(Direction.Left, "#####", "#SPG#", "#####", "#####")]
        [TestCase(Direction.Right, "#####", "#PS#G", "#####", "#####")]
        public void RearSpawnUsesActualInputDirection(Direction direction, string a, string b, string c, string d)
        {
            var game = Game(a, b, c, d);
            game.Slide(direction);
            Assert.That(game.State.HasSplit, Is.True);
            Assert.That(game.State.Bodies[1], Is.EqualTo(game.State.Bodies[0] - Directions.Offset(direction)));
        }

        [Test]
        public void LongInputSequencesNeverOverlapOrLeaveBoard()
        {
            var game = Game("#########", "#.##....#", "#....T..#", "##..DP..#", "#....S..#", "#..G.#..#", "#########");
            var random = new Random(714);
            for (int step = 0; step < 2000; step++)
            {
                SlideResult result = game.Slide((Direction)random.Next(4));
                if (result != null)
                foreach (PuzzleSnapshot frame in result.Frames)
                {
                    Assert.That(frame.Bodies.Count, Is.EqualTo(frame.HasSplit ? 2 : 1));
                    foreach (Cell body in frame.Bodies)
                    {
                        Assert.That(game.Level.Contains(body), Is.True);
                        Assert.That(game.Level.TileAt(body), Is.Not.EqualTo(Tile.Wall));
                    }
                    if (frame.Bodies.Count == 2) Assert.That(frame.Bodies[0], Is.Not.EqualTo(frame.Bodies[1]));
                }
                if (game.State.Won || random.Next(8) == 0) game.Undo();
                if (random.Next(60) == 0) game.Reset();
            }
        }

        [Test]
        public void SplitCannotHappenAgainAfterReturningToSplitter()
        {
            var game = SplitGame();
            game.Slide(Direction.Right);
            game.Slide(Direction.Left);
            game.Slide(Direction.Right);
            Assert.That(game.State.Bodies.Count, Is.EqualTo(2));
        }

        [Test]
        public void AdjacentBodiesMoveTogetherInEitherOrderWithoutCrossing()
        {
            var game = SplitGame();
            game.Slide(Direction.Right);
            var result = game.Slide(Direction.Left);
            foreach (var frame in result.Frames)
            {
                Assert.That(frame.Bodies[0].X - frame.Bodies[1].X, Is.EqualTo(1));
                Assert.That(frame.Bodies[0].Y, Is.EqualTo(frame.Bodies[1].Y));
            }
            Assert.That(game.State.Bodies, Is.EqualTo(new[] { new Cell(2, 1), new Cell(1, 1) }));
            game.Slide(Direction.Right);
            Assert.That(game.State.Bodies, Is.EqualTo(new[] { new Cell(6, 1), new Cell(5, 1) }));
        }

        [Test]
        public void BodiesStopIndependentlyAndRestartOnNextInput()
        {
            var game = Game("########", "#G.....#", "#.....##", "#P....S#", "########");
            game.Slide(Direction.Right);
            game.Slide(Direction.Up);
            Assert.That(game.State.Bodies, Is.EqualTo(new[] { new Cell(6, 1), new Cell(5, 3) }));
            game.Slide(Direction.Left);
            Assert.That(game.State.Bodies[0], Is.EqualTo(new Cell(1, 1)));
        }

        [Test]
        public void EnteringSwitchOpensDoorDuringSameSlide()
        {
            var game = Game("#P.T.D.G#");
            game.Slide(Direction.Right);
            Assert.That(game.State.DoorsOpen, Is.True);
            Assert.That(game.State.Won, Is.True);
        }

        [Test]
        public void TwoSwitchEntriesFlipTwiceAndCloseDoor()
        {
            var game = Game("#PTT.DG#");
            game.Slide(Direction.Right);
            Assert.That(game.State.DoorsOpen, Is.False);
            Assert.That(game.State.Bodies[0], Is.EqualTo(new Cell(4, 0)));
        }

        [Test]
        public void EachBodyEnteringSameSwitchCountsSeparately()
        {
            var game = Game("########", "#G.....#", "#P.T..S#", "########");
            game.Slide(Direction.Right);
            Assert.That(game.State.DoorsOpen, Is.True);
            var result = game.Slide(Direction.Left);
            bool closedDuringSlide = false;
            foreach (var frame in result.Frames) closedDuringSlide |= !frame.DoorsOpen;
            Assert.That(closedDuringSlide, Is.True);
            Assert.That(game.State.DoorsOpen, Is.True);
        }

        [Test]
        public void SimultaneousSwitchEntriesCancelEachOther()
        {
            var game = Game("########", "#G.....#", "#....TT#", "#P....S#", "########");
            game.Slide(Direction.Right);
            var result = game.Slide(Direction.Up);
            foreach (var frame in result.Frames) Assert.That(frame.DoorsOpen, Is.False);
        }

        [Test]
        public void ClosedDoorBlocksEntry()
        {
            var game = Game("#P.D.G#");
            game.Slide(Direction.Right);
            Assert.That(game.State.Bodies[0], Is.EqualTo(new Cell(2, 0)));
        }

        [Test]
        public void InitiallyOpenDoorCanBePassed()
        {
            var game = new PuzzleSession(new LevelDefinition(new[] { "#P.D.G#" }, true));
            game.Slide(Direction.Right);
            Assert.That(game.State.Won, Is.True);
        }

        [Test]
        public void StoppedBodyDoesNotResumeWhenOtherBodyOpensDoor()
        {
            var game = Game("########", "#G.....#", "#....TD#", "#P....S#", "########");
            game.Slide(Direction.Right);
            game.Slide(Direction.Up);
            Assert.That(game.State.DoorsOpen, Is.True);
            Assert.That(game.State.Bodies[0], Is.EqualTo(new Cell(6, 1)));
            game.Slide(Direction.Up);
            Assert.That(game.State.Bodies[0], Is.EqualTo(new Cell(6, 3)));
        }

        [Test]
        public void ClosingDoorOnBodyDoesNotCrushItOrPreventExit()
        {
            var game = new PuzzleSession(new LevelDefinition(new[]
                { "########", "#G.....#", "#....TD#", "#P....S#", "########" }, true));
            game.Slide(Direction.Right);
            game.Slide(Direction.Up);
            Assert.That(game.State.DoorsOpen, Is.False);
            Assert.That(game.State.Bodies[0], Is.EqualTo(new Cell(6, 3)));
        }

        [Test]
        public void StandingOnSwitchDoesNotFlipAgainOnBlockedInput()
        {
            var game = Game("####", "#GT#", "##P#", "####");
            game.Slide(Direction.Up);
            Assert.That(game.State.DoorsOpen, Is.True);
            Assert.That(game.Slide(Direction.Up), Is.Null);
            Assert.That(game.State.DoorsOpen, Is.True);
        }

        [Test]
        public void PassingGoalDoesNotWin()
        {
            var game = Game("#P.G..#");
            game.Slide(Direction.Right);
            Assert.That(game.State.Won, Is.False);
        }

        [Test]
        public void OneOccupiedGoalWinsWithExtraBodyAndClosedDoors()
        {
            var game = Game("#########", "#.....G.#", "#.......#", "#P.....S#", "#########");
            game.Slide(Direction.Right);
            game.Slide(Direction.Up);
            Assert.That(game.State.Bodies.Count, Is.EqualTo(2));
            Assert.That(game.State.DoorsOpen, Is.False);
            Assert.That(game.State.Won, Is.True);
        }

        [Test]
        public void AllDesignatedGoalsMustBeOccupied()
        {
            var game = Game("########", "#....GG#", "#......#", "#P....S#", "########");
            game.Slide(Direction.Up);
            game.Slide(Direction.Right);
            Assert.That(game.State.Won, Is.False);
            game.Reset();
            game.Slide(Direction.Right);
            game.Slide(Direction.Up);
            Assert.That(game.State.Won, Is.True);
        }

        [Test]
        public void UndoRestoresSplitBodyCountAndAllowsSplitAgain()
        {
            var game = SplitGame();
            game.Slide(Direction.Right);
            game.Undo();
            Assert.That(game.State.HasSplit, Is.False);
            Assert.That(game.State.Bodies.Count, Is.EqualTo(1));
            Assert.That(game.State.MoveCount, Is.Zero);
            game.Slide(Direction.Right);
            Assert.That(game.State.HasSplit, Is.True);
        }

        [Test]
        public void UndoRestoresDoorsWinAndMoveCount()
        {
            var game = Game("#P.T.D.G#");
            game.Slide(Direction.Right);
            Assert.That(game.Slide(Direction.Left), Is.Null);
            game.Undo();
            Assert.That(game.State.DoorsOpen, Is.False);
            Assert.That(game.State.Won, Is.False);
            Assert.That(game.State.MoveCount, Is.Zero);
            Assert.That(game.State.Bodies[0], Is.EqualTo(new Cell(1, 0)));
        }

        [Test]
        public void BlockedInputDoesNotHideLastValidUndo()
        {
            var game = SplitGame();
            game.Slide(Direction.Right);
            Assert.That(game.Slide(Direction.Right), Is.Null);
            Assert.That(game.Undo(), Is.True);
            Assert.That(game.State.MoveCount, Is.Zero);
        }

        [Test]
        public void ResetRestoresInitialStateAndClearsHistory()
        {
            var game = SplitGame();
            game.Slide(Direction.Right);
            game.Reset();
            Assert.That(game.State.Bodies.Count, Is.EqualTo(1));
            Assert.That(game.State.HasSplit, Is.False);
            Assert.That(game.State.MoveCount, Is.Zero);
            Assert.That(game.CanUndo, Is.False);
        }

        [Test]
        public void SnapshotDoesNotChangeWhenSessionMovesOrUndoes()
        {
            var game = SplitGame();
            var initial = game.State;
            var result = game.Slide(Direction.Right);
            game.Undo();
            Assert.That(initial.Bodies.Count, Is.EqualTo(1));
            Assert.That(result.After.Bodies.Count, Is.EqualTo(2));
            Assert.That(result.After.HasSplit, Is.True);
        }

        [TestCase("P")]
        [TestCase("PPG")]
        [TestCase("G")]
        [TestCase("P?G")]
        [TestCase("PGGG")]
        public void RejectsMalformedLevel(string row) => Assert.Throws<ArgumentException>(() => Game(row));

        [Test]
        public void RejectsRaggedAndEmptyLevels()
        {
            Assert.Throws<ArgumentException>(() => Game("PG", "."));
            Assert.Throws<ArgumentException>(() => Game());
        }
    }
}
