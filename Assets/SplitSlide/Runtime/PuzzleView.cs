using System;
using System.Collections;
using System.Collections.Generic;
using SplitSlide.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using EndlessFun.UI;

namespace SplitSlide
{
    /// <summary>
    /// Transparent artwork over tiled floors, with a minimal HUD and no labels over game pieces.
    /// </summary>
    public sealed class PuzzleView : MonoBehaviour
    {
        private static readonly Color Ink = new Color32(35, 66, 58, 255);
        private static readonly Color Mint = new Color32(28, 94, 67, 255);
        private static readonly Color Gold = new Color32(122, 82, 29, 255);
        private static readonly Color Floor = new Color32(22, 34, 47, 255);
        private static readonly Color Wall = new Color32(45, 60, 75, 255);
        private static readonly Color Control = new Color32(74, 107, 92, 255);
        private static readonly Color KeyHeld = new Color32(126, 166, 145, 255);
        private static readonly Color Bad = new Color32(154, 57, 54, 255);

        private readonly List<RectTransform> bodies = new List<RectTransform>();
        private readonly Dictionary<Cell, Image> tiles = new Dictionary<Cell, Image>();
        private readonly Dictionary<Cell, Image> fixtures = new Dictionary<Cell, Image>();
        private readonly Dictionary<Cell, Coroutine> wallBumps = new Dictionary<Cell, Coroutine>();
        private readonly List<Cell> bodyCells = new List<Cell>();
        private readonly List<Vector2> trailAt = new List<Vector2>();
        private readonly Dictionary<Cell, float> floorPulse = new Dictionary<Cell, float>();
        private float rippleTime;
        private readonly HashSet<Cell> pulsingFixtures = new HashSet<Cell>();
        private readonly HashSet<Cell> pulsingDoors = new HashSet<Cell>();
        private readonly bool[] doorOpenLast = new bool[LevelDefinition.MaxPairs];
        private readonly bool[] doorKnown = new bool[LevelDefinition.MaxPairs];
        private readonly List<Button> levelButtons = new List<Button>();
        private readonly List<Direction> moveButtons = new List<Direction>();
        private readonly List<Image> moveImages = new List<Image>();
        private RectTransform root;
        private RectTransform board;
        private Image boardPanel;
        private Font font;
        private PuzzleArtwork artwork;
        private LevelDefinition level;
        private float cellSize;
        private Text moves;
        private Text status;
        private Text hintText;
        private RectTransform hintPanel;
        private CanvasGroup hintGroup;
        private Coroutine hintRoutine;
        private Button undoButton;
        private Coroutine statusRoutine;
        private bool wonShown;
        private Image fadeOverlay;
        private EventSystem ownedEventSystem;

