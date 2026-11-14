using System;

namespace SplitSlide.Core
{
    /// <summary>Grid coordinates: x grows right, y grows up. Independent of Unity/physics.</summary>
    public readonly struct Cell : IEquatable<Cell>
    {
        public int X { get; }
        public int Y { get; }

        public Cell(int x, int y) { X = x; Y = y; }
        public bool Equals(Cell other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is Cell other && Equals(other);
        public override int GetHashCode() => unchecked(X * 397 ^ Y);
        public override string ToString() => $"({X}, {Y})";
        public static Cell operator +(Cell a, Cell b) => new Cell(a.X + b.X, a.Y + b.Y);
        public static Cell operator -(Cell a, Cell b) => new Cell(a.X - b.X, a.Y - b.Y);
        public static bool operator ==(Cell a, Cell b) => a.Equals(b);
        public static bool operator !=(Cell a, Cell b) => !a.Equals(b);
    }

    public enum Direction { Up, Down, Left, Right }
    public enum Tile { Floor, Wall, Splitter, Switch, Door, Goal }

    public static class Directions
    {
        public static Cell Offset(Direction direction)
        {
            switch (direction)
            {
                case Direction.Up: return new Cell(0, 1);
                case Direction.Down: return new Cell(0, -1);
                case Direction.Left: return new Cell(-1, 0);
                case Direction.Right: return new Cell(1, 0);
                default: throw new ArgumentOutOfRangeException(nameof(direction));
            }
        }
    }
}
