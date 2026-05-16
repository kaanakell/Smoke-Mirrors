using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class PuzzlePiece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [HideInInspector] public RectTransform rectTransform;
    private Image _image;

    [Header("Positions")]
    [Tooltip("The anchored position where this piece belongs.")]
    public Vector2 correctPosition;
    [Tooltip("The anchored position where this piece jumps to during the glitch.")]
    public Vector2 scrambledPosition;

    private bool _isLocked = false;
    public bool isLocked
    {
        get => _isLocked;
        set
        {
            _isLocked = value;
            if (_isLocked)
                transform.SetAsFirstSibling();
            SetPieceAlpha(1f);
        }
    }

    private PuzzleGame _game;
    private Canvas _canvas;
    private RectTransform _canvasRect;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        _image = GetComponent<Image>();
    }

    public void Setup(PuzzleGame game, Canvas canvas)
    {
        _game = game;
        _canvas = canvas;
        _canvasRect = canvas.GetComponent<RectTransform>();
    }

    private void Update()
    {
        if (_image == null) return;
        Color c = _image.color;
        c.a = 1f;
        _image.color = c;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isLocked || _game.IsPlayingGlitch) return;

        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isLocked || _game.IsPlayingGlitch) return;

        rectTransform.anchoredPosition += eventData.delta / _canvas.scaleFactor;

        ClampToScreen();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isLocked || _game.IsPlayingGlitch) return;

        _game.CheckPiecePlacement(this);
    }

    private void ClampToScreen()
    {
        if (_canvasRect == null) return;

        Vector3[] canvasCorners = new Vector3[4];
        _canvasRect.GetWorldCorners(canvasCorners);

        Vector3[] pieceCorners = new Vector3[4];
        rectTransform.GetWorldCorners(pieceCorners);

        Vector3 pos = rectTransform.position;

        if (pieceCorners[0].x < canvasCorners[0].x) pos.x += canvasCorners[0].x - pieceCorners[0].x;
        if (pieceCorners[2].x > canvasCorners[2].x) pos.x -= pieceCorners[2].x - canvasCorners[2].x;
        if (pieceCorners[0].y < canvasCorners[0].y) pos.y += canvasCorners[0].y - pieceCorners[0].y;
        if (pieceCorners[2].y > canvasCorners[2].y) pos.y -= pieceCorners[2].y - canvasCorners[2].y;

        rectTransform.position = pos;
    }

    public void SnapTo(Vector2 pos)
    {
        if (rectTransform != null)
            rectTransform.anchoredPosition = pos;
        SetPieceAlpha(1f);
    }

    private void SetPieceAlpha(float alpha)
    {
        if (_image == null) _image = GetComponent<Image>();
        if (_image != null)
        {
            Color c = _image.color;
            c.a = alpha;
            _image.color = c;
        }
    }
}