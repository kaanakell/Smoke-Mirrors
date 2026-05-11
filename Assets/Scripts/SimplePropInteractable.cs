using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SimplePropInteractable : MonoBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    [SerializeField] private string promptText = "[SPACE]  Look";

    [Header("Monologue")]
    [Tooltip("The dialogue set to play when the player interacts with this object.")]
    [SerializeField] private DialogueSet dialogueSet;

    [Header("Audio (Optional)")]
    [Tooltip("Place the piano SFX here. Leave empty for the painting.")]
    [SerializeField] private AudioClip interactionSound;

    public string PromptText => promptText;

    public bool CanInteract => true;

    private AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    public void Interact(GameObject interactor)
    {
        if (!CanInteract) return;

        if (interactionSound != null)
        {
            if (_audioSource != null)
            {
                _audioSource.PlayOneShot(interactionSound);
            }
            else
            {
                AudioSource.PlayClipAtPoint(interactionSound, transform.position);
            }
        }

        if (dialogueSet != null && dialogueSet.lines.Length > 0)
        {
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartDialogue(dialogueSet);
            }
            else
            {
                Debug.LogWarning("[Prop] DialogueManager is missing from the scene!");
            }
        }
    }
}