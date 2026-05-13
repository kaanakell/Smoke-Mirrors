using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class RoomTransition : MonoBehaviour
{
    [Header("Transition Settings")]
    [SerializeField] private string targetScene;
    [SerializeField] private string spawnPointID;

    [Header("Lock System")]
    [SerializeField] private bool isLocked = false;
    [SerializeField] private DialogueSet lockedDialogue;

    private bool _transitioning = false;

    public void SetLocked(bool locked)
    {
        isLocked = locked;
        Debug.Log($"[RoomTransition] {gameObject.name} is now {(isLocked ? "LOCKED" : "UNLOCKED")}");
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (_transitioning) return;

        if (col.CompareTag("Player"))
        {
            if (isLocked)
            {
                Vector3 pushDirection = (col.transform.position - transform.position).normalized;
                col.transform.position += pushDirection * 0.5f;

                if (lockedDialogue != null && DialogueManager.Instance != null)
                {
                    PlayerController pc = col.GetComponent<PlayerController>();
                    if (pc != null) pc.MovementLocked = true;

                    DialogueManager.Instance.StartDialogue(lockedDialogue, () =>
                    {
                        if (pc != null) pc.MovementLocked = false;
                    });
                }
                return;
            }

            StartCoroutine(DoTransition());
        }
    }

    private IEnumerator DoTransition()
    {
        _transitioning = true;
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null) pc.MovementLocked = true;

        PlayerSpawnManager.NextSpawnID = spawnPointID;

        if (PlayerSpawnManager.Instance != null)
            yield return StartCoroutine(PlayerSpawnManager.Instance.FadeOut(0.5f));

        if (CorridorSequenceManager.IsEmergencyLoopActive)
        {
            CorridorSequenceManager.LoopCount++;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        else
        {
            SceneManager.LoadScene(targetScene);
        }
    }
}