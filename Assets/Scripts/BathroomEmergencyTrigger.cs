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

        // Check if we reached the target stage
        if (ItemProgressionManager.Instance != null && ItemProgressionManager.Instance.CurrentStage == targetStage)
        {
            _hasTriggered = true;
            StartCoroutine(EmergencyRoutine());
        }
    }

    private IEnumerator EmergencyRoutine()
    {
        // Wait for the timer
        yield return new WaitForSeconds(delayBeforeEmergency);

        // Wait until the player is free (not talking or playing a game)
        while (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
        {
            yield return new WaitForSeconds(1f);
        }

        // 1. Play the "I need to go" dialogue
        bool dialogueFinished = false;
        DialogueManager.Instance.StartDialogue(emergencyDialogue, () => dialogueFinished = true);
        yield return new WaitUntil(() => dialogueFinished);

        // 2. Activate Loop Mode!
        CorridorSequenceManager.IsEmergencyLoopActive = true;
        CorridorSequenceManager.LoopCount = 0;

        // 3. Fade out and teleport
        if (PlayerSpawnManager.Instance != null)
        {
            yield return StartCoroutine(PlayerSpawnManager.Instance.FadeOut(0.5f));
        }

        PlayerSpawnManager.NextSpawnID = corridorSpawnID;
        SceneManager.LoadScene(corridorSceneName);
    }
}