using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class PickupItem : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemData itemData;

    [Header("Visual Feedback")]
    [SerializeField] private float bobAmplitude = 0.06f;
    [SerializeField] private float bobSpeed = 1.5f;

    public string PromptText => $"[SPACE]  Pick up {itemData?.itemName ?? "item"}";
    public bool CanInteract => !_collected;

    private bool _collected = false;
    private SpriteRenderer _sr;
    private Vector3 _startPos;
    private AudioSource _audio;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _audio = GetComponent<AudioSource>();
        _startPos = transform.position;

        if (itemData != null && itemData.worldSprite != null)
            _sr.sprite = itemData.worldSprite;

        // Already collected in a previous visit — silently remove from world
        if (itemData != null && Inventory.Instance != null && Inventory.Instance.HasItem(itemData))
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Update()
    {
        if (_collected) return;
        float offsetY = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        transform.position = _startPos + new Vector3(0f, offsetY, 0f);
    }

    public void Interact(GameObject interactor)
    {
        if (_collected || itemData == null) return;
        _collected = true;

        Inventory.Instance.AddItem(itemData);

        if (itemData.pickupSound != null)
        {
            if (_audio != null)
                _audio.PlayOneShot(itemData.pickupSound);
            else
                AudioSource.PlayClipAtPoint(itemData.pickupSound, transform.position);
        }

        // TryTrigger returns true if the forest will fire.
        // It handles memory text → forest sequencing internally,
        // so we must NOT show memory text ourselves if it returns true.
        bool forestTriggered = MindForestTrigger.Instance != null &&
                               MindForestTrigger.Instance.TryTrigger(itemData, interactor);

        if (!forestTriggered && !string.IsNullOrEmpty(itemData.memoryText))
            MemoryDisplay.Instance?.ShowMemory(itemData.memoryText);

        _sr.enabled = false;
        GetComponent<Collider2D>().enabled = false;
        StartCoroutine(DestroyAfterDelay(0.5f));
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }

    private void OnValidate()
    {
        if (itemData != null && itemData.worldSprite != null)
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.sprite = itemData.worldSprite;
        }
    }
}