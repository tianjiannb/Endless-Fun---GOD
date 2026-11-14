using System;
using NUnit.Framework;
using SplitSlide.Core;

namespace SplitSlide.Tests
{
    public sealed class PuzzleRulesTests
    {
        private static PuzzleSession Game(params string[] rows) => new PuzzleSession(new LevelDefinition(rows));
        private static PuzzleSession SplitGame() => Game("########", "#P.....#", "#......#", "#T....S#", "########");

        [Test]
        public void SlidesToBoundaryWithoutPhysics()
        {
            var game = Game("T...P");
            game.Slide(Direction.Right);
            Assert.That(game.State.Bodies[0], Is.EqualTo(new Cell(4, 0)));
            Assert.That(game.State.Won, Is.True);
        }

        [Test]
        public void StopsImmediatelyBeforeWall()
        {
            var game = Game("T..#P");
            game.Slide(Direction.Right);
            Assert.That(game.State.Bodies[0], Is.EqualTo(new Cell(2, 0)));
        }

        [Test]
        public void BlockedInputDoesNotConsumeMoveOrUndo()
        {
            var game = Game("T#P");
            Assert.That(game.Slide(Direction.Right), Is.Null);
            Assert.That(game.State.MoveCount, Is.Zero);
            Assert.That(game.CanUndo, Is.False);
        }

        [Test]
        public void PassingSplitterDoesNotSplit()
        {
            var game = Game("#T.S.P#");
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

        [TestCase(Direction.Up, "#####", "#S.P#", "#T..#", "#####")]
        [TestCase(Direction.Down, "#####", "#T.P#", "#S..#", "#####")]
        [TestCase(Direction.Left, "#####", "#STP#", "#####", "#####")]
        [TestCase(Direction.Right, "#####", "#TS#P", "#####", "#####")]
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
            var game = Game("#########", "#.###...#", "#....a..#", "##..AT###", "#.b##S###", "#....B.P#", "#########");
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
            var game = Game("########", "#P.....#", "#.....##", "#T....S#", "########");
            game.Slide(Direction.Right);
            game.Slide(Direction.Up);
            Assert.That(game.State.Bodies, Is.EqualTo(new[] { new Cell(6, 1), new Cell(5, 3) }));
            game.Slide(Direction.Left);
            Assert.That(game.State.Bodies[0], Is.EqualTo(new Cell(1, 1)));
        }

        [Test]
        public void EnteringSwitchOpensDoorDuringSameSlide()
        {
            var game = Game("#T.a.A.P#");
            game.Slide(Direction.Right);
            Assert.That(game.State.IsDoorOpen(0), Is.True);
            Assert.That(game.State.Won, Is.True);
        }

        [Test]
        public void EnteringSameButtonTwiceClosesItsDoor()
        {
            var game = Game("########", "#..A.P.#", "#T.a...#", "########");
            game.Slide(Direction.Right);
            Assert.That(game.State.IsDoorOpen(0), Is.True);
            game.Slide(Direction.Left);
            Assert.That(game.State.IsDoorOpen(0), Is.False);
        }

        [Test]
        public void EachBodyEnteringSameSwitchCountsSeparately()
        {
            var game = Game("########", "#..A.P.#", "#T.a..S#", "########");
            game.Slide(Direction.Right);
            Assert.That(game.State.IsDoorOpen(0), Is.True);
            var result = game.Slide(Direction.Left);
            bool closedDuringSlide = false;
            foreach (var frame in result.Frames) closedDuringSlide |= !frame.IsDoorOpen(0);
            Assert.That(closedDuringSlide, Is.True);
            Assert.That(game.State.IsDoorOpen(0), Is.True);
        }

        [Test]
        public void EachButtonOnlyTogglesItsPairedDoor()
        {
            var game = Game("#########", "#T.a.A.P#", "#.b..B..#", "#########");
            game.Slide(Direction.Right);
            Assert.That(game.State.IsDoorOpen(0), Is.True);
            Assert.That(game.State.IsDoorOpen(1), Is.False);
            Assert.That(game.State.Won, Is.True);
        }

