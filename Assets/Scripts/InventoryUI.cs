using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("Slot Container")]
    [SerializeField] private RectTransform slotContainer;
    [SerializeField] private GameObject slotPrefab;

    [Header("Empty Slot Visual")]
    [SerializeField] private Sprite emptySlotSprite;
    [SerializeField] private Color emptyColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    [SerializeField] private Color filledColor = Color.white;

    private readonly List<InventorySlot> slots = new();
    private bool initialized = false;

    void Start()
    {
        if (Inventory.Instance == null)
        {
            Debug.LogError("[InventoryUI] Inventory.Instance is null!");
            return;
        }

        Inventory.Instance.OnInventoryChanged += RefreshUI;
        BuildSlots();
        initialized = true;

        gameObject.SetActive(false);
    }

    void OnEnable()
    {
        if (initialized) RefreshUI();
    }

    void BuildSlots()
    {
        foreach (Transform child in slotContainer)
            Destroy(child.gameObject);

        slots.Clear();

        for (int i = 0; i < Inventory.Instance.Capacity; i++)
        {
            GameObject go = Instantiate(slotPrefab, slotContainer);
            InventorySlot slot = go.GetComponent<InventorySlot>();

            if (slot == null)
            {
                Debug.LogError("[InventoryUI] Slot prefab is missing an InventorySlot component!");
                continue;
            }

            slot.SetEmpty(emptySlotSprite, emptyColor);
            slots.Add(slot);
        }
    }

    void RefreshUI()
    {
        if (Inventory.Instance == null) return;

        List<ItemData> items = Inventory.Instance.GetItems();

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null) continue;

            if (i < items.Count && items[i] != null)
            {
                Sprite icon = items[i].inventorySprite != null
                    ? items[i].inventorySprite
                    : items[i].worldSprite;

                slots[i].SetItem(items[i], icon, filledColor);
            }
            else
            {
                slots[i].SetEmpty(emptySlotSprite, emptyColor);
            }
        }
    }

    void OnDestroy()
    {
        if (Inventory.Instance != null)
            Inventory.Instance.OnInventoryChanged -= RefreshUI;
    }
}
