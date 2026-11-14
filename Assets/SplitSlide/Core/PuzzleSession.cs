using System;
using System.Collections.Generic;

namespace SplitSlide.Core
{
    /// <summary>An immutable state used by undo, animation playback, tests and tools.</summary>
    public sealed class PuzzleSnapshot
    {
        public IReadOnlyList<Cell> Bodies { get; }
        public IReadOnlyList<bool> DoorsOpen { get; }
        public bool HasSplit { get; }
        public bool Won { get; }
        public int MoveCount { get; }

        internal PuzzleSnapshot(List<Cell> bodies, bool[] doorsOpen, bool hasSplit, bool won, int moveCount)
        {
            Bodies = Array.AsReadOnly(bodies.ToArray());
            var doors = new bool[doorsOpen.Length];
            Array.Copy(doorsOpen, doors, doorsOpen.Length);
            DoorsOpen = Array.AsReadOnly(doors);
            HasSplit = hasSplit;
            Won = won;
            MoveCount = moveCount;
        }

        public bool IsDoorOpen(int pair) => pair >= 0 && pair < DoorsOpen.Count && DoorsOpen[pair];
    }

    public sealed class SlideResult
    {
        public PuzzleSnapshot Before { get; }
        public PuzzleSnapshot After { get; }
        public IReadOnlyList<PuzzleSnapshot> Frames { get; }
        internal SlideResult(PuzzleSnapshot before, PuzzleSnapshot after, List<PuzzleSnapshot> frames)
        {
            Before = before;
            After = after;
            Frames = frames.AsReadOnly();
        }
    }

    /// <summary>
    /// Resolves a whole input as simultaneous grid steps, never using physics or frame time.
    /// A body that stops stays stopped until the NEXT input, even if a door subsequently opens.
    /// Each button (a-d) toggles only its paired door (A-D).
    /// </summary>
    public sealed class PuzzleSession
    {
        private readonly List<Cell> bodies = new List<Cell>(2);
        private readonly Stack<PuzzleSnapshot> history = new Stack<PuzzleSnapshot>();
        private readonly bool[] doorsOpen = new bool[LevelDefinition.MaxPairs];
        private bool hasSplit;
        private bool won;
        private int moveCount;
        public LevelDefinition Level { get; }
        public bool CanUndo => history.Count > 0;
        public PuzzleSnapshot State => Capture();

        public PuzzleSession(LevelDefinition level)
        {
            Level = level ?? throw new ArgumentNullException(nameof(level));
            Reset();
        }

        public void Reset()
        {
            bodies.Clear();
            bodies.Add(Level.Start);
            for (int pair = 0; pair < doorsOpen.Length; pair++) doorsOpen[pair] = Level.InitiallyOpen;
            hasSplit = false;
            won = false;
            moveCount = 0;
            history.Clear();
        }

        public bool Undo()
        {
            if (!CanUndo) return false;
            Restore(history.Pop());
            return true;
        }

        /// <returns>Null for a blocked input or a completed level; these do not consume undo.</returns>
        public SlideResult Slide(Direction direction)
        {
            Cell delta = Directions.Offset(direction);
            if (won) return null;
            var before = Capture();
            var frames = new List<PuzzleSnapshot>();
            var active = new[] { true, bodies.Count == 2 };
            var moved = new bool[2];
            var switchEntries = new int[LevelDefinition.MaxPairs];
            moveCount++;

            while (active[0] || active[1])
            {
                // Leading body first: a trailing body may enter a cell vacated IN THIS step.
                int first = 0;
                if (bodies.Count == 2 && Projection(bodies[1], delta) > Projection(bodies[0], delta)) first = 1;
                int second = 1 - first;
                var advance = new bool[2];
                advance[first] = CanAdvance(first, delta, active, advance);
                if (bodies.Count == 2) advance[second] = CanAdvance(second, delta, active, advance);

                bool changed = false;
                Array.Clear(switchEntries, 0, switchEntries.Length);
                int countBeforeSplit = bodies.Count;
                for (int i = 0; i < countBeforeSplit; i++)
                {
                    if (!advance[i]) { active[i] = false; continue; }
                    bodies[i] += delta;
                    moved[i] = true;
                    changed = true;
                    if (Level.TileAt(bodies[i]) == Tile.Switch) switchEntries[Level.PairIndexAt(bodies[i])]++;
                }
                // Both bodies inspect the same door state. Only then do entered buttons flip their own doors.
                for (int pair = 0; pair < switchEntries.Length; pair++)
                    if (switchEntries[pair] % 2 == 1) doorsOpen[pair] = !doorsOpen[pair];

                for (int i = 0; i < countBeforeSplit && !hasSplit; i++)
                {
                    if (active[i] || !moved[i] || Level.TileAt(bodies[i]) != Tile.Splitter) continue;
                    Cell rear = bodies[i] - delta;
                    // Invalid authored layouts never create a body inside a wall/closed door.
                    if (!Level.CanEnter(rear, doorsOpen) || bodies.Contains(rear)) continue;
                    bodies.Add(rear);
                    hasSplit = true;
                    active[1] = false; // The new body joins on the NEXT input.
                    changed = true;
                }
                if (changed) frames.Add(Capture());
            }

            if (!moved[0] && !moved[1])
            {
                Restore(before);
                return null;
            }
            won = true;
            foreach (Cell goal in Level.Goals)
                if (!bodies.Contains(goal)) { won = false; break; }
            history.Push(before);
            var after = Capture();
            frames.Add(after);
            return new SlideResult(before, after, frames);
        }

        private bool CanAdvance(int index, Cell delta, bool[] active, bool[] advance)
        {
            if (!active[index]) return false;
            Cell next = bodies[index] + delta;
            if (!Level.CanEnter(next, doorsOpen)) return false;
            for (int other = 0; other < bodies.Count; other++)
                if (other != index && bodies[other] == next && !advance[other]) return false;
            return true;
        }

        private static int Projection(Cell cell, Cell delta) => cell.X * delta.X + cell.Y * delta.Y;
        private PuzzleSnapshot Capture() => new PuzzleSnapshot(bodies, doorsOpen, hasSplit, won, moveCount);
        private void Restore(PuzzleSnapshot snapshot)
        {
            bodies.Clear();
            bodies.AddRange(snapshot.Bodies);
            for (int pair = 0; pair < doorsOpen.Length; pair++)
                doorsOpen[pair] = pair < snapshot.DoorsOpen.Count && snapshot.DoorsOpen[pair];
            hasSplit = snapshot.HasSplit;
            won = snapshot.Won;
            moveCount = snapshot.MoveCount;
        }
    }
}
