using UnityEngine;

namespace Numen.Chie
{
    /// <summary>全部可调数值与资源引用。禁止在其它脚本里写魔数。</summary>
    [CreateAssetMenu(menuName = "Chie/ChieConfig", fileName = "ChieConfig")]
    public class ChieConfig : ScriptableObject
    {
        [Header("场景名")]
        [SerializeField] private string _gallerySceneName = "Scene_Gallery";
        [SerializeField] private string _selfSceneName = "Scene_Chie";
        [SerializeField] private string _paintingId = "chie";

        [Header("画布几何：世界单位")]
        [SerializeField] private int _canvasWidth = 1920;
        [SerializeField] private int _canvasHeight = 1080;
        [SerializeField] private float _pixelsPerUnit = 100f;
        [SerializeField] private float _cameraSize = 5.4f;

        [Header("判定网格：像素，10 = 每格 10×10")]
        [SerializeField] private int _cellPixels = 10;
        [Range(0f, 1f)] [SerializeField] private float _coverThreshold = 0.45f;
        [Range(0f, 1f)] [SerializeField] private float _colorTolerance = 0.18f;

        [Header("笔刷：半径 = 像素")]
        [SerializeField] private float _brushRadiusPx = 18f;
        [SerializeField] private float _brushStepPx = 4f;
        [SerializeField] private int _maxBlitsPerFrame = 16;

        [Header("姿态交叉淡入：秒")]
        [SerializeField] private float _poseBlendShort = 0.25f;
        [SerializeField] private float _poseBlendStand = 0.4f;

        [Header("对话：每字秒数 / 选项缩进像素")]
        [SerializeField] private float _typeCharSeconds = 0.03f;
        [SerializeField] private float _optionHoverIndent = 12f;
        [SerializeField] private float _optionHoverSeconds = 0.12f;

        [Header("演出：秒")]
        [SerializeField] private float _holdPause = 0.8f;
        [SerializeField] private float _holdSilence = 0.4f;
        [SerializeField] private float _finaleFade = 0.4f;
        [SerializeField] private float _exitFade = 0.6f;
        [SerializeField] private float _breathPeriod = 1.2f;
        [SerializeField] private float _breathMinAlpha = 0.26f;
        [SerializeField] private float _breathMaxAlpha = 0.60f;

        [Header("结局阈值")]
        [SerializeField] private int _allOpenAt = 6;
        [SerializeField] private int _partOpenAt = 3;

        [Header("资源：四姿态")]
        [SerializeField] private PoseArt[] _poses = new PoseArt[0];

        [Header("资源：其他")]
        [SerializeField] private Texture2D _brushTex;
        [SerializeField] private Texture2D _dimTex;
        [SerializeField] private Texture2D _iconBrush;
        [SerializeField] private Texture2D _iconEraser;
        [SerializeField] private Texture2D _iconEyedropper;
        [SerializeField] private Texture2D _iconPutDown;
        [SerializeField] private TMPro.TMP_FontAsset _font;
        [SerializeField] private string _dialogueResourcePath = "Chie/chie_dialogue";
        [SerializeField] private string _fontResourcePath = "Chie/Fonts/ChieSDF";

        [Header("六处区域（归一化，左下原点）")]
        [SerializeField] private FlawZone[] _zones = new FlawZone[0];

        [Header("吸管目标色")]
        [SerializeField] private Color _lipTargetColor = new Color(0.72f, 0.32f, 0.30f, 1f);
        [SerializeField] private Color _dressTargetColor = new Color(0.42f, 0.46f, 0.55f, 1f);

        /// <summary>画廊场景名。</summary>
        public string GallerySceneName => _gallerySceneName;
        /// <summary>本场景名。</summary>
        public string SelfSceneName => _selfSceneName;
        /// <summary>画 ID。</summary>
        public string PaintingId => _paintingId;

        /// <summary>画布宽（像素）。</summary>
        public int CanvasWidth => _canvasWidth;
        /// <summary>画布高（像素）。</summary>
        public int CanvasHeight => _canvasHeight;
        /// <summary>每单位像素数。</summary>
        public float PixelsPerUnit => _pixelsPerUnit;
        /// <summary>相机正交尺寸。</summary>
        public float CameraSize => _cameraSize;
        /// <summary>画布世界宽。</summary>
        public float WorldWidth => _canvasWidth / _pixelsPerUnit;
        /// <summary>画布世界高。</summary>
        public float WorldHeight => _canvasHeight / _pixelsPerUnit;

        /// <summary>判定格边长（像素）。</summary>
        public int CellPixels => _cellPixels;
        /// <summary>网格宽（格数）。</summary>
        public int GridW => _canvasWidth / _cellPixels;
        /// <summary>网格高（格数）。</summary>
        public int GridH => _canvasHeight / _cellPixels;
        /// <summary>修复覆盖率阈值。</summary>
        public float CoverThreshold => _coverThreshold;
        /// <summary>吸管取色容差。</summary>
        public float ColorTolerance => _colorTolerance;

