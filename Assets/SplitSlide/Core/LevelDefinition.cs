using System;
using System.Collections.Generic;

namespace SplitSlide.Core
{
    /// <summary>
    /// ASCII rows are top to bottom. A lowercase letter (a-d) is a button and its uppercase
    /// letter is the one door it controls: a/A, b/B, c/C, d/D.
    /// </summary>
    public sealed class LevelDefinition
    {
        public const int MaxPairs = 4;
        private readonly Tile[,] tiles;
        private readonly int[,] pairIndices;
        public int Width { get; }
        public int Height { get; }
        public Cell Start { get; }
        public IReadOnlyList<Cell> Goals { get; }
        public bool InitiallyOpen { get; }

        public LevelDefinition(string[] rows, bool initiallyOpen = false)
        {
            if (rows == null || rows.Length == 0 || string.IsNullOrEmpty(rows[0]))
                throw new ArgumentException("A level needs non-empty rows.", nameof(rows));

            Width = rows[0].Length;
            Height = rows.Length;
            tiles = new Tile[Width, Height];
            pairIndices = new int[Width, Height];
            var starts = new List<Cell>();
            var goals = new List<Cell>();
            var buttons = new int[MaxPairs];
            var doors = new int[MaxPairs];
            for (int row = 0; row < Height; row++)
            {
                if (rows[row] == null || rows[row].Length != Width)
                    throw new ArgumentException("Every row must have the same width.", nameof(rows));
                for (int x = 0; x < Width; x++)
                {
                    var cell = new Cell(x, Height - 1 - row);
                    char symbol = rows[row][x];
                    Tile tile;
                    int pair = -1;
                    switch (symbol)
                    {
                        case '#': tile = Tile.Wall; break;
                        case '.': tile = Tile.Floor; break;
                        case 'T': tile = Tile.Floor; starts.Add(cell); break;
                        case 'P': tile = Tile.Goal; goals.Add(cell); break;
                        case 'S': tile = Tile.Splitter; break;
                        default:
                            if (symbol >= 'a' && symbol < 'a' + MaxPairs)
                            {
                                tile = Tile.Switch;
                                pair = symbol - 'a';
                                buttons[pair]++;
                            }
                            else if (symbol >= 'A' && symbol < 'A' + MaxPairs)
                            {
                                tile = Tile.Door;
                                pair = symbol - 'A';
                                doors[pair]++;
                            }
                            else
                            {
                                throw new ArgumentException($"Unknown tile '{symbol}' at {cell}.");
                            }
                            break;
                    }
                    tiles[cell.X, cell.Y] = tile;
                    pairIndices[cell.X, cell.Y] = pair;
                }
            }
            if (starts.Count != 1) throw new ArgumentException("A level needs exactly one T (start).");
            if (goals.Count < 1 || goals.Count > 2)
                throw new ArgumentException("A level needs one or two P (goal).");
            for (int pair = 0; pair < MaxPairs; pair++)
            {
                if (buttons[pair] == 0 && doors[pair] == 0) continue;
                if (buttons[pair] == 1 && doors[pair] == 1) continue;
                throw new ArgumentException(
                    $"Button '{(char)('a' + pair)}' and door '{(char)('A' + pair)}' must appear exactly once each.");
            }
            Start = starts[0];
            Goals = goals.AsReadOnly();
            InitiallyOpen = initiallyOpen;
        }

        public bool Contains(Cell cell) => cell.X >= 0 && cell.X < Width && cell.Y >= 0 && cell.Y < Height;
        public Tile TileAt(Cell cell) => Contains(cell) ? tiles[cell.X, cell.Y] : Tile.Wall;
        public int PairIndexAt(Cell cell) => Contains(cell) ? pairIndices[cell.X, cell.Y] : -1;
        public bool CanEnter(Cell cell, IReadOnlyList<bool> doorsOpen)
        {
            if (!Contains(cell)) return false;
            Tile tile = TileAt(cell);
            if (tile == Tile.Wall) return false;
            if (tile != Tile.Door) return true;
            int pair = PairIndexAt(cell);
            return doorsOpen != null && pair >= 0 && doorsOpen[pair];
        }
    }
}
