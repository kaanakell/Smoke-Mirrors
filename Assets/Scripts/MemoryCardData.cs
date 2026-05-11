using UnityEngine;

[CreateAssetMenu(menuName = "Smoke And Mirrors/Memory Card Data", fileName = "CardData_")]
public class MemoryCardData : ScriptableObject
{
    [Tooltip("Display name shown in debug / tooltips.")]
    public string cardLabel = "Card";

    [Tooltip("The sprite shown on both cards (one may be scrambled at runtime).")]
    public Sprite cardSprite;

    [Tooltip("How large the scrambled chunks are (in pixels). Higher number = less scrambled. (Try 16, 32, or 64)")]
    [Min(4)]
    public int scrambleBlockSize = 32;

    [TextArea(1, 3)]
    [Tooltip("Optional flavour text — not used in-game yet, but useful for narrative iteration.")]
    public string flavourText = "";
}