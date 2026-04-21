using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RawImage))]
public class MemoryMatchCard : MonoBehaviour, IPointerClickHandler
{
    public int MatchID { get; private set; }
    public bool IsMatched { get; private set; }
    public bool IsSelected { get; private set; }

    private RawImage _image;
    private MemoryMatchGame _manager;

    [Header("Feedback Colours")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = new Color(1f, 0.85f, 0.3f, 1f);
    [SerializeField] private Color matchedColor = new Color(0.5f, 1f, 0.5f, 0.75f);

    [Header("Outline (optional)")]
    [Tooltip("Optional child Image that lights up on select/match.")]
    [SerializeField] private Image outlineImage;

    public void Init(int matchID, Texture2D texture, bool unrecognizable, MemoryMatchGame manager)
    {
        MatchID = matchID;
        _manager = manager;
        _image = GetComponent<RawImage>();

        _image.texture = unrecognizable ? ScrambleTexture(texture) : texture;
        _image.color = normalColor;

        if (outlineImage != null) outlineImage.enabled = false;
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (IsMatched || IsSelected) return;
        _manager.OnCardClicked(this);
    }

    public void SetSelected(bool selected)
    {
        IsSelected = selected;
        _image.color = selected ? selectedColor : normalColor;
        if (outlineImage != null) outlineImage.enabled = selected;
    }

    public void SetMatched()
    {
        IsMatched = true;
        IsSelected = false;
        _image.color = matchedColor;
        if (outlineImage != null)
        {
            outlineImage.color = matchedColor;
            outlineImage.enabled = true;
        }
    }

    private static Texture2D ScrambleTexture(Texture2D source)
    {
        int w = source.width, h = source.height;
        Texture2D copy = new Texture2D(w, h, source.format, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        copy.SetPixels(source.GetPixels());

        const int block = 4;
        int bCols = w / block;
        int bRows = h / block;

        for (int by = 0; by < bRows; by++)
            for (int bx = 0; bx < bCols; bx++)
            {
                int rby = Random.Range(0, bRows);
                int rbx = Random.Range(0, bCols);
                SwapBlocks(copy, bx * block, by * block, rbx * block, rby * block, block);
            }

        copy.Apply();
        return copy;
    }

    private static void SwapBlocks(Texture2D t, int ax, int ay, int bx, int by, int sz)
    {
        for (int dy = 0; dy < sz; dy++)
            for (int dx = 0; dx < sz; dx++)
            {
                Color ca = t.GetPixel(ax + dx, ay + dy);
                Color cb = t.GetPixel(bx + dx, by + dy);
                t.SetPixel(ax + dx, ay + dy, cb);
                t.SetPixel(bx + dx, by + dy, ca);
            }
    }
}