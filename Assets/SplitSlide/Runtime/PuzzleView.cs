using System;
using System.Collections.Generic;
using SplitSlide.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SplitSlide
{
    /// <summary>Code-built, resolution-scaled UI. Bodies move only through RectTransform displacement.</summary>
    public sealed class PuzzleView : MonoBehaviour
    {
        private static readonly Color Ink = new Color32(225, 235, 242, 255);
        private static readonly Color Muted = new Color32(139, 158, 176, 255);
        private static readonly Color Mint = new Color32(100, 236, 190, 255);
        private static readonly Color Gold = new Color32(255, 207, 112, 255);
        private static readonly Color Violet = new Color32(183, 157, 255, 255);
        private readonly List<RectTransform> bodies = new List<RectTransform>();
        private readonly Dictionary<Cell, Image> tiles = new Dictionary<Cell, Image>();
        private readonly Dictionary<Cell, Text> symbols = new Dictionary<Cell, Text>();
        private readonly List<Button> levelButtons = new List<Button>();
        private RectTransform root;
        private RectTransform board;
        private Font font;
        private LevelDefinition level;
        private float cellSize;
        private Text levelTitle;
        private Text hint;
        private Text stats;
        private Text status;
        private Text doorStatus;
        private Button undoButton;
        private Button nextButton;
        private EventSystem ownedEventSystem;

        public void Build(PuzzleLevelLibrary library, Action undo, Action reset, Action next,
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

            Label("SLIDE / SPLIT", root, new Vector2(-336, 333), new Vector2(470, 64), 43, Ink, TextAnchor.MiddleLeft);
            Label("ONE DIRECTION. TWO BODIES. FIND YOUR WAY.", root, new Vector2(-340, 281), new Vector2(520, 30), 14, Muted);
            Panel("Accent", root, new Vector2(-584, 333), new Vector2(5, 42), Mint);
            Panel("Board Panel", root, new Vector2(-192, -5), new Vector2(808, 510), new Color32(20, 32, 45, 255));
            board = Rect("Board", root, new Vector2(-192, -5), new Vector2(760, 460));

            Panel("Side Panel", root, new Vector2(420, -5), new Vector2(342, 670), new Color32(23, 36, 50, 255));
            Label("THE EXPERIMENT", root, new Vector2(420, 291), new Vector2(290, 30), 13, Mint, TextAnchor.MiddleLeft);
            levelTitle = Label("", root, new Vector2(420, 241), new Vector2(290, 62), 27, Ink, TextAnchor.MiddleLeft);
            hint = Label("", root, new Vector2(420, 155), new Vector2(290, 100), 18, Muted, TextAnchor.UpperLeft);
            stats = Label("", root, new Vector2(420, 65), new Vector2(290, 36), 18, Ink, TextAnchor.MiddleLeft);
            doorStatus = Label("", root, new Vector2(420, 27), new Vector2(290, 30), 15, Gold, TextAnchor.MiddleLeft);
            undoButton = MakeButton("Undo  [Z]", root, new Vector2(343, -29), new Vector2(138, 45), undo);
            MakeButton("Reset  [R]", root, new Vector2(495, -29), new Vector2(138, 45), reset);
            nextButton = MakeButton("Next level  [N]", root, new Vector2(420, -86), new Vector2(290, 45), next);
            Label("WASD / ARROW KEYS", root, new Vector2(420, -142), new Vector2(290, 26), 13, Muted);
            MakeButton("^", root, new Vector2(420, -181), new Vector2(56, 43), () => move(Direction.Up));
            MakeButton("<", root, new Vector2(354, -234), new Vector2(56, 43), () => move(Direction.Left));
            MakeButton("v", root, new Vector2(420, -234), new Vector2(56, 43), () => move(Direction.Down));
            MakeButton(">", root, new Vector2(486, -234), new Vector2(56, 43), () => move(Direction.Right));
            Label("SELECT A LEVEL", root, new Vector2(420, -285), new Vector2(290, 24), 12, Muted);
            float buttonWidth = Mathf.Min(48, 280f / library.levels.Length - 8);
            for (int i = 0; i < library.levels.Length; i++)
            {
                int index = i;
                float x = 420 + (i - (library.levels.Length - 1) * .5f) * (buttonWidth + 8);
                levelButtons.Add(MakeButton((i + 1).ToString(), root, new Vector2(x, -319),
                    new Vector2(buttonWidth, 32), () => select(index)));
            }

            status = Label("", root, new Vector2(-192, -298), new Vector2(808, 46), 20, Mint);
            Label("S  SPLITTER       T  TOGGLE       D  DOOR       G  GOAL", root,
                new Vector2(-192, -351), new Vector2(808, 28), 14, Muted);
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
            for (int i = board.childCount - 1; i >= 0; i--)
            {
                board.GetChild(i).gameObject.SetActive(false);
                Destroy(board.GetChild(i).gameObject);
            }
            bodies.Clear(); tiles.Clear(); symbols.Clear();
            cellSize = Mathf.Min(750f / level.Width, 455f / level.Height);
            for (int y = 0; y < level.Height; y++)
            for (int x = 0; x < level.Width; x++)
            {
                var cell = new Cell(x, y);
                Tile tile = level.TileAt(cell);
                Image image = Panel($"{tile} {cell}", board, Position(cell), Vector2.one * (cellSize - 4),
                    tile == Tile.Wall ? new Color32(56, 73, 91, 255) : new Color32(29, 44, 59, 255));
                tiles.Add(cell, image);
                if (tile == Tile.Floor || tile == Tile.Wall) continue;
                string symbol = tile == Tile.Splitter ? "S" : tile == Tile.Switch ? "T" : tile == Tile.Door ? "D" : "G";
                Text marker = Label(symbol, image.transform, Vector2.zero, Vector2.one * cellSize,
                    Mathf.RoundToInt(cellSize * .4f), TileColor(tile));
                symbols.Add(cell, marker);
            }
            levelTitle.text = $"{index + 1:00} / {entry.title}";
            hint.text = entry.hint;
            for (int i = 0; i < levelButtons.Count; i++) levelButtons[i].interactable = i != index;
        }

        public void Apply(PuzzleSnapshot state, bool canUndo, bool busy, bool hasNext)
        {
            EnsureBodies(state);
            for (int i = 0; i < bodies.Count; i++) bodies[i].anchoredPosition = Position(state.Bodies[i]);
            foreach (var pair in symbols)
            {
                Tile tile = level.TileAt(pair.Key);
                if (tile == Tile.Door)
                {
                    pair.Value.text = state.DoorsOpen ? "-" : "D";
                    tiles[pair.Key].color = state.DoorsOpen ? new Color32(34, 67, 64, 255) : new Color32(125, 87, 45, 255);
                }
                else if (tile == Tile.Switch) pair.Value.color = state.DoorsOpen ? Mint : Gold;
                else if (tile == Tile.Splitter) pair.Value.color = state.HasSplit ? Muted : Violet;
            }
            stats.text = $"MOVES  {state.MoveCount:00}          BODIES  {state.Bodies.Count}/2";
            doorStatus.text = "DOORS  /  " + (state.DoorsOpen ? "OPEN" : "CLOSED");
            undoButton.interactable = canUndo;
            nextButton.interactable = state.Won && hasNext && !busy;
            status.color = Mint;
            status.text = busy ? "Sliding..." : state.Won ? (hasNext ? "Goal reached. Next experiment is ready." : "All experiments complete. Well played!")
                : state.HasSplit ? "Two bodies. One input. Use each other as a wall." : "Choose a direction. Slide all the way to a stop.";
        }

        public void Interpolate(PuzzleSnapshot from, PuzzleSnapshot to, float progress)
        {
            for (int i = 0; i < from.Bodies.Count; i++)
                bodies[i].anchoredPosition = Vector2.Lerp(Position(from.Bodies[i]), Position(to.Bodies[i]), progress);
        }

        public void ShowBlocked() => status.text = "Blocked. Try another direction, or undo with Z.";
        public void ShowError(string message)
        {
            if (status != null) { status.color = new Color32(255, 140, 140, 255); status.text = message; }
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
                Image body = Panel($"Body {index + 1}", board, Position(state.Bodies[index]), Vector2.one * cellSize * .68f,
                    index == 0 ? Mint : Violet);
                bodies.Add(body.rectTransform);
                Label(index == 0 ? "A" : "B", body.transform, Vector2.zero, Vector2.one * cellSize * .65f,
                    Mathf.RoundToInt(cellSize * .35f), new Color32(16, 31, 40, 255));
            }
        }

        private Vector2 Position(Cell cell) => new Vector2((cell.X - (level.Width - 1) * .5f) * cellSize,
            (cell.Y - (level.Height - 1) * .5f) * cellSize);
        private static Color TileColor(Tile tile) => tile == Tile.Splitter ? Violet : tile == Tile.Goal ? Mint : Gold;

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
            Image image = Panel(text, parent, position, size, new Color32(49, 69, 88, 255));
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
            Label(text, image.transform, Vector2.zero, size, 16, Ink);
            return button;
        }

        private void OnDestroy()
        {
            if (ownedEventSystem != null) Destroy(ownedEventSystem.gameObject);
        }
    }
}
