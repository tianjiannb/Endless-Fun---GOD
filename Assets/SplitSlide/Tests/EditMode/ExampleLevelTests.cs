using NUnit.Framework;
using SplitSlide.Core;
using UnityEngine;

namespace SplitSlide.Tests
{
    public sealed class ExampleLevelTests
    {
        [TestCase(0, "UR")]
        [TestCase(1, "RU")]
        [TestCase(2, "R")]
        [TestCase(3, "DLULD")]
        [TestCase(4, "DUDLDR")]
        public void ShippedLevelHasAWorkingSolution(int index, string solution)
        {
            var library = Resources.Load<PuzzleLevelLibrary>("PuzzleLevels");
            Assert.That(library, Is.Not.Null);
            var game = new PuzzleSession(library.levels[index].CreateDefinition());
            foreach (char key in solution)
            {
                Direction direction = key == 'U' ? Direction.Up : key == 'D' ? Direction.Down
                    : key == 'L' ? Direction.Left : Direction.Right;
                Assert.That(game.Slide(direction), Is.Not.Null, "Unexpected blocked input in solution.");
            }
            Assert.That(game.State.Won, Is.True);
        }

        [Test]
        public void FinaleHasTwoSeparatelyPairedDoors()
        {
            var library = Resources.Load<PuzzleLevelLibrary>("PuzzleLevels");
            Assert.That(library, Is.Not.Null);
            var level = library.levels[4].CreateDefinition();
            int doors = 0, buttons = 0;
            for (int y = 0; y < level.Height; y++)
            for (int x = 0; x < level.Width; x++)
            {
                var cell = new Cell(x, y);
                if (level.TileAt(cell) == Tile.Door) doors++;
                else if (level.TileAt(cell) == Tile.Switch) buttons++;
            }
            Assert.That(doors, Is.EqualTo(2));
            Assert.That(buttons, Is.EqualTo(2));
        }
    }
}
