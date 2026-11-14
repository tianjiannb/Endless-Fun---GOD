using System;
using System.Collections;
using System.Collections.Generic;
using SplitSlide.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SplitSlide
{
    /// <summary>
    /// Board-first minimal HUD. Tiles are flat placeholders; each button/door pair shares one
    /// accent color so the pairing survives when the placeholders are replaced with sprites.
    /// </summary>
    public sealed class PuzzleView : MonoBehaviour
    {
        private static readonly Color Ink = new Color32(225, 235, 242, 255);
        private static readonly Color Muted = new Color32(139, 158, 176, 255);
        private static readonly Color Mint = new Color32(100, 236, 190, 255);
        private static readonly Color Violet = new Color32(183, 157, 255, 255);
        private static readonly Color Gold = new Color32(255, 207, 112, 255);
        private static readonly Color Floor = new Color32(22, 34, 47, 255);
        private static readonly Color Wall = new Color32(45, 60, 75, 255);
        private static readonly Color Control = new Color32(30, 45, 60, 255);
        private static readonly Color Bad = new Color32(255, 140, 140, 255);
        private static readonly Color[] PairColors =
        {
            Gold, new Color32(120, 186, 255, 255), new Color32(255, 140, 140, 255), new Color32(140, 235, 170, 255)
        };
        private static readonly Color[] PairDarks =
        {
            new Color32(86, 67, 36, 255), new Color32(36, 56, 84, 255),
            new Color32(84, 44, 44, 255), new Color32(42, 78, 58, 255)
        };
        private readonly List<RectTransform> bodies = new List<RectTransform>();
        private readonly Dictionary<Cell, Image> tiles = new Dictionary<Cell, Image>();
        private readonly Dictionary<Cell, Text> symbols = new Dictionary<Cell, Text>();
        private readonly Dictionary<Cell, Image> doorBars = new Dictionary<Cell, Image>();
        private readonly Dictionary<Cell, Coroutine> wallBumps = new Dictionary<Cell, Coroutine>();
        private readonly List<Button> levelButtons = new List<Button>();
        private readonly List<Button> moveButtons = new List<Button>();
        private RectTransform root;
        private RectTransform board;
        private Image boardPanel;
        private Font font;
        private LevelDefinition level;
        private float cellSize;
        private Text levelTitle;
        private Text moves;
        private Text status;
        private Text hintText;
        private RectTransform hintPanel;
        private Button undoButton;
        private Coroutine statusRoutine;
        private bool wonShown;
        private EventSystem ownedEventSystem;

        public void Build(PuzzleLevelLibrary library, Action undo, Action reset,
                          Action<int> select, Action<Direction> move)
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasObject = new GameObject("Puzzle UI", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 800);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            root = Rect("Content", canvasObject.transform, Vector2.zero, new Vector2(1280, 800));
            Panel("Background", root, Vector2.zero, new Vector2(4000, 4000), new Color32(13, 22, 32, 255));

            levelTitle = Label("", root, new Vector2(-480, 370), new Vector2(280, 26), 15, Muted, TextAnchor.MiddleLeft);
            moves = Label("", root, new Vector2(490, 370), new Vector2(240, 26), 15, Muted, TextAnchor.MiddleRight);
            MakeButton("?", root, new Vector2(604, 322), new Vector2(36, 30), ToggleHint);

            boardPanel = Panel("Board", root, new Vector2(0, 10), new Vector2(700, 480), new Color32(18, 30, 42, 255));
            board = Rect("Board Grid", root, new Vector2(0, 10), new Vector2(660, 440));

            hintPanel = Rect("Hint", root, new Vector2(0, 322), new Vector2(820, 52));
            Panel("Hint Panel", hintPanel, Vector2.zero, new Vector2(820, 52), new Color32(23, 36, 50, 255));
            hintText = Label("", hintPanel, Vector2.zero, new Vector2(780, 44), 15, Ink);
            hintPanel.gameObject.SetActive(false);

            status = Label("", root, new Vector2(0, -292), new Vector2(900, 40), 17, Mint);
            undoButton = MakeButton("Undo [Z]", root, new Vector2(-560, -350), new Vector2(120, 36), undo);
            MakeButton("Reset [R]", root, new Vector2(-432, -350), new Vector2(120, 36), reset);
            Label("WASD move   Z undo   R reset   H hint", root, new Vector2(0, -358),
                new Vector2(600, 24), 12, Muted);
            moveButtons.Add(MakeButton("^", root, new Vector2(556, -286), new Vector2(36, 30), () => move(Direction.Up)));
            moveButtons.Add(MakeButton("<", root, new Vector2(516, -318), new Vector2(36, 30), () => move(Direction.Left)));
            moveButtons.Add(MakeButton("v", root, new Vector2(556, -318), new Vector2(36, 30), () => move(Direction.Down)));
            moveButtons.Add(MakeButton(">", root, new Vector2(596, -318), new Vector2(36, 30), () => move(Direction.Right)));

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
            bodies.Clear(); tiles.Clear(); symbols.Clear(); doorBars.Clear();
            cellSize = Mathf.Min(1100f / level.Width, 520f / level.Height);
            Vector2 size = new Vector2(cellSize * level.Width, cellSize * level.Height);
            board.sizeDelta = size;
            boardPanel.rectTransform.sizeDelta = size + new Vector2(36, 36);
            for (int y = 0; y < level.Height; y++)
            for (int x = 0; x < level.Width; x++)
            {
                var cell = new Cell(x, y);
                Tile tile = level.TileAt(cell);
                Image image = Panel($"{tile} {cell}", board, Position(cell), Vector2.one * (cellSize - 6),
                    tile == Tile.Wall ? Wall : Floor);
                tiles.Add(cell, image);
                if (tile == Tile.Floor || tile == Tile.Wall) continue;
                if (tile == Tile.Goal)
                {
                    RectTransform diamond = Rect("Goal", image.transform, Vector2.zero,
                        Vector2.one * cellSize * .3f);
                    diamond.localRotation = Quaternion.Euler(0, 0, 45);
                    Image mark = diamond.gameObject.AddComponent<Image>();
                    mark.color = Mint;
                    mark.raycastTarget = false;
                    continue;
                }
                int pair = level.PairIndexAt(cell);
                string symbol = tile == Tile.Splitter ? "S" : tile == Tile.Door ? ((char)('A' + pair)).ToString()
                    : ((char)('a' + pair)).ToString();
                Text marker = Label(symbol, image.transform, Vector2.zero, Vector2.one * cellSize,
                    Mathf.RoundToInt(cellSize * .36f), tile == Tile.Splitter ? Violet : PairColors[pair]);
                symbols.Add(cell, marker);
                if (tile == Tile.Door)
                    doorBars.Add(cell, Panel("Bar", image.transform, new Vector2(0, cellSize * .3f),
                        new Vector2(cellSize * .5f, 3), PairColors[pair]));
            }
            levelTitle.text = $"{index + 1:00} / {entry.title}";
            hintText.text = entry.hint;
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
            foreach (var pair in symbols)
            {
                Tile tile = level.TileAt(pair.Key);
                if (tile == Tile.Door)
                {
                    int index = level.PairIndexAt(pair.Key);
                    bool open = state.IsDoorOpen(index);
                    tiles[pair.Key].color = open ? Floor : PairDarks[index];
                    pair.Value.color = open ? Muted : PairColors[index];
                    if (doorBars.TryGetValue(pair.Key, out Image bar)) bar.gameObject.SetActive(!open);
                }
                else if (tile == Tile.Switch)
                {
                    pair.Value.color = PairColors[level.PairIndexAt(pair.Key)];
                }
            }
            moves.text = $"{state.MoveCount:00} MOVES";
            undoButton.interactable = canUndo;
            for (int i = 0; i < moveButtons.Count; i++) moveButtons[i].interactable = !busy && !state.Won;
            if (state.Won)
            {
                if (wonShown) return;
                wonShown = true;
                StopStatus();
                status.color = Mint;
                status.text = hasNext ? "Goal reached. Press N." : "All levels complete.";
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
            }
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

        public void ToggleHint() => hintPanel.gameObject.SetActive(!hintPanel.gameObject.activeSelf);

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

        public void ShowBlocked()
        {
            if (wonShown) return;
            if (statusRoutine != null) StopCoroutine(statusRoutine);
            status.color = Gold;
            status.text = "Blocked. Try another direction.";
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
                    Vector2.one * cellSize * .62f, index == 0 ? Mint : Violet);
                bodies.Add(body.rectTransform);
                Label(index == 0 ? "1" : "2", body.transform, Vector2.zero, Vector2.one * cellSize * .6f,
                    Mathf.RoundToInt(cellSize * .32f), new Color32(16, 31, 40, 255));
            }
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

        private Button MakeButton(string text, Transform parent, Vector2 position, Vector2 size, Action action)
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
            Label(text, image.transform, Vector2.zero, size, 13, Ink);
            return button;
        }

        private void OnDestroy()
        {
            if (ownedEventSystem != null) Destroy(ownedEventSystem.gameObject);
        }
    }
}
