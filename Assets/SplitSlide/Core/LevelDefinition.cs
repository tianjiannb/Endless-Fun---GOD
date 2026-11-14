using System;
using System.Collections.Generic;

namespace SplitSlide.Core
{
    /// <summary>ASCII rows are top to bottom. All switches share the same door channel.</summary>
    public sealed class LevelDefinition
    {
        private readonly Tile[,] tiles;
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
            var starts = new List<Cell>();
            var goals = new List<Cell>();
            for (int row = 0; row < Height; row++)
            {
                if (rows[row] == null || rows[row].Length != Width)
                    throw new ArgumentException("Every row must have the same width.", nameof(rows));
                for (int x = 0; x < Width; x++)
                {
                    var cell = new Cell(x, Height - 1 - row);
                    Tile tile;
                    switch (rows[row][x])
                    {
                        case '#': tile = Tile.Wall; break;
                        case '.': tile = Tile.Floor; break;
                        case 'P': tile = Tile.Floor; starts.Add(cell); break;
                        case 'S': tile = Tile.Splitter; break;
                        case 'T': tile = Tile.Switch; break;
                        case 'D': tile = Tile.Door; break;
                        case 'G': tile = Tile.Goal; goals.Add(cell); break;
                        default: throw new ArgumentException($"Unknown tile '{rows[row][x]}' at {cell}.");
                    }
                    tiles[cell.X, cell.Y] = tile;
                }
            }
            if (starts.Count != 1) throw new ArgumentException("A level needs exactly one P (start).");
            if (goals.Count < 1 || goals.Count > 2)
                throw new ArgumentException("A level needs one or two G (goals).");
            Start = starts[0];
            Goals = goals.AsReadOnly();
            InitiallyOpen = initiallyOpen;
        }

        public bool Contains(Cell cell) => cell.X >= 0 && cell.X < Width && cell.Y >= 0 && cell.Y < Height;
        public Tile TileAt(Cell cell) => Contains(cell) ? tiles[cell.X, cell.Y] : Tile.Wall;
        public bool CanEnter(Cell cell, bool doorsOpen) => Contains(cell) && TileAt(cell) != Tile.Wall &&
                                                        (TileAt(cell) != Tile.Door || doorsOpen);
    }
}
