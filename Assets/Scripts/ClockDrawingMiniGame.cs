using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ClockDrawingGame : MonoBehaviour
{
    public static ClockDrawingGame Instance { get; private set; }

    [Header("Monologue Panel")]
    [SerializeField] private GameObject monologuePanel;
    [SerializeField] private TextMeshProUGUI monologueText;

    [TextArea(2, 5)]
    [SerializeField]
    private string monologue =
        "I'm supposed to draw a clock.\nSomebody said so. When was that?";

    [Header("Drawing Panel")]
    [SerializeField] private GameObject drawingPanel;

    [Header("Board Background")]
    [Tooltip("Image behind the drawing canvas. Swap Source Image for paper/notebook sprites.")]
    [SerializeField] private Image boardBackground;

    [Header("Drawing Area")]
    [SerializeField] private RawImage drawingCanvas;
    [SerializeField] private RectTransform drawingRect;

    [Header("UI Elements")]
    [Tooltip("Small dot showing where the pen actually draws after distortion is applied.")]
    [SerializeField] private RectTransform cursorDot;
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private Button clearAllButton;
    [SerializeField] private Button closeButton;

    [Header("Ink")]
    [SerializeField] private Color inkColor = new Color(0.08f, 0.04f, 0f, 1f);
    [SerializeField] private int brushRadius = 5;
    [SerializeField] private int eraserRadius = 12;
    [SerializeField] private int textureResolution = 512;

    [Header("Distortion")]
    [Tooltip("Seconds of free drawing before the inversion kicks in.")]
    [SerializeField] private float distortionDelaySec = 8f;
    [SerializeField] private bool invertX = true;
    [SerializeField] private bool invertY = true;
    [Tooltip("Extra rotation added on top of inversion (degrees). 0 = pure mirror.")]
    [SerializeField] private float extraRotationDeg = 0f;
    [Tooltip("Random pixel jitter per stamp (hand tremor). 0 = none.")]
    [SerializeField] private int tremorJitter = 1;

    private Texture2D _tex;
    private bool _drawingStarted;
    private float _drawingStartTime;
    private bool _distortionActive;
    private bool _hadPrevPoint;
    private Vector2 _prevTexCoord;
    private bool _texDirty;
    private PlayerController _player;
    private Canvas _parentCanvas;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _parentCanvas = GetComponentInParent<Canvas>();
        gameObject.SetActive(false);
    }

    private void Start()
    {
        clearAllButton?.onClick.AddListener(ClearAll);
        closeButton?.onClick.AddListener(CloseGame);

        if (instructionText != null)
            instructionText.text = "Draw the clock.  Show the time as 10:10.\nRight-click to erase.";
    }

    private void OnDestroy()
    {
        if (_tex != null) Destroy(_tex);
    }

    public void OpenGame(string overrideMonologue = null)
    {
        _player = FindFirstObjectByType<PlayerController>();
        if (_player != null) _player.MovementLocked = true;

        if (monologueText != null)
            monologueText.text = overrideMonologue ?? monologue;

        _drawingStarted = false;

        gameObject.SetActive(true);

        if (monologuePanel != null) monologuePanel.SetActive(true);
        if (drawingPanel != null) drawingPanel.SetActive(false);
    }

    public void ClearAll()
    {
        if (_tex == null) return;
        Color32[] blank = new Color32[textureResolution * textureResolution];
        for (int i = 0; i < blank.Length; i++) blank[i] = new Color32(0, 0, 0, 0);
        _tex.SetPixels32(blank);
        _tex.Apply();
    }

    public void SetBoardSprite(Sprite sprite)
    {
        if (boardBackground != null) boardBackground.sprite = sprite;
    }

    public void CloseGame()
    {
        if (_tex != null) { Destroy(_tex); _tex = null; }
        gameObject.SetActive(false);
        if (_player != null) { _player.MovementLocked = false; _player = null; }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace))
        {
            CloseGame();
            return;
        }

        if (!_drawingStarted)
        {
            bool dismiss = Input.GetKeyDown(KeyCode.Space) ||
                           Input.GetKeyDown(KeyCode.Return) ||
                           Input.GetMouseButtonDown(0);
            if (dismiss) StartDrawing();
            return;
        }

        if (drawingPanel == null || !drawingPanel.activeSelf) return;

        if (!_distortionActive && Time.time - _drawingStartTime >= distortionDelaySec)
        {
            _distortionActive = true;
            if (instructionText != null)
                instructionText.text = "Something feels… off.";
        }

        _texDirty = false;

        Camera uiCam = (_parentCanvas != null &&
                        _parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                       ? _parentCanvas.worldCamera : null;

        bool inRect = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            drawingRect, Input.mousePosition, uiCam, out Vector2 localPos);

        bool isErasing = Input.GetMouseButton(1);
        bool isDrawing = Input.GetMouseButton(0);

        Vector2 distorted = _distortionActive ? Distort(localPos) : localPos;
        if (cursorDot != null) cursorDot.localPosition = distorted;

        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
            _hadPrevPoint = false;

        if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1))
            _hadPrevPoint = false;

        if ((isDrawing || isErasing) && inRect)
        {
            Vector2 texCoord = LocalToTexCoord(distorted);
            int radius = isErasing ? eraserRadius : brushRadius;
            Color color = isErasing ? Color.clear : inkColor;

            if (_hadPrevPoint) StrokeLine(_prevTexCoord, texCoord, color, radius);
            else StrokeDot((int)texCoord.x, (int)texCoord.y, color, radius);

            _prevTexCoord = texCoord;
            _hadPrevPoint = true;
            _texDirty = true;
        }
        else if (!isDrawing && !isErasing)
        {
            _hadPrevPoint = false;
        }

        if (_texDirty) _tex.Apply();
    }

    private void StartDrawing()
    {
        _drawingStarted = true;
        _drawingStartTime = Time.time;
        _distortionActive = false;
        _hadPrevPoint = false;
        _texDirty = false;

        if (monologuePanel != null) monologuePanel.SetActive(false);
        if (drawingPanel != null) drawingPanel.SetActive(true);

        CreateFreshTexture();
    }

    private Vector2 Distort(Vector2 local)
    {
        float x = invertX ? -local.x : local.x;
        float y = invertY ? -local.y : local.y;

        if (Mathf.Abs(extraRotationDeg) > 0.001f)
        {
            float rad = extraRotationDeg * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
            return new Vector2(cos * x - sin * y, sin * x + cos * y);
        }
        return new Vector2(x, y);
    }

    private Vector2 LocalToTexCoord(Vector2 local)
    {
        Rect r = drawingRect.rect;
        float u = Mathf.InverseLerp(r.xMin, r.xMax, local.x);
        float v = Mathf.InverseLerp(r.yMin, r.yMax, local.y);
        return new Vector2(
            Mathf.Clamp(u * textureResolution, 0, textureResolution - 1),
            Mathf.Clamp(v * textureResolution, 0, textureResolution - 1));
    }

    private void StrokeDot(int cx, int cy, Color color, int radius)
    {
        if (color != Color.clear && tremorJitter > 0)
        {
            cx += Random.Range(-tremorJitter, tremorJitter + 1);
            cy += Random.Range(-tremorJitter, tremorJitter + 1);
        }
        int r2 = radius * radius;
        for (int dx = -radius; dx <= radius; dx++)
            for (int dy = -radius; dy <= radius; dy++)
                if (dx * dx + dy * dy <= r2) SetPixelSafe(cx + dx, cy + dy, color);
    }

    private void StrokeLine(Vector2 from, Vector2 to, Color color, int radius)
    {
        int x0 = (int)from.x, y0 = (int)from.y;
        int x1 = (int)to.x, y1 = (int)to.y;
        int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = -Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;
        while (true)
        {
            StrokeDot(x0, y0, color, radius);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }

    private void SetPixelSafe(int x, int y, Color color)
    {
        if (x >= 0 && x < textureResolution && y >= 0 && y < textureResolution)
            _tex.SetPixel(x, y, color);
    }

    private void CreateFreshTexture()
    {
        if (_tex != null) Destroy(_tex);
        _tex = new Texture2D(textureResolution, textureResolution, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        Color32[] blank = new Color32[textureResolution * textureResolution];
        for (int i = 0; i < blank.Length; i++) blank[i] = new Color32(0, 0, 0, 0);
        _tex.SetPixels32(blank);
        _tex.Apply();
        if (drawingCanvas != null) drawingCanvas.texture = _tex;
    }
}