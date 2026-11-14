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
        private static readonly Color Muted = new Color32(65, 88, 78, 255);
        private static readonly Color Mint = new Color32(28, 94, 67, 255);
        private static readonly Color Gold = new Color32(122, 82, 29, 255);
        private static readonly Color Floor = new Color32(22, 34, 47, 255);
        private static readonly Color Wall = new Color32(45, 60, 75, 255);
        private static readonly Color Control = new Color32(74, 107, 92, 255);
        private static readonly Color Bad = new Color32(154, 57, 54, 255);

        private readonly List<RectTransform> bodies = new List<RectTransform>();
        private readonly Dictionary<Cell, Image> tiles = new Dictionary<Cell, Image>();
        private readonly Dictionary<Cell, Image> fixtures = new Dictionary<Cell, Image>();
        private readonly Dictionary<Cell, Coroutine> wallBumps = new Dictionary<Cell, Coroutine>();
        private readonly List<Button> levelButtons = new List<Button>();
        private readonly List<Button> moveButtons = new List<Button>();
        private RectTransform root;
        private RectTransform board;
        private Image boardPanel;
        private Font font;
        private PuzzleArtwork artwork;
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

            levelTitle = Label("", root, new Vector2(-460, 370), new Vector2(320, 28), 17, Ink, TextAnchor.MiddleLeft);
            moves = Label("", root, new Vector2(490, 370), new Vector2(240, 28), 17, Ink, TextAnchor.MiddleRight);
            MakeButton("提示", root, new Vector2(590, 322), new Vector2(64, 32), ToggleHint);

            boardPanel = Panel("Board", root, new Vector2(0, 10), new Vector2(700, 480), new Color32(147, 165, 142, 255));
            board = Rect("Board Grid", root, new Vector2(0, 10), new Vector2(660, 440));

            hintPanel = Rect("Hint", root, new Vector2(0, 322), new Vector2(820, 52));
            Panel("Hint Panel", hintPanel, Vector2.zero, new Vector2(820, 52), new Color32(202, 215, 193, 250));
            hintText = Label("", hintPanel, Vector2.zero, new Vector2(780, 44), 16, Ink);
            hintPanel.gameObject.SetActive(false);

            status = Label("", root, new Vector2(0, -292), new Vector2(900, 40), 17, Mint);
            undoButton = MakeButton("撤销 [Z]", root, new Vector2(-560, -350), new Vector2(120, 36), undo);
            MakeButton("重置 [R]", root, new Vector2(-432, -350), new Vector2(120, 36), reset);
            Label("方向键 / WASD 移动　 Z 撤销　 R 重置　 H 提示", root, new Vector2(0, -358),
                new Vector2(600, 26), 14, Muted);
            moveButtons.Add(MakeButton("上", root, new Vector2(556, -286), new Vector2(36, 30), () => move(Direction.Up)));
            moveButtons.Add(MakeButton("左", root, new Vector2(516, -318), new Vector2(36, 30), () => move(Direction.Left)));
            moveButtons.Add(MakeButton("下", root, new Vector2(556, -318), new Vector2(36, 30), () => move(Direction.Down)));
            moveButtons.Add(MakeButton("右", root, new Vector2(596, -318), new Vector2(36, 30), () => move(Direction.Right)));

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
            cellSize = Mathf.Min(1100f / level.Width, 520f / level.Height);
            Vector2 size = new Vector2(cellSize * level.Width, cellSize * level.Height);
            board.sizeDelta = size;
            boardPanel.rectTransform.sizeDelta = size + new Vector2(36, 36);
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
            levelTitle.text = $"第 {index + 1:00} 关 · {entry.title}";
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
            foreach (var pair in fixtures)
            {
                Tile tile = level.TileAt(pair.Key);
                if (tile == Tile.Door)
                {
                    int index = level.PairIndexAt(pair.Key);
                    bool open = state.IsDoorOpen(index);
                    pair.Value.sprite = open ? artwork.DoorOpen : artwork.DoorClosed;
                }
                else if (tile == Tile.Switch)
                {
                    int index = level.PairIndexAt(pair.Key);
                    pair.Value.sprite = state.IsDoorOpen(index) ? artwork.SwitchOn : artwork.SwitchOff;
                }
            }
            moves.text = $"已走 {state.MoveCount:00} 步";
            undoButton.interactable = canUndo;
            for (int i = 0; i < moveButtons.Count; i++) moveButtons[i].interactable = !busy && !state.Won;
            if (state.Won)
            {
                if (wonShown) return;
                wonShown = true;
                StopStatus();
                status.color = Mint;
                status.text = hasNext ? "目标已达成！按 N 进入下一关。" : "全部关卡完成，恭喜通关！";
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
            Label(text, image.transform, Vector2.zero, size, 14, new Color32(240, 244, 231, 255));
            return button;
        }

        private void OnDestroy()
        {
            artwork?.Release();
            if (ownedEventSystem != null) Destroy(ownedEventSystem.gameObject);
        }
    }
}
