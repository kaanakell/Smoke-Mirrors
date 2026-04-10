using UnityEngine;

[CreateAssetMenu(menuName = "Smoke And Mirrors/Item Data", fileName = "ItemData_")]
public class ItemData : ScriptableObject
{
    [Header("Identity")]
    public string itemName = "Unknown Item";

    [TextArea(2, 5)]
    public string description = "An item that feels familiar.";

    [Header("Visuals")]
    public Sprite worldSprite;
    public Sprite inventorySprite;

    [Header("Narrative")]
    [TextArea(3, 8)]
    public string memoryText = "";

    [Header("Audio")]
    public AudioClip pickupSound;

    [Header("Mind Forest")]
    [Tooltip("If false, picking up this item will never trigger the mind forest event.")]
    public bool canTriggerMindForest = true;
}