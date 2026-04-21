using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PuzzleGame : MonoBehaviour
{
    public static PuzzleGame Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject panel;
    [SerializeField] private RectTransform pieceContainer;

    [Header("Puzzle Image")]
    [Tooltip("Sprite to slice. Texture does NOT need Read/Write enabled.")]
    [SerializeField] private Sprite puzzleSprite;

    [Range(2, 10)]
    [SerializeField] private int pieceCount = 6;

    [Header("Piece Prefab")]
    [Tooltip("RawImage + PuzzlePiece. See wiring steps in script header.")]
    [SerializeField] private GameObject piecePrefab;

    [Header("Piece Size")]
    [Tooltip("Display size per piece in canvas pixels. " +
             "Set to (containerWidth / cols, containerHeight / rows). " +
             "E.g. 600×400 container, 3×2 grid → 200×200.")]
    [SerializeField] private Vector2 pieceSize = new Vector2(160f, 160f);

    [Header("Scatter & Snap")]
    [Tooltip("Scatter radius for initial piece placement. " +
             "Should be LARGER than pieceSize so pieces aren't already touching.")]
    [SerializeField] private float scatterRadius = 350f;

    [Tooltip("Max centre-to-centre distance (canvas px) for a snap attempt.")]
    [SerializeField] private float snapProximityThreshold = 110f;

    [Tooltip("Colour similarity 0–1. Lower = stricter. Recommended 0.25–0.35.")]
    [Range(0f, 1f)]
    [SerializeField] private float colorSnapThreshold = 0.28f;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI statusText;
    [Tooltip("Brief text that pops up when a colour match happens, e.g. 'Colour match!'. " +
             "Position it centered near the top of PieceContainer.")]
    [SerializeField] private TextMeshProUGUI colourMatchText;
    [SerializeField] private Button closeButton;

    private readonly Dictionary<PuzzlePiece, List<PuzzlePiece>> _pieceToGroup = new();
    private readonly List<PuzzlePiece> _allPieces = new();
    private int _snapCount;

    private PlayerController _player;
    private Coroutine _matchTextCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (panel != null) panel.SetActive(false);
        if (colourMatchText != null) colourMatchText.enabled = false;
    }

    private void Start()
    {
        closeButton?.onClick.AddListener(CloseGame);
    }

    private void Update()
    {
        if (panel == null || !panel.activeSelf) return;
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace))
            CloseGame();
    }

    public void OpenGame(Sprite overrideSprite = null)
    {
        _player = FindFirstObjectByType<PlayerController>();
        if (_player != null) _player.MovementLocked = true;

        if (overrideSprite != null) puzzleSprite = overrideSprite;
        if (panel != null) panel.SetActive(true);

        BuildPuzzle();
    }

    public void CloseGame()
    {
        DestroyPieces();
        if (panel != null) panel.SetActive(false);
        if (_player != null) { _player.MovementLocked = false; _player = null; }
    }

    public List<PuzzlePiece> GetGroup(PuzzlePiece piece)
    {
        return _pieceToGroup.TryGetValue(piece, out var g) ? g : new List<PuzzlePiece> { piece };
    }

    public void TrySnap(PuzzlePiece dropped)
    {
        List<PuzzlePiece> droppedGroup = GetGroup(dropped);

        PuzzlePiece bestCandidate = null;
        float bestScore = float.MaxValue;

        foreach (var other in _allPieces)
        {
            if (GetGroup(other) == droppedGroup) continue;

            float dist = Vector2.Distance(
                dropped.Rt.localPosition, other.Rt.localPosition);

            if (dist > snapProximityThreshold) continue;

            float colorDist = ColorDistance(dropped.AverageColor, other.AverageColor);
            if (colorDist > colorSnapThreshold) continue;

            float score = colorDist * 0.7f + (dist / snapProximityThreshold) * 0.3f;
            if (score < bestScore) { bestScore = score; bestCandidate = other; }
        }

        if (bestCandidate == null) return;

        Vector2 dir = (Vector2)(dropped.Rt.localPosition - bestCandidate.Rt.localPosition);
        Vector2 snapOffset = Mathf.Abs(dir.x) >= Mathf.Abs(dir.y)
            ? new Vector2(Mathf.Sign(dir.x) * pieceSize.x, 0f)
            : new Vector2(0f, Mathf.Sign(dir.y) * pieceSize.y);

        Vector2 positionDelta = ((Vector2)bestCandidate.Rt.localPosition + snapOffset)
                                - (Vector2)dropped.Rt.localPosition;

        foreach (var p in droppedGroup)
            p.Rt.localPosition += (Vector3)positionDelta;

        Color matchedColor = (dropped.AverageColor + bestCandidate.AverageColor) * 0.5f;

        MergeGroups(bestCandidate, dropped);

        foreach (var p in GetGroup(bestCandidate))
            p.FlashSnapColor(matchedColor);

        ShowColourMatchCaption(matchedColor);

        _snapCount++;
        UpdateStatus();
    }

    private void BuildPuzzle()
    {
        DestroyPieces();
        _snapCount = 0;

        if (puzzleSprite == null) { Debug.LogError("[PuzzleGame] No puzzle sprite."); return; }
        if (piecePrefab == null) { Debug.LogError("[PuzzleGame] No piece prefab."); return; }

        GetGridDimensions(pieceCount, out int cols, out int rows);

        Texture2D tex = ExtractReadableTexture(puzzleSprite);
        int cellW = Mathf.Max(1, tex.width / cols);
        int cellH = Mathf.Max(1, tex.height / rows);

        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                Color[] pixels = tex.GetPixels(c * cellW, r * cellH, cellW, cellH);
                Color avgColor = ComputeAverageColor(pixels);

                Texture2D pieceTex = new Texture2D(cellW, cellH, TextureFormat.RGBA32, false)
                { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                pieceTex.SetPixels(pixels);
                pieceTex.Apply();

                GameObject go = Instantiate(piecePrefab, pieceContainer);
                go.GetComponent<RectTransform>().sizeDelta = pieceSize;
                go.GetComponent<RectTransform>().localPosition =
                    (Vector3)(Random.insideUnitCircle * scatterRadius);

                var img = go.GetComponent<RawImage>();
                if (img != null) img.texture = pieceTex;

                var piece = go.GetComponent<PuzzlePiece>();
                if (piece == null)
                {
                    Debug.LogError("[PuzzleGame] Piece prefab missing PuzzlePiece component!");
                    continue;
                }

                piece.Init(r * cols + c, avgColor, this);
                _pieceToGroup[piece] = new List<PuzzlePiece> { piece };
                _allPieces.Add(piece);
            }

        Destroy(tex);
        UpdateStatus();
    }

    private void DestroyPieces()
    {
        foreach (var p in _allPieces)
        {
            if (p == null) continue;
            var img = p.GetComponent<RawImage>();
            if (img?.texture != null) Destroy(img.texture);
            Destroy(p.gameObject);
        }
        _allPieces.Clear();
        _pieceToGroup.Clear();
    }

    private void MergeGroups(PuzzlePiece anchor, PuzzlePiece incoming)
    {
        var anchorGroup = GetGroup(anchor);
        var incomingGroup = GetGroup(incoming);
        if (anchorGroup == incomingGroup) return;

        foreach (var p in incomingGroup)
        {
            anchorGroup.Add(p);
            _pieceToGroup[p] = anchorGroup;
            p.ShowGroupHighlight();
        }
        foreach (var p in anchorGroup)
            p.ShowGroupHighlight();
    }

    private void ShowColourMatchCaption(Color matchedColor)
    {
        if (colourMatchText == null) return;
        if (_matchTextCoroutine != null) StopCoroutine(_matchTextCoroutine);
        _matchTextCoroutine = StartCoroutine(ColourCaptionRoutine(matchedColor));
    }

    private IEnumerator ColourCaptionRoutine(Color col)
    {
        colourMatchText.text = "Colour match!";
        colourMatchText.color = new Color(col.r, col.g, col.b, 1f);
        colourMatchText.enabled = true;

        yield return new WaitForSeconds(0.15f);

        float duration = 0.8f, elapsed = 0f;
        Color start = colourMatchText.color;
        Color end = new Color(start.r, start.g, start.b, 0f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            colourMatchText.color = Color.Lerp(start, end, elapsed / duration);
            yield return null;
        }

        colourMatchText.enabled = false;
        _matchTextCoroutine = null;
    }

    private static void GetGridDimensions(int desired, out int cols, out int rows)
    {
        switch (desired)
        {
            case 2: cols = 2; rows = 1; break;
            case 3: cols = 3; rows = 1; break;
            case 4: cols = 2; rows = 2; break;
            case 5: case 6: cols = 3; rows = 2; break;
            case 7: case 8: cols = 4; rows = 2; break;
            case 9: cols = 3; rows = 3; break;
            default: cols = 5; rows = 2; break;
        }
    }

    private static float ColorDistance(Color a, Color b)
    {
        float dr = a.r - b.r, dg = a.g - b.g, db = a.b - b.b;
        return Mathf.Sqrt(dr * dr + dg * dg + db * db) / 1.732f;
    }

    private static Color ComputeAverageColor(Color[] px)
    {
        if (px == null || px.Length == 0) return Color.gray;
        float r = 0, g = 0, b = 0;
        foreach (var c in px) { r += c.r; g += c.g; b += c.b; }
        return new Color(r / px.Length, g / px.Length, b / px.Length, 1f);
    }

    private static Texture2D ExtractReadableTexture(Sprite sprite)
    {
        Rect rect = sprite.rect;
        int w = Mathf.Max(1, (int)rect.width), h = Mathf.Max(1, (int)rect.height);
        var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(sprite.texture, rt);
        var prev = RenderTexture.active; RenderTexture.active = rt;
        var result = new Texture2D(w, h, TextureFormat.RGBA32, false)
        { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        result.ReadPixels(new Rect(rect.x, rect.y, w, h), 0, 0);
        result.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        return result;
    }

    private void UpdateStatus()
    {
        if (statusText != null)
            statusText.text = $"{_snapCount} connections made";
    }
}