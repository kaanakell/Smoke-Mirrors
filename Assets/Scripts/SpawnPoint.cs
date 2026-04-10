using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [Tooltip("Must match the spawnPointID set on the RoomTransition that leads to this scene.")]
    public string spawnID = "default";

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.8f);
        Gizmos.DrawSphere(transform.position, 0.2f);
        Gizmos.DrawIcon(transform.position + Vector3.up * 0.4f, "d_NavMeshAgent Icon", true);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 0.6f,
            $"SPAWN: {spawnID}",
            new GUIStyle { normal = { textColor = Color.green }, fontSize = 10 }
        );
#endif
    }
}
