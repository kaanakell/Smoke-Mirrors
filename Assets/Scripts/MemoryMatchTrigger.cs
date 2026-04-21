using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MemoryMatchTrigger : MonoBehaviour, IInteractable
{
    [SerializeField] private string promptText = "[SPACE]  Look at pictures";

    [Tooltip("If true, can only be opened once per session.")]
    [SerializeField] private bool oneTimeUse = false;

    private bool _used = false;

    public string PromptText => promptText;
    public bool CanInteract => !(_used && oneTimeUse);

    public void Interact(GameObject interactor)
    {
        if (!CanInteract) return;

        if (MemoryMatchGame.Instance == null)
        {
            Debug.LogWarning("[MemoryMatchTrigger] MemoryMatchGame.Instance not found in scene.");
            return;
        }

        if (oneTimeUse) _used = true;

        MemoryMatchGame.Instance.OpenGame();
    }
}