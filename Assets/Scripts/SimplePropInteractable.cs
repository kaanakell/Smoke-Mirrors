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

    // IInteractable properties
    public string PromptText => promptText;

    // We want the player to always be able to interact with these as many times as they want!
    public bool CanInteract => true;

    private AudioSource _audioSource;

    private void Awake()
    {
        // Check if there is an AudioSource attached for volume/pitch control
        _audioSource = GetComponent<AudioSource>();
    }

    public void Interact(GameObject interactor)
    {
        if (!CanInteract) return;

        // 1. Play the sound effect if one is assigned
        if (interactionSound != null)
        {
            if (_audioSource != null)
            {
                _audioSource.PlayOneShot(interactionSound);
            }
            else
            {
                // Fallback: plays the sound at the object's position even without an AudioSource
                AudioSource.PlayClipAtPoint(interactionSound, transform.position);
            }
        }

        // 2. Trigger the Dialogue
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