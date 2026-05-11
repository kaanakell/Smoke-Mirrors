using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class BathroomEmergencyTrigger : MonoBehaviour
{
    [Header("Trigger Conditions")]
    [Tooltip("Which stage should trigger this emergency? (e.g. 2)")]
    [SerializeField] private int targetStage = 2;
    [Tooltip("How many seconds after the stage begins before the emergency hits?")]
    [SerializeField] private float delayBeforeEmergency = 120f; // 2 minutes

    [Header("Sequence")]
    [SerializeField] private DialogueSet emergencyDialogue;
    [SerializeField] private string corridorSceneName = "CorridorScene";
    [SerializeField] private string corridorSpawnID = "From_LivingRoom";

    private bool _hasTriggered = false;

    private void Update()
    {
        if (_hasTriggered) return;

        if (ItemProgressionManager.Instance != null && ItemProgressionManager.Instance.CurrentStage == targetStage)
        {
            _hasTriggered = true;
            StartCoroutine(EmergencyRoutine());
        }
    }

    private IEnumerator EmergencyRoutine()
    {
        yield return new WaitForSeconds(delayBeforeEmergency);

        while (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
        {
            yield return new WaitForSeconds(1f);
        }

        bool dialogueFinished = false;
        DialogueManager.Instance.StartDialogue(emergencyDialogue, () => dialogueFinished = true);
        yield return new WaitUntil(() => dialogueFinished);

        CorridorSequenceManager.IsEmergencyLoopActive = true;
        CorridorSequenceManager.LoopCount = 0;

        if (PlayerSpawnManager.Instance != null)
        {
            yield return StartCoroutine(PlayerSpawnManager.Instance.FadeOut(0.5f));
        }

        PlayerSpawnManager.NextSpawnID = corridorSpawnID;
        SceneManager.LoadScene(corridorSceneName);
    }
}