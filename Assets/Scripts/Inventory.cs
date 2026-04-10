using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; private set; }

    [SerializeField] private int capacity = 8;

    public int Capacity => capacity;
    public event Action OnInventoryChanged;

    private readonly List<ItemData> items = new();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddItem(ItemData item)
    {
        if (items.Count >= capacity)
        {
            Debug.Log("[Inventory] Inventory full!");
            return;
        }

        items.Add(item);
        Debug.Log($"[Inventory] Picked up: {item.itemName} (Total: {items.Count})");
        OnInventoryChanged?.Invoke();
    }

    public bool RemoveItem(ItemData item)
    {
        bool removed = items.Remove(item);
        if (removed)
        {
            Debug.Log($"[Inventory] Removed: {item.itemName}");
            OnInventoryChanged?.Invoke();
        }
        return removed;
    }

    public bool HasItem(ItemData item) => items.Contains(item);

    public List<ItemData> GetItems() => new(items);
}