using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class PuzzlePiece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [HideInInspector] public RectTransform rectTransform;

    [Header("Positions")]
    [Tooltip("The anchored position where this piece belongs.")]
    public Vector2 correctPosition;
    [Tooltip("The anchored position where this piece jumps to during the glitch.")]
    public Vector2 scrambledPosition;

    [HideInInspector] public bool isLocked = false;

    private PuzzleGame _game;
    private Canvas _canvas;
    private RectTransform _canvasRect;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void Setup(PuzzleGame game, Canvas canvas)
    {
        _game = game;
        _canvas = canvas;
        _canvasRect = canvas.GetComponent<RectTransform>();
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
        rectTransform.anchoredPosition = pos;
    }
}