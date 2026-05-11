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

        Inventory.Instance?.AddItem(itemData);
        
        if (itemData.pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(itemData.pickupSound, transform.position);
        }

        GetComponent<SpriteRenderer>().enabled = false;
        GetComponent<Collider2D>().enabled = false;

        StartCoroutine(HandleTrashSequence());
    }

    private IEnumerator HandleTrashSequence()
    {
        SonNPC son = FindFirstObjectByType<SonNPC>();

        if (son != null)
        {
            while (!son.IsAvailable)
            {
                yield return new WaitForSeconds(0.5f);
            }

            bool conversationFinished = false;

            son.TriggerCustomApproach(sonReactionDialogue, () =>
            {
                Inventory.Instance?.RemoveItem(itemData);
                Debug.Log($"[TrashPickup] Son took the {itemData.itemName} away.");
                conversationFinished = true;
            });

            yield return new WaitUntil(() => conversationFinished);
        }
        else
        {
            Inventory.Instance?.RemoveItem(itemData);
        }

        Destroy(gameObject);
    }
}