        public void Build(PuzzleLevelLibrary library, Action undo, Action reset,
                          Action<int> select, Action<Direction> move)
        {
            artwork = PuzzleArtwork.Load();
            font = GameUiFont.Load();
            var canvasObject = new GameObject("Puzzle UI", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 800);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            root = Rect("Content", canvasObject.transform, Vector2.zero, new Vector2(1280, 800));
            Panel("Background", root, Vector2.zero, new Vector2(4000, 4000), new Color32(173, 187, 165, 255));
            RawImage backdrop = Rect("Leaf Backdrop", root, Vector2.zero, new Vector2(1600, 1600))
                .gameObject.AddComponent<RawImage>();
            backdrop.texture = artwork.Backdrop.texture;
            backdrop.uvRect = new Rect(0, 0, 1, 1);
            backdrop.color = new Color(1, 1, 1, .18f);
            backdrop.raycastTarget = false;

            undoButton = MakeButton("撤销 [Z]", root, new Vector2(-575, 370), new Vector2(112, 34), undo, 16);
            MakeButton("重置 [R]", root, new Vector2(-455, 370), new Vector2(112, 34), reset, 16);
            moves = Label("", root, new Vector2(490, 370), new Vector2(240, 34), 18, Ink, TextAnchor.MiddleRight);
            MakeButton("提示", root, new Vector2(584, 316), new Vector2(76, 38), ToggleHint, 16);

            boardPanel = Panel("Board", root, new Vector2(0, 20), new Vector2(700, 480), new Color32(147, 165, 142, 255));
            board = Rect("Board Grid", root, new Vector2(0, 20), new Vector2(660, 440));

            hintPanel = Rect("Hint", root, new Vector2(0, -374), new Vector2(820, 52));
            Panel("Hint Panel", hintPanel, Vector2.zero, new Vector2(820, 52), new Color32(202, 215, 193, 250));
            hintText = Label("", hintPanel, Vector2.zero, new Vector2(780, 44), 16, Ink);
            hintGroup = hintPanel.gameObject.AddComponent<CanvasGroup>();
            hintGroup.alpha = 0;
            hintPanel.gameObject.SetActive(false);

            status = Label("", root, new Vector2(0, -330), new Vector2(900, 40), 17, Mint);
            AddMoveButton("W", new Vector2(564, -274), Direction.Up, move);
            AddMoveButton("A", new Vector2(516, -318), Direction.Left, move);
            AddMoveButton("S", new Vector2(564, -318), Direction.Down, move);
            AddMoveButton("D", new Vector2(612, -318), Direction.Right, move);

            float buttonWidth = Mathf.Max(20, Mathf.Min(38, 300f / library.levels.Length - 8));
            for (int i = 0; i < library.levels.Length; i++)
            {
                int index = i;
                float x = (i - (library.levels.Length - 1) * .5f) * (buttonWidth + 8);
                levelButtons.Add(MakeButton((i + 1).ToString(), root, new Vector2(x, 370),
                    new Vector2(buttonWidth, 24), () => select(index)));
            }

            if (EventSystem.current == null)
            {
                var events = new GameObject("Puzzle Event System", typeof(EventSystem), typeof(StandaloneInputModule));
                ownedEventSystem = events.GetComponent<EventSystem>();
                // Arrow keys belong to the puzzle, not UI navigation. Mouse buttons remain usable.
                ownedEventSystem.sendNavigationEvents = false;
            }
        }

        public void ShowLevel(LevelDefinition definition, PuzzleLevelEntry entry, int index)
        {
            level = definition;
            foreach (Coroutine bump in wallBumps.Values)
                if (bump != null) StopCoroutine(bump);
            wallBumps.Clear();
            for (int i = board.childCount - 1; i >= 0; i--)
            {
                board.GetChild(i).gameObject.SetActive(false);
                Destroy(board.GetChild(i).gameObject);
            }
            bodies.Clear(); tiles.Clear(); fixtures.Clear();
            bodyCells.Clear();
            trailAt.Clear();
            floorPulse.Clear();
            pulsingFixtures.Clear();
            pulsingDoors.Clear();
            for (int i = 0; i < doorKnown.Length; i++)
            {
                doorKnown[i] = false;
                doorOpenLast[i] = false;
            }
            cellSize = Mathf.Min(1100f / level.Width, 632f / level.Height);
            Vector2 size = new Vector2(cellSize * level.Width, cellSize * level.Height);
            board.sizeDelta = size;
            boardPanel.rectTransform.sizeDelta = size + new Vector2(24, 24);
            for (int y = 0; y < level.Height; y++)
            for (int x = 0; x < level.Width; x++)
            {
                var cell = new Cell(x, y);
                Tile tile = level.TileAt(cell);
                Image image = Panel($"{tile} {cell}", board, Position(cell), Vector2.one * cellSize * (tile == Tile.Wall ? 1.07f : .94f),
                    tile == Tile.Wall ? Wall : Floor);
                image.sprite = tile == Tile.Wall ? artwork.Wall : artwork.Floor;
                // Keep the leaf floor quieter than the player and interactive artwork above it.
                image.color = new Color(1f, 1f, 1f, tile == Tile.Wall ? .62f : .38f);
                image.preserveAspect = true;
                tiles.Add(cell, image);
                if (tile == Tile.Floor || tile == Tile.Wall) continue;
                Image prop = Panel($"{tile} Artwork", image.transform, Vector2.zero,
                    Vector2.one * cellSize * 1.05f, Color.white);
                prop.preserveAspect = true;
                prop.sprite = tile == Tile.Goal ? artwork.Goal : tile == Tile.Splitter ? artwork.Splitter
                    : tile == Tile.Switch ? artwork.SwitchOff : artwork.DoorClosed;
                fixtures.Add(cell, prop);
            }
            BuildPairLinks();
            hintText.text = entry.hint;
            if (hintRoutine != null) StopCoroutine(hintRoutine);
            hintRoutine = null;
            hintGroup.alpha = 0;
            hintPanel.gameObject.SetActive(false);
            StopStatus();
            wonShown = false;
            for (int i = 0; i < levelButtons.Count; i++) levelButtons[i].interactable = i != index;
        }

