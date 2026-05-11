using UnityEngine;
using System.Collections;
using UnityEngine.Events;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class ProgressionPickupItem : MonoBehaviour, IInteractable
{
    [Header("Item")]
    [SerializeField] private ItemData itemData;

    [Header("Progression")]
    [Tooltip("0 = visible at start. 1 = after 1st mini-game. 2 = after 2nd. Etc.")]
    [SerializeField] private int unlockStageIndex = 0;

    [Header("Events")]
    [Tooltip("Fire events when this item is picked up (e.g. unlock a door).")]
    public UnityEvent onCollected;

    public int UnlockStageIndex => unlockStageIndex;

    [Header("Bob")]
    [SerializeField] private float bobAmplitude = 0.06f;
    [SerializeField] private float bobSpeed = 1.5f;

    public string PromptText => $"[SPACE]  Pick up {itemData?.itemName ?? "item"}";
    public bool CanInteract => _visible && !_collected;

    private bool _visible = false;
    private bool _collected = false;
    private SpriteRenderer _sr;
    private Collider2D _col;
    private AudioSource _audio;
    private Vector3 _startPos;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _col = GetComponent<Collider2D>();
        _audio = GetComponent<AudioSource>();
        _startPos = transform.position;

        if (itemData?.worldSprite != null)
            _sr.sprite = itemData.worldSprite;

        SetVisible(false);

        if (itemData != null && Inventory.Instance != null && Inventory.Instance.HasItem(itemData))
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        ItemProgressionManager.Instance?.RegisterItem(this);
    }

    private void Update()
    {
        if (!_visible || _collected) return;
        float offsetY = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        transform.position = _startPos + new Vector3(0f, offsetY, 0f);
    }

    public void SetVisible(bool visible)
    {
        _visible = visible;
        _sr.enabled = visible;
        _col.enabled = visible;
    }

    public void Interact(GameObject interactor)
    {
        if (!CanInteract || itemData == null) return;
        _collected = true;

        onCollected?.Invoke();

        Inventory.Instance?.AddItem(itemData);

        if (itemData.pickupSound != null)
        {
            if (_audio != null) _audio.PlayOneShot(itemData.pickupSound);
            else AudioSource.PlayClipAtPoint(itemData.pickupSound, transform.position);
        }

        bool isForestItem = MindForestTrigger.Instance != null &&
                            MindForestTrigger.Instance.TryTrigger(itemData, interactor);

        if (!isForestItem && !string.IsNullOrEmpty(itemData.memoryText))
        {
            MemoryDisplay.Instance?.ShowMemory(itemData.memoryText, itemData.memoryBackground);
        }

        ItemProgressionManager.Instance?.ReportItemCollected(this, isForestItem);

        _sr.enabled = false;
        _col.enabled = false;
        StartCoroutine(DestroyAfterDelay(0.5f));
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }

    private void OnValidate()
    {
        if (itemData?.worldSprite != null)
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.sprite = itemData.worldSprite;
        }
    }
}