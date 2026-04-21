using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

[RequireComponent(typeof(RawImage))]
public class PuzzlePiece : MonoBehaviour,
    IPointerDownHandler, IDragHandler, IPointerUpHandler
{

    public int PieceIndex { get; private set; }
    public Color AverageColor { get; private set; }
    public RectTransform Rt { get; private set; }


    [Header("Snap Outline")]
    [Tooltip("Child Image that outlines the piece once it joins a group. " +
             "IMPORTANT: set its anchors to stretch-fill (Min 0,0 → Max 1,1, all offsets 0) " +
             "so it always matches the piece's runtime size.")]
    [SerializeField] private Image outlineImage;
    [SerializeField] private Color highlightColor = new Color(1f, 0.9f, 0.3f, 0.85f);

    [Header("Colour Swatch")]
    [Tooltip("Small corner Image (e.g. 20×20 px, bottom-left of the piece) that shows the " +
             "average colour of this piece. Helps players understand colour-based snapping.")]
    [SerializeField] private Image colourSwatchImage;

    [Header("Snap Flash")]
    [Tooltip("How long the colour flash lasts when this piece snaps to another.")]
    [SerializeField] private float flashDuration = 0.35f;

    private PuzzleGame _manager;
    private RawImage _rawImage;
    private Vector2 _lastLocalPos;
    private Coroutine _flashCoroutine;

    public void Init(int index, Color avgColor, PuzzleGame manager)
    {
        PieceIndex = index;
        AverageColor = avgColor;
        _manager = manager;
        Rt = GetComponent<RectTransform>();
        _rawImage = GetComponent<RawImage>();

        if (outlineImage != null) outlineImage.enabled = false;

        // Paint the swatch with this piece's average colour
        if (colourSwatchImage != null)
        {
            colourSwatchImage.color = new Color(avgColor.r, avgColor.g, avgColor.b, 1f);
            colourSwatchImage.enabled = true;
        }
    }

    public void OnPointerDown(PointerEventData e)
    {
        foreach (var p in _manager.GetGroup(this))
            p.transform.SetAsLastSibling();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            Rt.parent as RectTransform, e.position, e.pressEventCamera, out _lastLocalPos);
    }

    public void OnDrag(PointerEventData e)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            Rt.parent as RectTransform, e.position, e.pressEventCamera, out Vector2 cur);

        Vector2 delta = cur - _lastLocalPos;
        _lastLocalPos = cur;

        foreach (var p in _manager.GetGroup(this))
            p.Rt.localPosition += (Vector3)delta;
    }

    public void OnPointerUp(PointerEventData e)
    {
        _manager.TrySnap(this);
    }

    public void ShowGroupHighlight()
    {
        if (outlineImage == null) return;
        outlineImage.color = highlightColor;
        outlineImage.enabled = true;
    }

    public void FlashSnapColor(Color snapColor)
    {
        if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
        _flashCoroutine = StartCoroutine(FlashRoutine(snapColor));
    }

    private IEnumerator FlashRoutine(Color flashCol)
    {
        Color startCol = new Color(flashCol.r, flashCol.g, flashCol.b, 0.65f);
        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            _rawImage.color = Color.Lerp(startCol, Color.white, elapsed / flashDuration);
            yield return null;
        }
        _rawImage.color = Color.white;
        _flashCoroutine = null;
    }
}