        public void Apply(PuzzleSnapshot state, bool canUndo, bool busy, bool hasNext)
        {
            EnsureBodies(state);
            for (int i = 0; i < bodies.Count; i++)
            {
                bodies[i].anchoredPosition = Position(state.Bodies[i]);
                if (!busy) bodies[i].localScale = Vector3.one;
            }
            foreach (var pair in fixtures)
            {
                Tile tile = level.TileAt(pair.Key);
                if (tile == Tile.Door)
                {
                    int index = level.PairIndexAt(pair.Key);
                    bool open = state.IsDoorOpen(index);
                    pair.Value.sprite = open ? artwork.DoorOpen : artwork.DoorClosed;
                    if (doorKnown[index] && doorOpenLast[index] != open && !pulsingDoors.Contains(pair.Key))
                        StartCoroutine(DoorFeedback(pair.Key, pair.Value, open));
                    doorKnown[index] = true;
                    doorOpenLast[index] = open;
                }
                else if (tile == Tile.Switch)
                {
                    int index = level.PairIndexAt(pair.Key);
                    pair.Value.sprite = state.IsDoorOpen(index) ? artwork.SwitchOn : artwork.SwitchOff;
                }
                else if (tile == Tile.Splitter && !state.HasSplit)
                {
                    // Restore the flower after undo or reset.
                    pair.Value.gameObject.SetActive(true);
                    pair.Value.rectTransform.sizeDelta = Vector2.one * cellSize * 1.05f;
                    pair.Value.color = Color.white;
                }
            }
            PulseTouchedFixtures(state);
            moves.text = $"已走 {state.MoveCount:00} 步";
            undoButton.interactable = canUndo;
            if (state.Won)
            {
                if (wonShown) return;
                wonShown = true;
                StopStatus();
                status.text = "";
            }
            else if (wonShown)
            {
                wonShown = false;
                status.text = "";
            }
        }

        public void Interpolate(PuzzleSnapshot from, PuzzleSnapshot to, float progress, bool settling = false)
        {
            for (int i = 0; i < from.Bodies.Count; i++)
            {
                Vector2 start = Position(from.Bodies[i]);
                Vector2 end = Position(to.Bodies[i]);
                bodies[i].anchoredPosition = Vector2.Lerp(start, end, progress);
                ApplyStretch(bodies[i], end - start, settling ? progress : 0);
                EmitTrail(i, end - start);
            }
        }

        /// <summary>Leaves a small fading droplet behind the body while it slides.</summary>
        private void EmitTrail(int index, Vector2 delta)
        {
            if (delta.sqrMagnitude < 1f || index >= bodies.Count) return;
            while (trailAt.Count <= index) trailAt.Add(Vector2.positiveInfinity);
            Vector2 position = bodies[index].anchoredPosition;
            if (Vector2.Distance(position, trailAt[index]) < cellSize * .3f) return;
            trailAt[index] = position;
            StartCoroutine(TrailDot(position));
        }

        private IEnumerator TrailDot(Vector2 position)
        {
            Image dot = Panel("Trail", board, position, Vector2.one * cellSize * .3f, new Color(1f, 1f, 1f, .32f));
            dot.sprite = artwork.Player;
            dot.preserveAspect = true;
            dot.raycastTarget = false;
            if (bodies.Count > 0 && bodies[0] != null)
                dot.rectTransform.SetSiblingIndex(bodies[0].GetSiblingIndex());
            const float duration = .3f;
            for (float time = 0; time < duration; time += Time.unscaledDeltaTime)
            {
                if (dot == null) yield break;
                float t = Mathf.Clamp01(time / duration);
                dot.color = new Color(1f, 1f, 1f, Mathf.Lerp(.32f, 0f, t));
                dot.rectTransform.sizeDelta = Vector2.one * cellSize * Mathf.Lerp(.3f, .14f, t);
                yield return null;
            }
            if (dot != null) Destroy(dot.gameObject);
        }

