using UnityEngine;

public class MiniGameStageObject : MonoBehaviour
{
    [Tooltip("Which stage index activates this mini game station. " +
             "Must match the stage passed to SonNPC.TriggerItemThresholdApproach().")]
    [SerializeField] public int activationStage = 0;

    public int ActivationStage => activationStage;

    private void Awake()
    {
        gameObject.transform.GetChild(0).gameObject.SetActive(false);
    }

    private void Start()
    {
        ItemProgressionManager.Instance?.RegisterMiniGame(this);
    }

    public void SetVisible(bool visible) => gameObject.transform.GetChild(0).gameObject.SetActive(visible);

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