using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class BathroomEmergencyTrigger : MonoBehaviour
{
    [Header("Trigger Conditions")]
    [Tooltip("Which story phase should trigger this emergency?")]
    [SerializeField] private StoryManager.StoryPhase targetPhase = StoryManager.StoryPhase.Forest1Done;

    [Tooltip("How many seconds after the phase begins before the emergency hits?")]
    [SerializeField] private float delayBeforeEmergency = 30f;

    [Header("Sequence")]
    [SerializeField] private DialogueSet emergencyDialogue;
    [SerializeField] private string corridorSceneName = "CorridorScene";
    [SerializeField] private string corridorSpawnID = "From_LivingRoom";

    private bool _hasTriggered = false;

    private void Update()
    {
        if (_hasTriggered) return;

        if (StoryManager.Instance == null) return;

        if (StoryManager.Instance.corridorEventHasOccurred)
        {
            _hasTriggered = true;
            return;
        }

        if (StoryManager.Instance.currentPhase == targetPhase)
        {
            _hasTriggered = true;
            StoryManager.Instance.corridorEventHasOccurred = true;
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
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(emergencyDialogue, () => dialogueFinished = true);
            yield return new WaitUntil(() => dialogueFinished);
        }

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