        [Test]
        public void BothPairsCanOpenInOneSlide()
        {
            var game = Game("#T.a.A.b.B.P#");
            game.Slide(Direction.Right);
            Assert.That(game.State.IsDoorOpen(0), Is.True);
            Assert.That(game.State.IsDoorOpen(1), Is.True);
            Assert.That(game.State.Won, Is.True);
        }

        [Test]
        public void ClosedDoorBlocksEntry()
        {
            var game = Game("#########", "#..a....#", "#T.A...P#", "#########");
            game.Slide(Direction.Right);
            Assert.That(game.State.Bodies[0], Is.EqualTo(new Cell(2, 1)));
        }

        [Test]
        public void InitiallyOpenDoorCanBePassed()
        {
            var game = new PuzzleSession(new LevelDefinition(new[]
                { "#########", "#..a....#", "#T.A...P#", "#########" }, true));
            game.Slide(Direction.Right);
            Assert.That(game.State.Won, Is.True);
        }

        [Test]
        public void StoppedBodyDoesNotResumeWhenOtherBodyOpensDoor()
        {
            var game = Game("########", "#P.....#", "#....aA#", "#T....S#", "########");
            game.Slide(Direction.Right);
            game.Slide(Direction.Up);
            Assert.That(game.State.IsDoorOpen(0), Is.True);
            Assert.That(game.State.Bodies[0], Is.EqualTo(new Cell(6, 1)));
            game.Slide(Direction.Up);
            Assert.That(game.State.Bodies[0], Is.EqualTo(new Cell(6, 3)));
        }

        [Test]
        public void ClosingDoorOnBodyDoesNotCrushItOrPreventExit()
        {
            var game = new PuzzleSession(new LevelDefinition(new[]
                { "########", "#P.....#", "#....aA#", "#T....S#", "########" }, true));
            game.Slide(Direction.Right);
            game.Slide(Direction.Up);
            Assert.That(game.State.IsDoorOpen(0), Is.False);
            Assert.That(game.State.Bodies[0], Is.EqualTo(new Cell(6, 3)));
        }

        [Test]
        public void StandingOnSwitchDoesNotFlipAgainOnBlockedInput()
        {
            var game = Game("#####", "#PaA#", "##T##", "#####");
            game.Slide(Direction.Up);
            Assert.That(game.State.IsDoorOpen(0), Is.True);
            Assert.That(game.Slide(Direction.Up), Is.Null);
            Assert.That(game.State.IsDoorOpen(0), Is.True);
        }

        [Test]
        public void PassingGoalDoesNotWin()
        {
            var game = Game("#T.P..#");
            game.Slide(Direction.Right);
            Assert.That(game.State.Won, Is.False);
        }

        [Test]
        public void OneOccupiedGoalWinsWithExtraBodyAndClosedDoors()
        {
            var game = Game("#########", "#.....P.#", "#.......#", "#T.....S#", "#########");
            game.Slide(Direction.Right);
            game.Slide(Direction.Up);
            Assert.That(game.State.Bodies.Count, Is.EqualTo(2));
            Assert.That(game.State.IsDoorOpen(0), Is.False);
            Assert.That(game.State.Won, Is.True);
        }

        [Test]
        public void AllDesignatedGoalsMustBeOccupied()
        {
            var game = Game("########", "#....PP#", "#......#", "#T....S#", "########");
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
            var game = Game("#T.a.A.P#");
            game.Slide(Direction.Right);
            Assert.That(game.Slide(Direction.Left), Is.Null);
            game.Undo();
            Assert.That(game.State.IsDoorOpen(0), Is.False);
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

        [TestCase("T.a.P#")] // button without its door
        [TestCase("T.A.P#")] // door without its button
        [TestCase("T.aa.A.P#")] // two buttons for one door
        [TestCase("T.a.AA.P#")] // two doors for one button
        public void RejectsUnpairedButtonsAndDoors(string row)
            => Assert.Throws<ArgumentException>(() => Game(row));

        [Test]
        public void RejectsRaggedAndEmptyLevels()
        {
            Assert.Throws<ArgumentException>(() => Game("PG", "."));
            Assert.Throws<ArgumentException>(() => Game());
        }
    }
}