        /// <summary>笔刷半径（像素）。</summary>
        public float BrushRadiusPx => _brushRadiusPx;
        /// <summary>插值落点间距（像素）。</summary>
        public float BrushStepPx => _brushStepPx;
        /// <summary>每帧最多 Blit 次数。</summary>
        public int MaxBlitsPerFrame => _maxBlitsPerFrame;

        /// <summary>普通姿态淡入秒数。</summary>
        public float PoseBlendShort => _poseBlendShort;
        /// <summary>站起淡入秒数。</summary>
        public float PoseBlendStand => _poseBlendStand;

        /// <summary>打字机每字秒数。</summary>
        public float TypeCharSeconds => _typeCharSeconds;
        /// <summary>打字机每字秒数（对话系统别名）。</summary>
        public float DialogueCharSeconds => _typeCharSeconds;
        /// <summary>台词行距：TMP 的 lineSpacing 单位是「字号百分比」，不是像素。
        /// 换算依据（规范 §10.4：正文字号 32、行高 56px、三行）：
        /// ChieSDF（DengXian Regular）FaceInfo pointSize=48、lineHeight=50.015625、lineGap=0，
        /// 32 号时自然行距 = 50.015625 × 32 / 48 = 33.34375px；
        /// TextMeshProUGUI 的 m_isOrthographic=true（TMPro_UGUI_Private.cs:94），
        /// 每 1 单位行距 = 字号 × 0.01 = 0.32px（TMP_Text.cs:3967 / TMPro_UGUI_Private.cs:1731）；
        /// 故 (56 − 33.34375) / 0.32 = 70.80078125 ≈ 70.8，三行占 3×56 = 168px ≤ 文本框高 180px。</summary>
        public float BodyLineSpacing => 70.8f;
        /// <summary>选项悬停缩进（像素）。</summary>
        public float OptionHoverIndent => _optionHoverIndent;
        /// <summary>选项悬停缓动秒数。</summary>
        public float OptionHoverSeconds => _optionHoverSeconds;

        /// <summary>站起时长。</summary>
        public float HoldPause => _holdPause;
        /// <summary>站起后的静止。</summary>
        public float HoldSilence => _holdSilence;
        /// <summary>终幕文字淡出。</summary>
        public float FinaleFade => _finaleFade;
        /// <summary>退出淡出。</summary>
        public float ExitFade => _exitFade;
        /// <summary>虚线呼吸周期。</summary>
        public float BreathPeriod => _breathPeriod;
        /// <summary>呼吸最低 alpha。</summary>
        public float BreathMinAlpha => _breathMinAlpha;
        /// <summary>呼吸最高 alpha。</summary>
        public float BreathMaxAlpha => _breathMaxAlpha;

        /// <summary>全开放的恢复数。</summary>
        public int AllOpenAt => _allOpenAt;
        /// <summary>部分开放的恢复数。</summary>
        public int PartOpenAt => _partOpenAt;

        /// <summary>取某姿态的图组，缺失返回 null。</summary>
        public PoseArt GetPose(PoseId id)
        {
            if (_poses == null) return null;
            for (int i = 0; i < _poses.Length; i++)
                if (_poses[i] != null && _poses[i].pose == id) return _poses[i];
            return null;
        }

        /// <summary>所有姿态图组。</summary>
        public PoseArt[] Poses => _poses;

        /// <summary>笔刷贴图。</summary>
        public Texture2D BrushTex => _brushTex;
        /// <summary>暗底渐晕贴图。</summary>
        public Texture2D DimTex => _dimTex;
        /// <summary>工具图标：画笔。</summary>
        public Texture2D IconBrush => _iconBrush;
        /// <summary>工具图标：橡皮。</summary>
        public Texture2D IconEraser => _iconEraser;
        /// <summary>工具图标：吸管。</summary>
        public Texture2D IconEyedropper => _iconEyedropper;
        /// <summary>工具图标：放下笔。</summary>
        public Texture2D IconPutDown => _iconPutDown;
        /// <summary>中文 SDF 字体。</summary>
        public TMPro.TMP_FontAsset Font => _font;

        /// <summary>对话 JSON 的 Resources 路径（不含扩展名）。</summary>
        public string DialogueResourcePath => _dialogueResourcePath;
        /// <summary>TMP 字体资产的 Resources 路径。</summary>
        public string FontResourcePath => _fontResourcePath;

        /// <summary>六处归一化区域。</summary>
        public FlawZone[] Zones => _zones;

        /// <summary>取某处在某姿态下的归一化区域，找不到返回 false。</summary>
        public bool TryGetZone(FlawId id, PoseId pose, out Vector4 rect)
        {
            rect = Vector4.zero;
            if (_zones == null) return false;
            for (int i = 0; i < _zones.Length; i++)
            {
                if (_zones[i] == null || _zones[i].id != id) continue;
                if (_zones[i].pose != pose) continue;
                rect = _zones[i].rect;
                return true;
            }
            return false;
        }

        /// <summary>唇色目标。</summary>
        public Color LipTargetColor => _lipTargetColor;
        /// <summary>裙子目标色。</summary>
        public Color DressTargetColor => _dressTargetColor;
    }
}
