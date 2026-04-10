using UnityEngine;
using TMPro;

public class TooltipUI : MonoBehaviour
{
    public static TooltipUI Instance { get; private set; }

    [Header("References")]
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private RectTransform tooltipRect;

    [Header("Settings")]
    [SerializeField] private Vector2 offset = new Vector2(15f, -15f);

    private Canvas canvas;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        canvas = GetComponentInParent<Canvas>();
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (gameObject.activeSelf)
            FollowMouse();
    }

    public void Show(string description)
    {
        descriptionText.text = description;
        gameObject.SetActive(true);
        FollowMouse();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    void FollowMouse()
    {
        if (canvas == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            Input.mousePosition,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out Vector2 localPoint
        );

        tooltipRect.localPosition = localPoint + offset;
    }
}