        /// <summary>
        /// Stretches the body along its travel axis and squeezes it across that axis. The final
        /// cell of a slide relaxes back to a circle so the droplet settles instead of popping.
        /// </summary>
        private static void ApplyStretch(RectTransform body, Vector2 delta, float settleProgress)
        {
            if (delta.sqrMagnitude < .01f)
            {
                body.localScale = Vector3.one;
                return;
            }
            const float stretch = 1.22f;
            const float squash = .84f;
            float along = Mathf.Lerp(stretch, 1f, settleProgress);
            float across = Mathf.Lerp(squash, 1f, settleProgress);
            body.localScale = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)
                ? new Vector3(along, across, 1f)
                : new Vector3(across, along, 1f);
        }

        public void ToggleHint()
        {
            if (hintPanel == null) return;
            bool visible = hintPanel.gameObject.activeSelf && hintGroup.alpha > .01f;
            if (hintRoutine != null) StopCoroutine(hintRoutine);
            hintPanel.gameObject.SetActive(true);
            hintRoutine = StartCoroutine(FadeHint(!visible, !visible));
        }

        /// <summary>Fades the hint in, keeps it for a few seconds, then fades it out.</summary>
        private IEnumerator FadeHint(bool show, bool autoHide)
        {
            const float fade = .25f;
            float from = hintGroup.alpha;
            float to = show ? 1f : 0f;
            for (float time = 0; time < fade; time += Time.unscaledDeltaTime)
            {
                hintGroup.alpha = Mathf.Lerp(from, to, time / fade);
                yield return null;
            }
            hintGroup.alpha = to;
            if (autoHide)
            {
                yield return new WaitForSecondsRealtime(4f);
                for (float time = 0; time < fade; time += Time.unscaledDeltaTime)
                {
                    hintGroup.alpha = Mathf.Lerp(1f, 0f, time / fade);
                    yield return null;
                }
                hintGroup.alpha = 0;
                hintPanel.gameObject.SetActive(false);
            }
            hintRoutine = null;
        }

        /// <summary>Nudges every wall tile directly in front of a body outward, then settles it back.</summary>
        public void BumpWalls(PuzzleSnapshot state, Direction direction)
        {
            if (level == null) return;
            Cell step = Directions.Offset(direction);
            foreach (Cell body in state.Bodies)
            {
                Cell hit = body + step;
                if (!level.Contains(hit) || level.TileAt(hit) != Tile.Wall) continue;
                if (!tiles.TryGetValue(hit, out Image wall)) continue;
                if (wallBumps.TryGetValue(hit, out Coroutine running) && running != null)
                    StopCoroutine(running);
                wallBumps[hit] = StartCoroutine(BumpWall(wall.rectTransform, Position(hit), DirectionVector(direction)));
            }
        }

        public void PlayWin()
        {
            for (int i = 0; i < bodies.Count; i++)
                if (bodies[i] != null) StartCoroutine(WinPulse(i));
        }

