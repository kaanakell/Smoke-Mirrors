using TMPro;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float interactRadius = 1.2f;
    [SerializeField] private LayerMask interactLayer;

    [Header("UI Prompt")]
    [Tooltip("Assign in prefab, or tag your prompt GameObject 'PromptUI' for auto-find.")]
    [SerializeField] private GameObject promptUI;
    [Tooltip("Assign in prefab, or it will be found inside the PromptUI object automatically.")]
    [SerializeField] private TextMeshProUGUI promptLabel;

    private IInteractable _nearest;
    private PlayerController _controller;

    void Awake()
    {
        _controller = GetComponent<PlayerController>();
        ResolvePromptReferences();
    }

    private void ResolvePromptReferences()
    {
        if (promptUI == null)
        {
            var found = GameObject.FindGameObjectWithTag("PromptUI");
            if (found != null)
            {
                promptUI = found;
            }
            else
            {
                Debug.LogWarning("[PlayerInteraction] promptUI is null and no GameObject tagged 'PromptUI' was found. " +
                                 "Either assign it in the prefab or tag your prompt panel 'PromptUI'.");
            }
        }

        if (promptLabel == null && promptUI != null)
        {
            promptLabel = promptUI.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        // Start hidden
        if (promptUI != null)
            promptUI.SetActive(false);
    }

    void OnEnable()
    {
        ResolvePromptReferences();
    }

    void Update()
    {
        if (promptUI == null) ResolvePromptReferences();

        FindNearest();

        if (promptUI != null)
            promptUI.SetActive(_nearest != null);

        if (promptLabel != null && _nearest != null)
            promptLabel.text = _nearest.PromptText;

        if (_nearest != null && Input.GetKeyDown(KeyCode.Space))
        {
            if (!_controller.MovementLocked)
                _nearest.Interact(gameObject);
        }
    }

    private void FindNearest()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactRadius, interactLayer);

        float bestDist = float.MaxValue;
        _nearest = null;

        foreach (var hit in hits)
        {
            IInteractable interactable = hit.GetComponent<IInteractable>();
            if (interactable == null) continue;
            if (!interactable.CanInteract) continue;

            float dist = Vector2.Distance(transform.position, hit.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                _nearest = interactable;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}

public interface IInteractable
{
    string PromptText { get; }
    bool CanInteract { get; }
    void Interact(GameObject interactor);
}