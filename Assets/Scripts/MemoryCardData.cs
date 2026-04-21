using UnityEngine;

[CreateAssetMenu(menuName = "Smoke And Mirrors/Memory Card Data", fileName = "CardData_")]
public class MemoryCardData : ScriptableObject
{
    [Tooltip("Display name shown in debug / tooltips.")]
    public string cardLabel = "Card";

    [Tooltip("The sprite shown on both cards (one may be scrambled at runtime).")]
    public Sprite cardSprite;

    [TextArea(1, 3)]
    [Tooltip("Optional flavour text — not used in-game yet, but useful for narrative iteration.")]
    public string flavourText = "";
}