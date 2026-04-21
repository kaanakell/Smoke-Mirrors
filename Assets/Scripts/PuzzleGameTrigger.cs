using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PuzzleGameTrigger : MonoBehaviour, IInteractable
{
    [SerializeField] private string promptText = "[SPACE]  Look at puzzle";

    [Tooltip("Optional: override the puzzle sprite for this specific trigger. " +
             "Leave null to use the sprite set on PuzzleGame.")]
    [SerializeField] private Sprite puzzleSpriteOverride;

    [Tooltip("If true, can only be used once per session.")]
    [SerializeField] private bool oneTimeUse = false;

    private bool _used = false;

    public string PromptText => promptText;
    public bool CanInteract => !(_used && oneTimeUse);

    public void Interact(GameObject interactor)
    {
        if (!CanInteract) return;

        if (PuzzleGame.Instance == null)
        {
            Debug.LogWarning("[PuzzleGameTrigger] PuzzleGame.Instance not found in scene.");
            return;
        }

        if (oneTimeUse) _used = true;

        PuzzleGame.Instance.OpenGame(puzzleSpriteOverride);
    }
}