        private IEnumerator WinPulse(int index)
        {
            const float duration = .5f;
            for (float time = 0; time < duration; time += Time.unscaledDeltaTime)
            {
                if (index >= bodies.Count || bodies[index] == null) yield break;
                float scale = 1f + Mathf.Sin(Mathf.Clamp01(time / duration) * Mathf.PI) * .35f;
                bodies[index].localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            if (index < bodies.Count && bodies[index] != null) bodies[index].localScale = Vector3.one;
        }

        public IEnumerator FadeToBlack(float duration)
        {
            if (fadeOverlay == null)
            {
                fadeOverlay = Panel("Fade", root, Vector2.zero, new Vector2(4000, 4000), Color.clear);
                fadeOverlay.raycastTarget = false;
            }
            fadeOverlay.transform.SetAsLastSibling();
            for (float time = 0; time < duration; time += Time.unscaledDeltaTime)
            {
                fadeOverlay.color = new Color(0f, 0f, 0f, Mathf.Clamp01(time / duration));
                yield return null;
            }
            fadeOverlay.color = Color.black;
        }

        public IEnumerator FadeFromBlack(float duration)
        {
            if (fadeOverlay == null) yield break;
            for (float time = 0; time < duration; time += Time.unscaledDeltaTime)
            {
                fadeOverlay.color = new Color(0f, 0f, 0f, 1f - Mathf.Clamp01(time / duration));
                yield return null;
            }
            fadeOverlay.color = Color.clear;
        }

        public void ShowBlocked()
        {
            if (wonShown) return;
            if (statusRoutine != null) StopCoroutine(statusRoutine);
            status.color = Gold;
            status.text = "这个方向被挡住了，换个方向试试。";
            statusRoutine = StartCoroutine(ClearStatusAfter(1.2f));
        }

        public void ShowError(string message)
        {
            StopStatus();
            status.color = Bad;
            status.text = message;
        }

        private IEnumerator BumpWall(RectTransform wall, Vector2 home, Vector2 direction)
        {
            const float duration = .2f;
            float distance = Mathf.Max(4f, cellSize * .1f);
            float time = 0;
            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / duration);
                // Quick push out, slower settle back.
                float ease = t < .35f
                    ? EaseOutQuad(t / .35f)
                    : 1f - EaseInOutCubic((t - .35f) / .65f);
                wall.anchoredPosition = home + direction * (distance * ease);
                yield return null;
            }
            wall.anchoredPosition = home;
        }

        private static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);

        private static float EaseInOutCubic(float t) =>
            t < .5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) * .5f;

        private static Vector2 DirectionVector(Direction direction)
        {
            switch (direction)
            {
                case Direction.Up: return Vector2.up;
                case Direction.Down: return Vector2.down;
                case Direction.Left: return Vector2.left;
                default: return Vector2.right;
            }
        }

        private IEnumerator ClearStatusAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            status.text = "";
            statusRoutine = null;
        }

        private void StopStatus()
        {
            if (statusRoutine != null) StopCoroutine(statusRoutine);
            statusRoutine = null;
            status.text = "";
        }

        private void EnsureBodies(PuzzleSnapshot state)
        {
            while (bodies.Count > state.Bodies.Count)
            {
                int last = bodies.Count - 1;
                bodies[last].gameObject.SetActive(false);
                Destroy(bodies[last].gameObject);
                bodies.RemoveAt(last);
            }
            while (bodies.Count < state.Bodies.Count)
            {
                int index = bodies.Count;
                Image body = Panel($"Body {index + 1}", board, Position(state.Bodies[index]),
                    Vector2.one * cellSize * .70f, Color.white);
                body.sprite = artwork.Player;
                body.preserveAspect = true;
                bodies.Add(body.rectTransform);
                if (index > 0)
                {
                    StartCoroutine(SplitPop(body.rectTransform));
                    StartCoroutine(SplitSplash(Position(state.Bodies[index])));
                    if (fixtures.TryGetValue(state.Bodies[0], out Image flower))
                        StartCoroutine(ConsumeFlower(flower));
                }
            }
        }

        /// <summary>New droplet pops out of the parent with a quick overshoot.</summary>
        private IEnumerator SplitPop(RectTransform body)
        {
            const float duration = .3f;
            for (float time = 0; time < duration; time += Time.unscaledDeltaTime)
            {
                if (body == null) yield break;
                float t = Mathf.Clamp01(time / duration);
                float scale = t < .45f ? Mathf.Lerp(.2f, 1.15f, t / .45f) : Mathf.Lerp(1.15f, 1f, (t - .45f) / .55f);
                body.localScale = new Vector3(scale, scale, 1);
                yield return null;
            }
            if (body != null) body.localScale = Vector3.one;
        }

        /// <summary>Water ring plus flying droplets at the split cell.</summary>
        private IEnumerator SplitSplash(Vector2 position)
        {
            Vector2 core = Vector2.one * cellSize * .70f;
            Image ring = Panel("Split Ring", board, position, core, new Color(1f, 1f, 1f, .85f));
            ring.sprite = artwork.Player;
            ring.preserveAspect = true;

            const int dropCount = 6;
            var drops = new Image[dropCount];
            var angles = new float[dropCount];
            for (int i = 0; i < dropCount; i++)
            {
                angles[i] = i / (float)dropCount * Mathf.PI * 2f + .35f;
                drops[i] = Panel($"Split Drop {i}", board, position, core * .16f, new Color(1f, 1f, 1f, .9f));
                drops[i].sprite = artwork.Player;
                drops[i].preserveAspect = true;
            }

            const float duration = .5f;
            for (float time = 0; time < duration; time += Time.unscaledDeltaTime)
            {
                if (ring == null) yield break;
                float t = Mathf.Clamp01(time / duration);
                float ease = EaseOutQuad(t);
                ring.rectTransform.sizeDelta = core * Mathf.Lerp(.6f, 2.8f, ease);
                ring.color = new Color(1f, 1f, 1f, Mathf.Lerp(.85f, 0f, t));
                float travel = cellSize * .95f * ease;
                for (int i = 0; i < dropCount; i++)
                {
                    if (drops[i] == null) continue;
                    drops[i].rectTransform.anchoredPosition = position +
                        new Vector2(Mathf.Cos(angles[i]), Mathf.Sin(angles[i])) * travel;
                    drops[i].color = new Color(1f, 1f, 1f, Mathf.Lerp(.9f, 0f, Mathf.Clamp01(t * 1.3f)));
                }
                yield return null;
            }
            if (ring != null) Destroy(ring.gameObject);
            foreach (Image drop in drops)
                if (drop != null) Destroy(drop.gameObject);
        }

        /// <summary>Flower swells, then shrinks and fades away once it has been used.</summary>
        private IEnumerator ConsumeFlower(Image flower)
        {
            Vector2 size = flower.rectTransform.sizeDelta;
            const float duration = .38f;
            for (float time = 0; time < duration; time += Time.unscaledDeltaTime)
            {
                if (flower == null) yield break;
                float t = Mathf.Clamp01(time / duration);
                float scale = t < .3f ? Mathf.Lerp(1f, 1.2f, t / .3f) : Mathf.Lerp(1.2f, 0f, (t - .3f) / .7f);
                flower.rectTransform.sizeDelta = size * scale;
                Color color = flower.color;
                color.a = Mathf.Lerp(1f, 0f, Mathf.Clamp01((t - .2f) / .8f));
                flower.color = color;
                yield return null;
            }
            if (flower != null) flower.gameObject.SetActive(false);
        }

        /// <summary>Dotted line between each switch and the door it controls.</summary>
        private void BuildPairLinks()
        {
            for (int y = 0; y < level.Height; y++)
            for (int x = 0; x < level.Width; x++)
            {
                var switchCell = new Cell(x, y);
                if (level.TileAt(switchCell) != Tile.Switch) continue;
                int pair = level.PairIndexAt(switchCell);
                Cell doorCell = FindDoor(pair);
                if (doorCell.X < 0) continue;
                Vector2 from = Position(switchCell);
                Vector2 to = Position(doorCell);
                float length = Vector2.Distance(from, to);
                int count = Mathf.Max(3, Mathf.RoundToInt(length / (cellSize * .24f)));
                for (int i = 1; i < count; i++)
                {
                    float t = i / (float)count;
                    if (t < .16f || t > .84f) continue;
                    Panel("Pair Link", board, Vector2.Lerp(from, to, t), Vector2.one * cellSize * .1f,
                        new Color(.24f, .52f, .92f, .6f));
                }
            }
        }

        private Cell FindDoor(int pair)
        {
            for (int y = 0; y < level.Height; y++)
            for (int x = 0; x < level.Width; x++)
            {
                var cell = new Cell(x, y);
                if (level.TileAt(cell) == Tile.Door && level.PairIndexAt(cell) == pair) return cell;
            }
            return new Cell(-1, -1);
        }

        private void PulseTouchedFixtures(PuzzleSnapshot state)
        {
            while (bodyCells.Count < state.Bodies.Count)
                bodyCells.Add(new Cell(int.MinValue, int.MinValue));
            while (bodyCells.Count > state.Bodies.Count)
                bodyCells.RemoveAt(bodyCells.Count - 1);
            for (int i = 0; i < state.Bodies.Count; i++)
            {
                Cell cell = state.Bodies[i];
                if (cell == bodyCells[i]) continue;
                bodyCells[i] = cell;
                Tile tile = level.TileAt(cell);
                if (tile == Tile.Floor)
                {
                    floorPulse[cell] = Time.unscaledTime;
                    continue;
                }
                if (tile != Tile.Switch && tile != Tile.Goal) continue;
                if (fixtures.TryGetValue(cell, out Image fixture) && !pulsingFixtures.Contains(cell))
                    StartCoroutine(PulseFixture(cell, fixture));
            }
        }

        private IEnumerator PulseFixture(Cell cell, Image fixture)
        {
            pulsingFixtures.Add(cell);
            Vector2 size = fixture.rectTransform.sizeDelta;
            const float duration = .26f;
            for (float time = 0; time < duration; time += Time.unscaledDeltaTime)
            {
                if (fixture == null) break;
                float t = Mathf.Clamp01(time / duration);
                float scale = t < .4f ? Mathf.Lerp(1f, 1.22f, t / .4f) : Mathf.Lerp(1.22f, 1f, (t - .4f) / .6f);
                fixture.rectTransform.sizeDelta = size * scale;
                yield return null;
            }
            if (fixture != null) fixture.rectTransform.sizeDelta = size;
            pulsingFixtures.Remove(cell);
        }

        /// <summary>Door springs open with a mint glow, or snaps shut with a dull flash.</summary>
        private IEnumerator DoorFeedback(Cell cell, Image door, bool open)
        {
            pulsingDoors.Add(cell);
            Vector2 size = door.rectTransform.sizeDelta;
            const float duration = .34f;
            for (float time = 0; time < duration; time += Time.unscaledDeltaTime)
            {
                if (door == null) break;
                float t = Mathf.Clamp01(time / duration);
                float peak = open ? 1.28f : .8f;
                float scale = t < .35f ? Mathf.Lerp(1f, peak, t / .35f) : Mathf.Lerp(peak, 1f, (t - .35f) / .65f);
                door.rectTransform.sizeDelta = size * scale;
                float flash = Mathf.Sin(t * Mathf.PI);
                door.color = open
                    ? Color.Lerp(Color.white, new Color(.7f, 1f, .82f), flash)
                    : Color.Lerp(Color.white, new Color(.45f, .52f, .45f), flash);
                yield return null;
            }
            if (door != null)
            {
                door.rectTransform.sizeDelta = size;
                door.color = Color.white;
            }
            pulsingDoors.Remove(cell);
        }

        private Vector2 Position(Cell cell) => new Vector2((cell.X - (level.Width - 1) * .5f) * cellSize,
            (cell.Y - (level.Height - 1) * .5f) * cellSize);

        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var item = new GameObject(name, typeof(RectTransform));
            var rect = item.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static Image Panel(string name, Transform parent, Vector2 position, Vector2 size, Color color)
        {
            Image image = Rect(name, parent, position, size).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private Text Label(string text, Transform parent, Vector2 position, Vector2 size, int fontSize,
                           Color color, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            Text label = Rect("Label", parent, position, size).gameObject.AddComponent<Text>();
            label.font = font;
            label.text = text;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = alignment;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }

        private Button MakeButton(string text, Transform parent, Vector2 position, Vector2 size, Action action,
                                  int fontSize = 14)
        {
            Image image = Panel(text, parent, position, size, Control);
            image.raycastTarget = true;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(.78f, 1f, .93f);
            colors.pressedColor = new Color(.55f, .8f, .72f);
            colors.disabledColor = new Color(.45f, .5f, .55f, .6f);
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() => action());
            Label(text, image.transform, Vector2.zero, size, fontSize, new Color32(240, 244, 231, 255));
            return button;
        }

        private void AddMoveButton(string text, Vector2 position, Direction direction, Action<Direction> move)
        {
            Button button = MakeButton(text, root, position, new Vector2(44, 42), () => move(direction), 18);
            moveButtons.Add(direction);
            moveImages.Add(button.image);
        }

        private void Update()
        {
            for (int i = 0; i < moveButtons.Count; i++)
                if (moveImages[i] != null)
                    moveImages[i].color = Held(moveButtons[i]) ? KeyHeld : Control;
            if (level == null) return;
            rippleTime += Time.unscaledDeltaTime;
            foreach (KeyValuePair<Cell, Image> pair in tiles)
            {
                if (pair.Value == null || level.TileAt(pair.Key) != Tile.Floor) continue;
                float phase = rippleTime * 2f + (pair.Key.X + pair.Key.Y) * .9f;
                float wobble = 1f + Mathf.Sin(phase) * .03f;
                if (floorPulse.TryGetValue(pair.Key, out float started))
                {
                    float t = (Time.unscaledTime - started) / .32f;
                    if (t >= 1f) floorPulse.Remove(pair.Key);
                    else wobble += Mathf.Sin(t * Mathf.PI) * .09f;
                }
                pair.Value.rectTransform.localScale = new Vector3(wobble, wobble, 1f);
                pair.Value.rectTransform.anchoredPosition = Position(pair.Key) + new Vector2(0f, Mathf.Cos(phase) * .9f);
            }
        }

        private static bool Held(Direction direction)
        {
            switch (direction)
            {
                case Direction.Up: return Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
                case Direction.Down: return Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
                case Direction.Left: return Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
                default: return Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
            }
        }

        private void OnDestroy()
        {
            artwork?.Release();
            if (ownedEventSystem != null) Destroy(ownedEventSystem.gameObject);
        }
    }
}
