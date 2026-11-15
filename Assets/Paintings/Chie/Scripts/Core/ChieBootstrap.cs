using Numen.Chie.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Numen.Chie
{
    /// <summary>运行时搭出整棵层级（场景里只放相机 + 本脚本），避免手写场景 YAML。</summary>
    public class ChieBootstrap : MonoBehaviour
    {
        [SerializeField] private ChieConfig _config;
        [SerializeField] private Camera _cam;

        [Tooltip("勾上表示由画廊/模块调用 EnsureBuilt() 启动；不勾则场景加载即自启。")]
        [SerializeField] private bool _moduleDriven;

        private PaintingView _view;
        private BrushPainter _painter;
        private CanvasPointer _pointer;
        private FlawJudge _judge;
        private WaveUnlock _wave;
        private DialogueRunner _runner;
        private InCanvasDialogueView _dialogueView;
        private ToolBarView _toolbar;
        private EndingView _ending;
        private RevealPlayer _reveal;
        private ChieMarkerOverlay _markers;
        private ChieFade _fade;
        private ChieExtractView _extract;
        private ChieSfx _sfx;
        private ChieFlow _flow;

        /// <summary>配置引用（场景里已填，缺了从 Resources 兜底）。</summary>
        public ChieConfig Config { get { return _config; } }

        /// <summary>本画专用相机（装配期注入，不再抢 Camera.main）。</summary>
        public Camera Cam { get { return _cam; } }

        private void Awake()
        {
            if (_config == null) _config = Resources.Load<ChieConfig>("Chie/ChieConfig");
            if (_cam == null) _cam = Camera.main;   // 唯一兜底：场景自带相机
            ApplyFont();
        }

        private void Start()
        {
            if (!_moduleDriven) EnsureBuilt();
        }

        // 只用于把 Resources 里的字体塞回配置（配置本身是只读资产，运行时补一层）
        private static readonly System.Reflection.FieldInfo FontField =
            typeof(ChieConfig).GetField("_font", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        private void ApplyFont()
        {
            if (_config == null || _config.Font != null) return;
            TMPro.TMP_FontAsset f = Resources.Load<TMPro.TMP_FontAsset>(_config.FontResourcePath);
            if (FontField != null && f != null) FontField.SetValue(_config, f);
        }

        /// <summary>幂等：搭好全部对象并把引用交给 ChieFlow。</summary>
        public ChieFlow EnsureBuilt()
        {
            if (_flow != null) return _flow;
            _flow = GetComponent<ChieFlow>();
            if (_flow == null) _flow = gameObject.AddComponent<ChieFlow>();
            BuildAll(_flow);
            return _flow;
        }

        private void BuildAll(ChieFlow flow)
        {
            EnsureEventSystem();
            if (_cam == null)
            {
                Debug.LogError("[Chie] 场景里没有相机，先建一个继续跑");
                var camGo = new GameObject("Main Camera", typeof(Camera));
                camGo.tag = "MainCamera";
                _cam = camGo.GetComponent<Camera>();
            }
            _cam.orthographic = true;
            _cam.orthographicSize = _config != null ? _config.CameraSize : 5.4f;
            _cam.transform.position = new Vector3(0f, 0f, -10f);
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color32(0x0A, 0x0A, 0x0A, 0xFF);

            Material paintMat = MakeMaterial("Numen/ChiePainting");
            Material brushMat = MakeMaterial("Numen/BrushStamp");
            Material eraseMat = MakeMaterial("Numen/BrushErase");

            Texture2D brushShape = _config != null ? _config.BrushTex : null;
            if (brushShape != null)
            {
                if (brushMat != null) brushMat.SetTexture("_Shape", brushShape);
                if (eraseMat != null) eraseMat.SetTexture("_Shape", brushShape);
            }

            _judge = gameObject.AddComponent<FlawJudge>();
            _judge.Build(_config);

            var quad = new GameObject("PaintingQuad", typeof(SpriteRenderer));
            quad.transform.SetParent(transform, false);
            var sr = quad.GetComponent<SpriteRenderer>();
            sr.sortingOrder = 0;
            sr.sprite = MakeFullSprite(_config);
            if (paintMat != null) sr.material = paintMat;

            _view = quad.AddComponent<PaintingView>();
            _view.Init(_config, paintMat, _judge);

            var brushLayer = new GameObject("BrushLayer");
            brushLayer.transform.SetParent(transform, false);
            _painter = brushLayer.AddComponent<BrushPainter>();
            _painter.Init(_config, brushMat, eraseMat);
            _view.SetBrushRT(_painter.BrushRT);

            var markerGo = new GameObject("MarkerOverlay");
            markerGo.transform.SetParent(transform, false);
            _markers = markerGo.AddComponent<ChieMarkerOverlay>();

            var fadeGo = new GameObject("FadeOverlay");
            fadeGo.transform.SetParent(transform, false);
            _fade = fadeGo.AddComponent<ChieFade>();
            _fade.Init(_config);

            _pointer = gameObject.AddComponent<CanvasPointer>();
            _pointer.Init(_config, _cam);

            _wave = gameObject.AddComponent<WaveUnlock>();
            _wave.Init(_config, _judge);

            _dialogueView = gameObject.AddComponent<InCanvasDialogueView>();
            _dialogueView.Build(_config, _config != null ? _config.Font : null, _cam);

            _runner = gameObject.AddComponent<DialogueRunner>();
            DialogueData data = DialogueData.Load(_config != null ? _config.DialogueResourcePath : "Chie/chie_dialogue");
            _runner.Init(data, _dialogueView);

            _toolbar = gameObject.AddComponent<ToolBarView>();
            _toolbar.Build(_config, _cam);
            _toolbar.SetVisible(false);

            _ending = gameObject.AddComponent<EndingView>();
            _ending.Build(_config, _cam);

            _reveal = gameObject.AddComponent<RevealPlayer>();
            _reveal.Init(_config, _view, _judge, _runner);

            _markers.Init(_config, _judge, _wave);
            _markers.SetVisible(false);

            _extract = ChieExtractView.Build(_config, _cam, transform);
            _sfx = ChieSfx.Build(transform);

            _flow.Bind(_config, _view, _painter, _pointer, _judge, _wave,
                _runner, _dialogueView, _toolbar, _ending, _reveal, _markers, _fade);
            _flow.AttachExtract(_extract);
            _flow.AttachSfx(_sfx);
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            go.transform.SetParent(null, false);
        }

        private static Material MakeMaterial(string shaderName)
        {
            Shader s = Shader.Find(shaderName);
            if (s == null || !s.isSupported)
            {
                Debug.LogError("[Chie] 找不到 shader：" + shaderName);
                return null;
            }
            return new Material(s);
        }

        private Sprite MakeFullSprite(ChieConfig config)
        {
            PoseArt sit = config != null ? config.GetPose(PoseId.Sit) : null;
            Texture2D tex = sit != null && sit.baseTex != null ? sit.baseTex : null;
            if (tex == null)
            {
                Debug.LogError("[Chie] 缺少 base_sit，用洋红占位继续跑");
                tex = ChieFallbackTex.Magenta;
            }
            float ppu = config != null ? config.PixelsPerUnit : 100f;
            return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), ppu);
        }
    }
}
