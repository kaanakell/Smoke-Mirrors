using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class TrashPickupItem : MonoBehaviour, IInteractable
{
    [Header("Item Info")]
    [SerializeField] private ItemData itemData;
    
    [Header("Son's Reaction")]
    [Tooltip("The dialogue the Son will say when you pick this up.")]
    [SerializeField] private DialogueSet sonReactionDialogue;

    private bool _collected = false;

    public string PromptText => $"[SPACE]  Pick up {itemData?.itemName ?? "item"}";
    public bool CanInteract => !_collected;

    public void Interact(GameObject interactor)
    {
        if (!CanInteract || itemData == null) return;
        _collected = true;

        // 1. Add to the player's UI Inventory
        Inventory.Instance?.AddItem(itemData);
        
        // Play sound if you have one
        if (itemData.pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(itemData.pickupSound, transform.position);
        }

        // Hide the item in the physical world immediately
        GetComponent<SpriteRenderer>().enabled = false;
        GetComponent<Collider2D>().enabled = false;

        // 2. Start the sequence with the Son
        StartCoroutine(HandleTrashSequence());
    }

    private IEnumerator HandleTrashSequence()
    {
        SonNPC son = FindFirstObjectByType<SonNPC>();

        if (son != null)
        {
            // Wait patiently just in case the Son is busy playing a mini-game
            while (!son.IsAvailable)
            {
                yield return new WaitForSeconds(0.5f);
            }

            bool conversationFinished = false;

            // 3. Call the Son over with our specific dialogue
            son.TriggerCustomApproach(sonReactionDialogue, () =>
            {
                // 4. This runs AFTER the dialogue closes! Remove the item.
                Inventory.Instance?.RemoveItem(itemData);
                Debug.Log($"[TrashPickup] Son took the {itemData.itemName} away.");
                conversationFinished = true;
            });

            // Wait until the callback finishes before destroying this object
            yield return new WaitUntil(() => conversationFinished);
        }
        else
        {
            // Failsafe: If the Son doesn't exist, just remove the item immediately
            Inventory.Instance?.RemoveItem(itemData);
        }

        // Clean up the invisible object from the scene
        Destroy(gameObject);
    }
}