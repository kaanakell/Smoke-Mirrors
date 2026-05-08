using UnityEngine;

/// <summary>
/// Place this component on the world-space GameObject that represents a mini game station
/// (the table, screen, or visual prop the Son walks the player to).
///
/// ItemProgressionManager will automatically show this object when the game reaches
/// activationStage and hide it once the mini game for that stage is completed.
///
/// The Son NPC's miniGameWaypoints[] entry for this stage should point at this transform
/// so it knows where to walk before opening the mini game panel.
/// </summary>
public class MiniGameStageObject : MonoBehaviour
{
    [Tooltip("Which stage index activates this mini game station. " +
             "Must match the stage passed to SonNPC.TriggerItemThresholdApproach().")]
    [SerializeField] public int activationStage = 0;

    public int ActivationStage => activationStage;

    // =========================================================================
    // Unity lifecycle
    // =========================================================================

    private void Awake()
    {
        gameObject.transform.GetChild(0).gameObject.SetActive(false);
    }

    private void Start()
    {
        // Register with IPM so it can drive our visibility.
        // IPM is DontDestroyOnLoad so it will exist if set up correctly.
        if (ItemProgressionManager.Instance != null)
            ItemProgressionManager.Instance.RegisterMiniGame(this);
        else
            Debug.LogWarning($"[MiniGameStageObject] IPM not found — '{gameObject.name}' " +
                             "will stay hidden. Make sure ItemProgressionManager is in the scene.");
    }

    // =========================================================================
    // Visibility API (called by ItemProgressionManager)
    // =========================================================================

    /// <summary>Shows or hides the entire station including its children.</summary>
    public void SetVisible(bool visible) => gameObject.transform.GetChild(0).gameObject.SetActive(visible);

    // =========================================================================
    // Editor gizmos
    // =========================================================================

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.9f);
        Gizmos.DrawSphere(transform.position, 0.22f);

        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 0.55f,
            $"Stage {activationStage} Mini Game",
            new GUIStyle(UnityEditor.EditorStyles.boldLabel)
            {
                normal = { textColor = new Color(1f, 0.7f, 0f) },
                fontSize = 10
            }
        );
    }
#endif
}