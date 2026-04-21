using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ClockDrawingTrigger : MonoBehaviour, IInteractable
{
    [SerializeField] private string promptText = "[SPACE]  Draw the clock";

    [TextArea(2, 4)]
    [Tooltip("Override the monologue text shown before drawing. " +
             "Leave empty to use the default set on ClockDrawingGame.")]
    [SerializeField] private string monologueOverride = "";

    [Tooltip("If true, can only be used once per session.")]
    [SerializeField] private bool oneTimeUse = true;

    private bool _used = false;

    public string PromptText => promptText;
    public bool CanInteract => !(_used && oneTimeUse);

    public void Interact(GameObject interactor)
    {
        if (!CanInteract) return;

        if (ClockDrawingGame.Instance == null)
        {
            Debug.LogWarning("[ClockDrawingTrigger] ClockDrawingGame.Instance not found in scene.");
            return;
        }

        if (oneTimeUse) _used = true;

        string msg = string.IsNullOrEmpty(monologueOverride) ? null : monologueOverride;
        ClockDrawingGame.Instance.OpenGame(msg);
    }
}