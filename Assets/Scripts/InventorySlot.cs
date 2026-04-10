using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Image image;
    private TextMeshProUGUI nameText;
    private ItemData currentItem;

    void Awake()
    {
        image = GetComponent<Image>();
        // Grab the TMP child automatically — no manual wiring needed
        nameText = GetComponentInChildren<TextMeshProUGUI>();

        if (nameText != null)
            nameText.text = "";
    }

    public void SetItem(ItemData item, Sprite icon, Color color)
    {
        currentItem = item;

        if (image != null)
        {
            image.sprite = icon;
            image.color = color;
        }

        if (nameText != null)
            nameText.text = item.itemName;
    }

    public void SetEmpty(Sprite emptySprite, Color color)
    {
        currentItem = null;

        if (image != null)
        {
            image.sprite = emptySprite;
            image.color = color;
        }

        if (nameText != null)
            nameText.text = "";
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentItem != null)
            TooltipUI.Instance?.Show(currentItem.description);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipUI.Instance?.Hide();
    }
}